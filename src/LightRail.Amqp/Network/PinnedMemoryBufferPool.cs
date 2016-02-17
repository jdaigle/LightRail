using System.Collections.Concurrent;

namespace LightRail.Amqp.Network
{
    /// <summary>
    /// This class creates a single large buffer which can be divided up 
    /// and assigned to SocketAsyncEventArgs objects for use with each 
    /// socket I/O operation.
    /// 
    /// This enables bufffers to be easily reused and guards against 
    /// fragmenting heap memory.
    /// 
    /// The operations exposed on the PinnedMemoryBufferPool class _are_ thread safe.
    /// </summary>
    public class PinnedMemoryBufferPool : IBufferPool
    {
        private readonly int totalBufferSize;
        private readonly byte[] bufferPool;
        private readonly ConcurrentStack<int> bufferOffsetPool;
        private readonly int bufferBlockSize;

        public PinnedMemoryBufferPool(int totalBytes, int bufferBlockSize)
        {
            totalBufferSize = totalBytes;
            bufferPool = new byte[totalBufferSize];
            this.bufferBlockSize = bufferBlockSize;
            bufferOffsetPool = new ConcurrentStack<int>();

            int offset = 0;
            while (offset < (totalBufferSize - bufferBlockSize))
            {
                bufferOffsetPool.Push(offset);
                offset += bufferBlockSize;
            }
        }

        public bool TryGetByteBuffer(out ByteBuffer buffer)
        {
            int offset;
            if (!bufferOffsetPool.TryPop(out offset))
            {
                buffer = null;
                return false;
            }
            buffer = new ByteBuffer(bufferPool, offset, 0, bufferBlockSize, false);
            return true;
        }

        public void FreeBuffer(ByteBuffer buffer)
        {
            bufferOffsetPool.Push(buffer.StartOffset);
        }
    }
}
