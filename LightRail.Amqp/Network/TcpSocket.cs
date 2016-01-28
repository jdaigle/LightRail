using System;
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
