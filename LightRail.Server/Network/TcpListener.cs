﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using LightRail.Amqp;
using NLog;

namespace LightRail.Server.Network
{
    public class TcpListener
    {
        private static readonly ILogger logger = LogManager.GetLogger("LightRail.Server.Network.TcpListener");

        private const int maxBufferSize = 64 * 1024; // 64 KB

        public int ListenPort { get; } = Constants.AmqpPort;
        public int MaxConnections { get; } = 100;

        private readonly PinnedMemoryBufferPool _memoryBufferPool;
        private readonly ConcurrentStack<SocketAsyncEventArgs> _receiveSocketAsyncEventArgs = new ConcurrentStack<SocketAsyncEventArgs>();
        private readonly ConcurrentStack<SocketAsyncEventArgs> _sendSocketAsyncEventArgs = new ConcurrentStack<SocketAsyncEventArgs>();
        private Socket _listenSocket;

        private readonly SocketAsyncEventArgs _acceptSocketAsyncEventArgs;

        public TcpListener()
        {
            var bufferPoolSize = maxBufferSize * 2 * MaxConnections; // bufferSize * 2 * maxConn (one send/reiv buffer each per conn)
            _memoryBufferPool = new PinnedMemoryBufferPool(bufferPoolSize, maxBufferSize);

            _acceptSocketAsyncEventArgs = new SocketAsyncEventArgs();
            _acceptSocketAsyncEventArgs.Completed += AsyncEventCompleted;

            for (int i = 0; i < MaxConnections; i++)
            {
                var e = new SocketAsyncEventArgs();
                e.Completed += AsyncEventCompleted;
                _receiveSocketAsyncEventArgs.Push(e);
            }
        }

        public void Start()
        {
            IPEndPoint endpoint = new IPEndPoint(0, ListenPort);
            _listenSocket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _listenSocket.Bind(endpoint);
            _listenSocket.Listen(100);
            logger.Info("Listening on {0}:{1}", endpoint.Address.ToString(), ListenPort.ToString());

            StartAccept(_acceptSocketAsyncEventArgs);
        }

        private void AsyncEventCompleted(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Accept:
                    CompleteAccept(e, true);
                    break;
                case SocketAsyncOperation.Receive:
                    CompleteReceive(e, true);
                    break;
                case SocketAsyncOperation.Send:
                    CompleteSend(e);
                    break;
                default:
                    throw new InvalidOperationException("Unhandled Operation: " + e.LastOperation);
            }
        }

        /// <summary>
        /// Begins a request to Accept a connection. If it is being called from the completion of
        /// an AcceptAsync call, then the AcceptSocket is cleared since it will create a new one for
        /// the new user.
        /// </summary>
        /// <param name="e">null if posted from startup, otherwise a <b>SocketAsyncEventArgs</b> for reuse.</param>
        private void StartAccept(SocketAsyncEventArgs e)
        {
            acceptAgain:
            e.AcceptSocket = null;
            if (_listenSocket.AcceptAsync(e) == false)
            {
                CompleteAccept(e, false);
                goto acceptAgain;
            }
        }

        /// <summary>
        /// Completion callback routine for AcceptAsync(). This will verify that the Accept occured
        /// and then setup a Receive chain to begin receiving data.
        /// </summary>
        /// <param name="e">Information about the Accept call.</param>
        private void CompleteAccept(SocketAsyncEventArgs e, bool startAccept)
        {
            // setup the connected socket
            e.AcceptSocket.NoDelay = true;
#if DEBUG
            var ipAddress = (e.AcceptSocket.RemoteEndPoint as IPEndPoint);
            logger.Trace("Connection Accepting From {0}:{1}", ipAddress.Address.ToString(), ipAddress.Port.ToString());
#endif

            SocketAsyncEventArgs args;
            if (!_receiveSocketAsyncEventArgs.TryPop(out args))
            {
                // TODO too many connections
                logger.Trace("Too Many Connections.");
                TryCloseSocket(e.AcceptSocket, null);
                return;
            }

            var connection = new TcpConnectionState(this, e.AcceptSocket);
            args.UserToken = connection;
            _memoryBufferPool.SetBuffer(args);
            connection.ReceiveBufferOffset = args.Offset;
            connection.ReceiverBufferSize = args.Count;

            StartReceive(args);

            // Loop to accept another connection.
            if (startAccept)
                StartAccept(e);
        }

        /// <summary>
        /// Post an asynchronous receive on the socket.
        /// </summary>
        /// <param name="e">Used to store information about the Receive call.</param>
        private void StartReceive(SocketAsyncEventArgs e)
        {
            receiveAgain:
            try
            {
                var connection = e.UserToken as TcpConnectionState;
                if (connection != null)
                {
                    if (!connection.Socket.Connected)
                    {
                        ReleaseReceiveSocketAsyncEventArgs(e);
                        return;
                    }
                    e.SetBuffer(connection.ReceiveBufferOffset, connection.ReceiverBufferSize);
                    if (connection.Socket.ReceiveAsync(e) == false)
                    {
                        CompleteReceive(e, false);
                        goto receiveAgain;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Fatal(ex, "Exception in StartReceive()");
            }
        }

        /// <summary>
        /// Receive completion callback. Should verify the connection, and then notify any event listeners
        /// that data has been received. For now it is always expected that the data will be handled by the
        /// listeners and thus the buffer is cleared after every call.
        /// </summary>
        /// <param name="e">Information about the Receive call.</param>
        private void CompleteReceive(SocketAsyncEventArgs e, bool startReceive)
        {
            try
            {
                var connection = e.UserToken as TcpConnectionState;
                if (e.BytesTransferred == 0 || e.SocketError != SocketError.Success || connection == null)
                {
#if DEBUG
                    if (connection == null)
                        logger.Fatal("CompleteReceive() e.UserToken is not an instance of TcpConnectionState");
                    if (e.SocketError != SocketError.Success)
                        logger.Error("CompleteReceive() SocketError '{0}'", e.SocketError.ToString());
                    if (e.BytesTransferred == 0)
                        logger.Error("CompleteReceive() 0 Bytes Transferred. Shutting down socket.");
#endif
                    if (connection != null)
                    {
                        TryCloseSocket(connection.Socket, connection);
                    }
                    ReleaseReceiveSocketAsyncEventArgs(e);
                    return;
                }

                var bytesReceived = e.BytesTransferred;
                logger.Trace("Received {0} bytes from {1}", bytesReceived, connection.IPAddress);

                byte[] buffer = new byte[bytesReceived];
                Array.Copy(e.Buffer, e.Offset, buffer, 0, bytesReceived);
                connection.HandleReceived(new ByteBuffer(buffer, 0, bytesReceived, bytesReceived));

                // loop to receive more data
                if (startReceive)
                    StartReceive(e);
            }
            catch (Exception ex)
            {
                logger.Fatal(ex, "Exception in CompleteReceive()");
            }
        }

        /// <summary>
        /// Sends data back on the open socket for the specified connection.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="buffer">The data buffer to send.</param>
        /// <param name="offset">The offset in the data buffer to send.</param>
        /// <param name="length">The length of the data to send.</param>
        public void SendAsync(TcpConnectionState connection, byte[] buffer, int offset, int length)
        {
            SocketAsyncEventArgs e;
            if (!_sendSocketAsyncEventArgs.TryPop(out e))
            {
                e = new SocketAsyncEventArgs();
                e.Completed += AsyncEventCompleted;
            }
            e.UserToken = connection;
            if (length > maxBufferSize)
            {
                throw new InvalidOperationException($"Cannot Send Buffer of Length {length}. Max Buffer Sized = {maxBufferSize}. Chunking has not been implemented.");
            }
            _memoryBufferPool.SetBuffer(e);
            Array.Copy(buffer, offset, e.Buffer, e.Offset, length); // copy to output buffer
            e.SetBuffer(e.Offset, length);
            if (connection.Socket.SendAsync(e) == false)
            {
                CompleteSend(e);
            }
        }

        /// <summary>
        /// Completion callback for SendAsync.
        /// </summary>
        /// <param name="e">Information about the SendAsync call.</param>
        private void CompleteSend(SocketAsyncEventArgs e)
        {
            try
            {
                var connection = e.UserToken as TcpConnectionState;
                if (e.BytesTransferred == 0 || e.SocketError != SocketError.Success || connection == null)
                {
#if DEBUG
                    if (connection == null)
                        logger.Fatal("CompleteSend() e.UserToken is not an instance of TcpConnectionState");
                    if (e.SocketError != SocketError.Success)
                        logger.Error("CompleteSend() SocketError '{0}'", e.SocketError.ToString());
                    if (e.BytesTransferred == 0)
                        logger.Error("CompleteSend() 0 Bytes Transferred. Shutting down socket.");
#endif
                    if (connection != null)
                    {
                        logger.Debug("Closing Socket to {0}", connection.IPAddress);
                        TryCloseSocket(connection.Socket, connection);
                    }
                    ReleaseSendSocketAsyncEventArgs(e);
                    return;
                }
                logger.Trace("Sent {0} Bytes", e.BytesTransferred);
                // todo: determine if need to loop and send more?
                // finished sending...
                ReleaseSendSocketAsyncEventArgs(e);
            }
            catch (Exception ex)
            {
                logger.Fatal(ex, "Exception in CompleteSend()");
            }
        }

        private void TryCloseSocket(Socket socket, TcpConnectionState connection)
        {
            try
            {
                if (connection != null)
                {
                    connection.HandleSocketClosed();
                }
                socket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception e)
            {
                logger.Error(e, "Non-fatal error shutting down socket");
            }
            socket.Close();
        }

        /// <summary>
        /// Marks a specific connection for graceful shutdown. The next receive or send to be posted
        /// will fail and close the connection.
        /// </summary>
        public void Disconnect(TcpConnectionState connection, SocketShutdown socketShutdown)
        {
            try
            {
                logger.Debug("Shutting Down Socket to {0} Side = {1}", connection.IPAddress, socketShutdown.ToString());
                connection.Socket.Shutdown(socketShutdown);
            }
            catch (Exception e)
            {
                logger.Error(e, "Non-fatal error shuttind down socket");
            }
        }

        private void ReleaseReceiveSocketAsyncEventArgs(SocketAsyncEventArgs e)
        {
            _memoryBufferPool.FreeBuffer(e);
            e.UserToken = null;
            _receiveSocketAsyncEventArgs.Push(e);
        }

        private void ReleaseSendSocketAsyncEventArgs(SocketAsyncEventArgs e)
        {
            _memoryBufferPool.FreeBuffer(e);
            e.UserToken = null;
            _sendSocketAsyncEventArgs.Push(e);
        }
    }
}
