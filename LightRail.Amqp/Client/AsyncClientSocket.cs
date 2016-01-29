﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading.Tasks;
using LightRail.Amqp.Network;
using LightRail.Amqp.Protocol;
using NLog;

namespace LightRail.Amqp.Client
{
    public class AsyncClientSocket : ISocket
    {
        private static readonly ILogger logger = LogManager.GetLogger("LightRail.Amqp.Client.TcpConnection");

        private const int defaultMaxBufferBlockSize = 64 * 1024; // 64 KB

        public AsyncClientSocket(string host, int port, bool useTLS)
        {
            this.host = host;
            this.port = port;
            this.useTLS = useTLS;

            var bufferPoolSize = defaultMaxBufferBlockSize * 100;
            BufferPool = new PinnedMemoryBufferPool(bufferPoolSize, defaultMaxBufferBlockSize);

            this.receiveEventArgs = new SocketAsyncEventArgs();
            this.receiveEventArgs.Completed += (s, a) => TcpSocket.CompleteAsyncIOOperation(((TaskCompletionSource<int>)a.UserToken), a, b => b.BytesTransferred);

            this.sendEventArgs = new SocketAsyncEventArgs();
            this.sendEventArgs.Completed += (s, a) => TcpSocket.CompleteAsyncIOOperation(((TcpSocket.SendAsyncBufferToken<int>)a.UserToken), a, b => b.bytesTransferred);
        }

        private readonly string host;
        private readonly int port;
        private readonly bool useTLS;

        public IBufferPool BufferPool { get; }

        private Socket socket;
        private readonly SocketAsyncEventArgs receiveEventArgs;
        private readonly SocketAsyncEventArgs sendEventArgs;
        private SslStream sslStream;

        public bool IsConnected { get; private set; }
        private volatile bool isConnecting = false;
        private readonly object connectAsyncLock = new object();

        public async Task ConnectAsync()
        {
            if (IsConnected || isConnecting)
                return;

            lock (connectAsyncLock)
            {
                if (IsConnected || isConnecting)
                    return;
                isConnecting = true;
            }

            try
            {
                IPAddress[] ipAddresses;
                IPAddress ip;
                if (IPAddress.TryParse(host, out ip))
                {
                    ipAddresses = new IPAddress[] { ip };
                }
                else
                {
                    logger.Trace("Resolving DNS for Host '{0}'", host);
                    ipAddresses = await GetHostAddressesAsync(host);
                }

                Exception exception = null;
                for (int i = 0; i < ipAddresses.Length; i++)
                {
                    if (ipAddresses[i] == null ||
                        (ipAddresses[i].AddressFamily == AddressFamily.InterNetwork && !Socket.OSSupportsIPv4) ||
                        (ipAddresses[i].AddressFamily == AddressFamily.InterNetworkV6 && !Socket.OSSupportsIPv6))
                    {
                        continue;
                    }

                    socket = new Socket(ipAddresses[i].AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        logger.Trace("Attempting to connect to {0}:{1}", ipAddresses[i].ToString(), port.ToString());
                        await TcpSocket.ConnectAsync(socket, ipAddresses[i], port);
                        logger.Trace("Successfully connected to {0}:{1}", ipAddresses[i].ToString(), port.ToString());
                        exception = null;
                        break;
                    }
                    catch (Exception e)
                    {
                        exception = e;
                        socket.Dispose();
                        socket = null;
                    }
                }

                if (socket == null)
                {
                    logger.Error(exception, "Error connecting to {0}", host);
                    throw exception ?? new SocketException((int)SocketError.AddressNotAvailable);
                }

                if (useTLS)
                {
                    logger.Trace("Starting TLS Authentication.");
                    sslStream = new SslStream(new NetworkStream(socket));
                    await sslStream.AuthenticateAsClientAsync(host, null, SslProtocols.Tls12 | SslProtocols.Tls11, true); // TODO super-safe defaults, but need to parameterize
                    logger.Trace("Finished TLS Authentication.");
                }

                IsConnected = true;
            }
            finally
            {
                isConnecting = false;
            }
        }

        private static Task<IPAddress[]> GetHostAddressesAsync(string host)
        {
            return Task.Factory.FromAsync(
                (c, s) => Dns.BeginGetHostAddresses(host, c, s),
                (r) => Dns.EndGetHostAddresses(r),
                null);
        }

        public Task SendAsync(ByteBuffer byteBuffer)
        {
            return SendAsync(byteBuffer.Buffer, byteBuffer.ReadOffset, byteBuffer.LengthAvailableToRead);
        }

        public async Task SendAsync(byte[] buffer, int offset, int count)
        {
            try
            {
                if (sslStream != null)
                {
                    await sslStream.WriteAsync(buffer, offset, count);
                    logger.Trace("Sent {0} Bytes", count.ToString());
                }
                else
                {
                    int bytesSent = await TcpSocket.SendAsync(socket, sendEventArgs, buffer, offset, count);
                    logger.Trace("Sent {0} Bytes", bytesSent.ToString());
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ReceiveAsync() Error. Closing Socket.");
                Close();
                throw;
            }
        }

        public async Task<int> ReceiveAsync(byte[] buffer, int offset, int count)
        {
            try
            {
                int bytesRead;
                if (sslStream != null)
                {
                    bytesRead = await sslStream.ReadAsync(buffer, offset, count);
                }
                else
                {
                    bytesRead = await TcpSocket.ReceiveAsync(socket, receiveEventArgs, buffer, offset, count);
                }
                logger.Trace("Read {0} bytes", bytesRead.ToString());
                return bytesRead;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ReceiveAsync() Error. Closing Socket.");
                Close();
                throw;
            }
        }

        public void Close()
        {
            try
            {
                logger.Trace("Shutting Down Socket.");
                socket.Shutdown(SocketShutdown.Both);
                logger.Trace("Closing Socket.");
                socket.Close();
            }
            catch (Exception) { } // intentionally swallow exceptions here.
            finally
            {
                IsConnected = false;
            }
        }

        public void CloseRead()
        {
            try
            {
                logger.Trace("Shutting Down Receive Side of Socket.");
                socket.Shutdown(SocketShutdown.Receive);
            }
            catch (Exception) { } // intentionally swallow exceptions here.
        }

        public void CloseWrite()
        {
            try
            {
                logger.Trace("Shutting Down Send Side of Socket.");
                socket.Shutdown(SocketShutdown.Send);
            }
            catch (Exception) { } // intentionally swallow exceptions here.
        }
    }
}
;