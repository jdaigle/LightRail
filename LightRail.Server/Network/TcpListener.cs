using System;
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

        public int ListenPort { get; } = Constants.AmqpPort;
        public int MaxConnections { get; } = 100;

        private readonly PinnedMemoryBufferPool _memoryBufferPool;
        private readonly ConcurrentStack<SocketAsyncEventArgs> _receiveSocketAsyncEventArgs = new ConcurrentStack<SocketAsyncEventArgs>();
        private readonly ConcurrentStack<SocketAsyncEventArgs> _sendSocketAsyncEventArgs = new ConcurrentStack<SocketAsyncEventArgs>();
        private Socket _listenSocket;

        private readonly SocketAsyncEventArgs _acceptSocketAsyncEventArgs;

        public TcpListener()
        {
            var bufferSize = 64 * 1024; // 64 KB
            var bufferPoolSize = bufferSize * 2 * MaxConnections; // bufferSize * 2 * maxConn (one send/reiv buffer each per conn)
            _memoryBufferPool = new PinnedMemoryBufferPool(bufferPoolSize, bufferSize);

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
                    CompleteAccept(e);
                    break;
                case SocketAsyncOperation.Receive:
                    CompleteReceive(e);
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
            e.AcceptSocket = null;
            if (_listenSocket.AcceptAsync(e) == false)
            {
                CompleteAccept(e);
            }
        }

        /// <summary>
        /// Completion callback routine for AcceptAsync(). This will verify that the Accept occured
        /// and then setup a Receive chain to begin receiving data.
        /// </summary>
        /// <param name="e">Information about the Accept call.</param>
        private void CompleteAccept(SocketAsyncEventArgs e)
        {
            // setup the connected socket
            e.AcceptSocket.NoDelay = true;
#if DEBUG
            var ipAddress = (e.AcceptSocket.RemoteEndPoint as IPEndPoint);
            logger.Debug("Connection Accepting From {0}:{1}", ipAddress.Address.ToString(), ipAddress.Port.ToString());
#endif

            SocketAsyncEventArgs args;
            if (!_receiveSocketAsyncEventArgs.TryPop(out args))
            {
                // TODO too many connections
                logger.Debug("Too Many Connections.");
                TryCloseSocket(e.AcceptSocket);
                return;
            }

            var connection = new TcpConnectionState(this, e.AcceptSocket);
            args.UserToken = connection;
            _memoryBufferPool.SetBuffer(args);
            connection.ReceiveBufferOffset = args.Offset;
            connection.ReceiverBufferSize = args.Count;

            StartReceive(args);

            // Loop to accept another connection.
            StartAccept(e);
        }

        /// <summary>
        /// Post an asynchronous receive on the socket.
        /// </summary>
        /// <param name="e">Used to store information about the Receive call.</param>
        private void StartReceive(SocketAsyncEventArgs e)
        {
            try
            {
                var connection = e.UserToken as TcpConnectionState;
                if (connection != null)
                {
                    e.SetBuffer(connection.ReceiveBufferOffset, connection.ReceiverBufferSize);
                    if (connection.Socket.ReceiveAsync(e) == false)
                    {
                        CompleteReceive(e);
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
        private void CompleteReceive(SocketAsyncEventArgs e)
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
                    Disconnect(e);
                    return;
                }
                var bytesReceived = e.BytesTransferred;
                logger.Debug("Received {0} bytes from {1}", bytesReceived, connection.IPAddress);
                // TODO: handle data
                byte[] buffer = new byte[bytesReceived];
                Array.Copy(e.Buffer, e.Offset, buffer, 0, bytesReceived);
                if (buffer.Length == 1 && buffer[0] == Encoding.ASCII.GetBytes("q")[0])
                {
                    Disconnect(e);
                }
                SendDataAsync(connection, buffer, 0, bytesReceived);
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
        public void SendDataAsync(TcpConnectionState connection, byte[] buffer, int offset, int length)
        {
            try
            {
                SocketAsyncEventArgs e;
                if (!_sendSocketAsyncEventArgs.TryPop(out e))
                {
                    e = new SocketAsyncEventArgs();
                    e.UserToken = connection;
                    e.Completed += AsyncEventCompleted;
                }
                _memoryBufferPool.SetBuffer(e);
                Array.Copy(buffer, offset, e.Buffer, e.Offset, length); // copy to output buffer
                if (connection.Socket.SendAsync(e) == false)
                {
                    CompleteSend(e);
                }
            }
            catch (Exception ex)
            {
                logger.Fatal(ex, "Exception in Echo()");
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
                    Disconnect(e);
                    return;
                }
                // todo: determine if need to loop and send more?
                // finished sending...
                _memoryBufferPool.FreeBuffer(e);
                e.UserToken = null;
                _receiveSocketAsyncEventArgs.Push(e);
            }
            catch (Exception ex)
            {
                logger.Fatal(ex, "Exception in CompleteSend()");
            }
        }

        /// <summary>
        /// Disconnects a socket.
        /// </summary>
        /// <remarks>
        /// It is expected that this disconnect is always posted by a failed receive call. Calling the public
        /// version of this method will cause the next posted receive to fail and this will cleanup properly.
        /// It is not advised to call this method directly.
        /// </remarks>
        /// <param name="e">Information about the socket to be disconnected.</param>
        private void Disconnect(SocketAsyncEventArgs e)
        {
            var connection = e.UserToken as TcpConnectionState;
            if (connection == null)
            {
                throw (new ArgumentNullException("e.UserToken"));
            }
            logger.Debug("Closing Socket to {0}", connection.IPAddress);
            TryCloseSocket(connection.Socket);
            _memoryBufferPool.FreeBuffer(e);
            e.UserToken = null;
            _receiveSocketAsyncEventArgs.Push(e);
        }

        private void TryCloseSocket(Socket socket)
        {
            try
            {
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
        public void Disconnect(TcpConnectionState connection)
        {
            try
            {
                logger.Debug("Shutting Down Socket to {0}", connection.IPAddress);
                connection.Socket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception e)
            {
                logger.Error(e, "Non-fatal error shuttind down socket");
            }
        }
    }
}
