using System;
using System.Threading.Tasks;

namespace LightRail.Amqp.Network
{
    /// <summary>
    /// Abstracts the network layer I/O for the AMQP procotol classes
    /// </summary>
    public interface ISocket
    {
        /// <summary>
        /// Starts an async operations to read "count" bytes into a buffer
        /// from the underlying stream. The specified callback is called
        /// after "count" bytes have been read.
        /// </summary>
        void ReceiveAsync(int count, Action<ByteBuffer> callback);

        /// <summary>
        /// Queues up a write to the underlying socket.
        /// </summary>
        void Write(ByteBuffer byteBuffer);

        /// <summary>
        /// Sends the specified byte buffer on the socket.
        /// </summary>
        Task SendAsync(byte[] buffer, int offset, int count);

        /// <summary>
        /// Immediately closes the underlying socket and cleans up.
        /// </summary>
        void Close();

        /// <summary>
        /// Closes the read side of the socket.
        /// </summary>
        void CloseRead();

        /// <summary>
        /// Closes the write side of the socket.
        /// </summary>
        void CloseWrite();

        /// <summary>
        /// Fires when the underlying socket is closed.
        /// </summary>
        event EventHandler OnClosed;
    }
}
