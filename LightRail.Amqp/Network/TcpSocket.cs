using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace LightRail.Amqp.Network
{
    public class TcpSocket
    {
        /// <summary>
        /// From the specified socket, asynchronously tries to read up to "count" bytes into the specified buffer writing at the specified offset.
        /// </summary>
        /// <param name="socket">The socket to read from.</param>
        /// <param name="args">Pool SocketAsyncEventArgs instance. Must have Completed event set prior to this method call.</param>
        /// <param name="buffer">A buffer to write to.</param>
        /// <param name="offset">The offset in the buffer to start writing.</param>
        /// <param name="count">The maximum number of bytes to write into the buffer.</param>
        /// <returns>The actual number of bytes read into the buffer.</returns>
        public static Task<int> ReceiveAsync(Socket socket, SocketAsyncEventArgs args, byte[] buffer, int offset, int count)
        {
            // The buffer should come from the underlying pinned buffer pool
            var tcs = new TaskCompletionSource<int>();
            args.SetBuffer(buffer, offset, count);
            args.UserToken = tcs;
            if (socket.ReceiveAsync(args) == false)
            {
                CompleteAsyncIOOperation(tcs, args, a => a.BytesTransferred);
            }
            return tcs.Task;
        }

        public static Task<int> SendAsync(Socket socket, SocketAsyncEventArgs args, byte[] buffer, int offset, int count)
        {
            // The buffer should come from the underlying pinned buffer pool
            var tcs = new TaskCompletionSource<int>();
            args.SetBuffer(buffer, offset, count);
            var token = new SendAsyncBufferToken<int>
            {
                socket = socket,
                taskCompletionSource = tcs,
                buffer = buffer,
                offset = offset,
                count = count,
                bytesTransferred = 0,
            };
            args.UserToken = token;
            if (socket.SendAsync(args) == false)
            {
                CompleteAsyncIOOperation(token, args, a => a.bytesTransferred);
            }
            return tcs.Task;
        }

        public class SendAsyncBufferToken<T>
        {
            public Socket socket;
            public TaskCompletionSource<T> taskCompletionSource;
            public byte[] buffer;
            public int offset;
            public int count;
            public int bytesTransferred;
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

        public static void CompleteAsyncIOOperation<T>(SendAsyncBufferToken<T> token, SocketAsyncEventArgs args, Func<SendAsyncBufferToken<T>, T> getResult)
        {
            completeAgain:
            var tcs = token.taskCompletionSource;
            args.UserToken = null;
            if (args.SocketError != SocketError.Success)
            {
                tcs.SetException(new SocketException((int)args.SocketError));
            }
            else
            {
                token.bytesTransferred += args.BytesTransferred;
                if (token.bytesTransferred < token.count)
                {
                    // send again
                    args.SetBuffer(token.buffer, token.offset + token.bytesTransferred, token.count - token.bytesTransferred);
                    args.UserToken = token;
                    if (!token.socket.ConnectAsync(args))
                    {
                        // operation completed synchronously
                        goto completeAgain; // loop instead of calling self (shields against a stackoverflow);
                    }
                }
                else
                {
                    tcs.SetResult(getResult(token));
                }
            }
        }
    }
}
