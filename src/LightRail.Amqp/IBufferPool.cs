namespace LightRail.Amqp
{
    /// <summary>
    /// An abstraction for a buffer pool. Each "buffer" is of a fixed maximum size,
    /// usually the maximum frame size of the AMQP connection.
    /// </summary>
    public interface IBufferPool
    {
        /// <summary>
        /// Gets a free ByteBuffer from the underlying pool. Returns true
        /// if the buffer is available, and false if no buffer is available.
        /// </summary>
        bool TryGetByteBuffer(out ByteBuffer buffer);

        /// <summary>
        /// Returns the ByteBuffer back to the pool of available buffers.
        /// </summary>
        void FreeBuffer(ByteBuffer buffer);
    }
}
