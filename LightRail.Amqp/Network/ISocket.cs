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
        /// A buffer pool used by the network layer for sending and receiving.
        /// </summary>
        IBufferPool BufferPool { get; }

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
        /// Asynchronously tries to read up to "count" bytes into the specified buffer writing at the specified offset.
        /// </summary>
        /// <param name="buffer">A buffer to write to.</param>
        /// <param name="offset">The offset in the buffer to start writing.</param>
        /// <param name="count">The maximum number of bytes to write into the buffer.</param>
        /// <returns>The actual number of bytes read into the buffer.</returns>
        Task<int> ReceiveAsync(byte[] buffer, int offset, int count);

        /// <summary>
        /// Fires when the underlying socket is closed.
        /// </summary>
        event EventHandler OnClosed;
    }
}
