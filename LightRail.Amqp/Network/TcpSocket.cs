using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using LightRail.Amqp.Protocol;

namespace LightRail.Amqp.Network
{
    public class TcpSocket : ISocket
    {
        private static readonly TraceSource trace = TraceSource.FromClass();

        public Socket UnderlyingSocket { get; }
        public IPEndPoint IPAddress { get; }
        private readonly IBufferPool bufferPool;
        private readonly SocketAsyncEventArgs receiveEventArgs;
        private readonly SocketAsyncEventArgs sendEventArgs;
        private readonly WriteBuffer socketWriterBuffer;
        private readonly AmqpConnection amqpConnection;

        public TcpSocket(Socket acceptSocket, IBufferPool bufferPool, IContainer container)
        {
            UnderlyingSocket = acceptSocket;
            IPAddress = acceptSocket.RemoteEndPoint as IPEndPoint;
            this.bufferPool = bufferPool;

            ByteBuffer receiveBuffer;
            if (!bufferPool.TryGetByteBuffer(out receiveBuffer))
                throw new Exception("No free buffers available to receive on the underlying socket.");

            this.receiveEventArgs = new SocketAsyncEventArgs();
            this.receiveEventArgs.UserToken = new ReceiveAsyncState()
            {
                byteBuffer = receiveBuffer,
            };
            this.receiveEventArgs.SetBuffer(receiveBuffer.Buffer, 0, 0); // initially assign the buffer
            this.receiveEventArgs.Completed += OnIOOperationCompleted;

            ByteBuffer sendBuffer;
            if (!bufferPool.TryGetByteBuffer(out sendBuffer))
                throw new Exception("No free buffers available to send on the underlying socket.");

            this.sendEventArgs = new SocketAsyncEventArgs();
            this.sendEventArgs.UserToken = new SendAsyncState()
            {
                byteBuffer = sendBuffer,
            };
            this.sendEventArgs.SetBuffer(sendBuffer.Buffer, 0, 0); // initially assign the buffer
            this.sendEventArgs.Completed += OnIOOperationCompleted;

            socketWriterBuffer = new WriteBuffer(SendAsync);
            amqpConnection = new AmqpConnection(this, container);
        }

        public event EventHandler OnClosed;

        private class SendAsyncState
        {
            internal int countToWrite;
            internal ByteBuffer byteBuffer;
            internal Action sendCompleteCallback;
        }

        public void Write(ByteBuffer byteBuffer)
        {
            socketWriterBuffer.Write(byteBuffer);
        }

        public void SendAsync(ByteBuffer byteBuffer, Action sendCompleteCallback)
        {
            SendAsync(byteBuffer.Buffer, byteBuffer.ReadOffset, byteBuffer.LengthAvailableToRead, sendCompleteCallback);
        }

        public void SendAsync(byte[] buffer, int offset, int count, Action sendCompleteCallback)
        {
            var sendState = sendEventArgs.UserToken as SendAsyncState;
            sendState.byteBuffer.ResetReadWrite();
            sendState.sendCompleteCallback = sendCompleteCallback;

            Array.Copy(buffer, offset, sendState.byteBuffer.Buffer, sendState.byteBuffer.WriteOffset, count);
            sendState.byteBuffer.AppendWrite(count);

            sendState.countToWrite = count;

            SendAsyncLoop(sendState);
        }

        private void SendAsyncLoop(SendAsyncState sendState)
        {
sendAsyncLoopAgain:
            if (sendState.countToWrite > 0)
            {
                sendEventArgs.SetBuffer(sendState.byteBuffer.ReadOffset, sendState.countToWrite);
                if (UnderlyingSocket.SendAsync(sendEventArgs) == false)
                {
                    // completed synchrounsly
                    CompleteSend(sendEventArgs, false);
                    goto sendAsyncLoopAgain;
                }
            }
        }

        private void CompleteSend(SocketAsyncEventArgs args, bool executeLoop = false)
        {
            if (args.SocketError != SocketError.Success)
            {
                var exception = new SocketException((int)args.SocketError);
                amqpConnection.OnIoException(exception);
                return; // no more sending
            }

            var sendState = sendEventArgs.UserToken as SendAsyncState;

            int sentCount = args.BytesTransferred;
            sendState.countToWrite -= sentCount;
            sendState.byteBuffer.CompleteRead(sentCount);

            trace.Debug("Sent {0} bytes on socket", sentCount.ToString());

            if (sendState.countToWrite <= 0)
            {
                // all bytes sent
                sendState.sendCompleteCallback();
                return;
            }

            // more bytes to send
            if (sendState.countToWrite > 0 && executeLoop)
                SendAsyncLoop(sendState);
        }

        private class ReceiveAsyncState
        {
            internal int countToRead;
            internal Action<ByteBuffer> callback;
            internal ByteBuffer byteBuffer;
        }

        public void ReceiveAsync(int count, Action<ByteBuffer> callback)
        {
            var receiveState = receiveEventArgs.UserToken as ReceiveAsyncState;
            receiveState.countToRead = count;
            receiveState.callback = callback;
            ReceiveAsyncLoop(receiveState);
        }

        private void ReceiveAsyncLoop(ReceiveAsyncState receiveState)
        {
receiveAsyncLoopAgain:
            if (receiveState.countToRead > 0)
            {
                receiveEventArgs.SetBuffer(receiveState.byteBuffer.WriteOffset, receiveState.countToRead);
                if (UnderlyingSocket.ReceiveAsync(receiveEventArgs) == false)
                {
                    // completed synchrounsly
                    CompleteReceive(receiveEventArgs, false);
                    goto receiveAsyncLoopAgain;
                }
            }
        }

        private void CompleteReceive(SocketAsyncEventArgs args, bool executeLoop = false)
        {
            if (args.SocketError != SocketError.Success)
            {
                var exception = new SocketException((int)args.SocketError);
                amqpConnection.OnIoException(exception);
                return; // no more receiving
            }

            // If no data was received, close the connection. This is a NORMAL
            // situation that shows when the client has finished sending data.
            if (receiveEventArgs.BytesTransferred == 0)
            {
                Close();
                return; // no more receiving
            }

            var receiveState = receiveEventArgs.UserToken as ReceiveAsyncState;

            int receivedCount = args.BytesTransferred;
            receiveState.countToRead -= receivedCount;
            receiveState.byteBuffer.AppendWrite(receivedCount);

            trace.Debug("Read {0} bytes from socket", receivedCount.ToString());

            if (receiveState.countToRead <= 0)
            {
                // all bytes read, call the callback
                receiveState.callback(receiveState.byteBuffer);
                return;
            }

            // more bytes to read
            if (receiveState.countToRead > 0 && executeLoop)
                ReceiveAsyncLoop(receiveState);
        }

        private void OnIOOperationCompleted(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    CompleteReceive(e, true);
                    break;
                case SocketAsyncOperation.Send:
                    CompleteSend(e, true);
                    break;
                default:
                    throw new InvalidOperationException("Unhandled Operation: " + e.LastOperation);
            }
        }


        public void Close()
        {
            try
            {
                try
                {
                    UnderlyingSocket.Shutdown(SocketShutdown.Both);
                }
                catch (Exception e)
                {
                    trace.Error(e, "Non-fatal error shutting down socket");
                }
                UnderlyingSocket.Close();
            }
            finally
            {
                var onClosedEvent = OnClosed;
                if (onClosedEvent != null)
                    onClosedEvent(this, EventArgs.Empty);
            }
        }

        public void CloseRead()
        {
            Shutdown(SocketShutdown.Receive);
        }

        public void CloseWrite()
        {
            Shutdown(SocketShutdown.Send);
        }

        /// <summary>
        /// Marks a specific connection for graceful shutdown. The next receive or send to be posted
        /// will fail and close the connection.
        /// </summary>
        public void Shutdown(SocketShutdown socketShutdown)
        {
            try
            {
                trace.Debug("Shutting Down Socket to {0} Side = {1}", IPAddress, socketShutdown.ToString());
                UnderlyingSocket.Shutdown(socketShutdown);
            }
            catch (Exception e)
            {
                trace.Error(e, "Non-fatal error shuttind down socket");
            }
        }

        /// <summary>
        /// From the specified socket, asynchronously try and connect to the specified addr at the specified port.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="addr"></param>
        /// <param name="port"></param>
        public static Task ConnectAsync(Socket socket, IPAddress addr, int port)
        {
            var tcs = new TaskCompletionSource<int>();
            var args = new SocketAsyncEventArgs(); // don't need to cache since clients shouldn't make a ton of connections
            args.RemoteEndPoint = new IPEndPoint(addr, port);
            args.UserToken = tcs;
            args.Completed += (s, a) =>
            {
                CompleteAsyncIOOperation(((TaskCompletionSource<int>)a.UserToken), a, args0 => 0);
                a.Dispose();
            };
            if (!socket.ConnectAsync(args))
            {
                CompleteAsyncIOOperation(tcs, args, args0 => 0);
                args.Dispose();
            }

            return tcs.Task;
        }

        public static void CompleteAsyncIOOperation<T>(TaskCompletionSource<T> tcs, SocketAsyncEventArgs args, Func<SocketAsyncEventArgs, T> getResult)
        {
            args.UserToken = null;
            if (args.SocketError != SocketError.Success)
            {
                tcs.SetException(new SocketException((int)args.SocketError));
            }
            else
            {
                tcs.SetResult(getResult(args));
            }
        }
    }
}
