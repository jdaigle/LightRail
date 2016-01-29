using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LightRail.Amqp;
using LightRail.Amqp.Network;
using NLog;

namespace LightRail.Server.Network
{
    public class TcpListener
    {
        private static readonly ILogger logger = LogManager.GetLogger("LightRail.Server.Network.TcpListener");

        private const int maxBufferBlockSize = 64 * 1024; // 64 KB

        public int ListenPort { get; } = Constants.AmqpPort;
        public int MaxConnections { get; } = 100;
        private int currentConnections;

        private readonly IBufferPool _memoryBufferPool;
        private Socket _listenSocket;

        private readonly SocketAsyncEventArgs _acceptSocketAsyncEventArgs;

        public TcpListener()
        {
            var bufferPoolSize = maxBufferBlockSize * 2 * MaxConnections; // bufferSize * 2 * maxConn (one send/reiv buffer each per conn)
            _memoryBufferPool = new PinnedMemoryBufferPool(bufferPoolSize, maxBufferBlockSize);

            _acceptSocketAsyncEventArgs = new SocketAsyncEventArgs();
            _acceptSocketAsyncEventArgs.Completed += AsyncEventCompleted;
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

            // create the connection, will immediately starting polling the receive side of the socket
            // via an async pump
            new TcpConnection(this, e.AcceptSocket, _memoryBufferPool);

            // Loop to accept another connection.
            if (startAccept)
                StartAccept(e);
        }

        private void CloseSocket(Socket socket, TcpConnection connection)
        {
            try
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
            finally
            {
                logger.Debug("Decrement Connection Count");
                Interlocked.Decrement(ref currentConnections);
            }
        }

        /// <summary>
        /// Marks a specific connection for graceful shutdown. The next receive or send to be posted
        /// will fail and close the connection.
        /// </summary>
        public void Disconnect(TcpConnection connection, SocketShutdown socketShutdown)
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
    }
}
