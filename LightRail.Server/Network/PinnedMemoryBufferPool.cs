using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LightRail.Server.Network
{
    /// <summary>
    /// This class creates a single large buffer which can be divided up 
    /// and assigned to SocketAsyncEventArgs objects for use with each 
    /// socket I/O operation.  
    /// This enables bufffers to be easily reused and guards against 
    /// fragmenting heap memory.
    /// 
    /// The operations exposed on the PinnedMemoryBufferPool class _are_ thread safe.
    /// </summary>
    public class PinnedMemoryBufferPool
    {
        private readonly int totalBufferSize;
        private readonly byte[] bufferPool;
        private readonly ConcurrentStack<int> freeBufferOffsets;
        private readonly int bufferSize;
        private int nextBufferOffset;
        private readonly object nextBufferOffsetLock = new object();

        /// <param name="totalBytes">The total number of bytes allocated to the pool.</param>
        /// <param name="bufferSize">The size of each buffer.</param>
        public PinnedMemoryBufferPool(int totalBytes, int bufferSize)
        {
            totalBufferSize = totalBytes;
            bufferPool = new byte[totalBufferSize];
            nextBufferOffset = 0;
            this.bufferSize = bufferSize;
            freeBufferOffsets = new ConcurrentStack<int>();
        }

        /// <summary>
        /// Sets the buffer of the specified SocketAsyncEventArgs. Returns true if the operation succeeded. False otherwise.
        /// </summary>
        public bool SetBuffer(SocketAsyncEventArgs args)
        {
            int offset;
            if (!freeBufferOffsets.TryPop(out offset))
            {
                lock (nextBufferOffsetLock)
                {
                    if ((totalBufferSize - bufferSize) < nextBufferOffset)
                    {
                        return false;
                    }
                    offset = nextBufferOffset += bufferSize;
                }
            }
            args.SetBuffer(bufferPool, offset, bufferSize);
            return true;
        }

        /// <summary>
        /// Releases the buffer used for the SocketAsyncEventArgs.
        /// </summary>
        /// <param name="args"></param>
        public void FreeBuffer(SocketAsyncEventArgs args)
        {
            freeBufferOffsets.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }
    }
}
