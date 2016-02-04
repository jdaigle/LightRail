using System;
using System.Collections.Concurrent;
using System.Threading;

namespace LightRail.Amqp.Network
{
    public class WriteBuffer
    {
        private static readonly TraceSource trace = TraceSource.FromClass();

        public WriteBuffer(Action<ByteBuffer, Action> writeToSocketAsync)
        {
            this.writeToSocketAsync = writeToSocketAsync;
            flushSignalWaitHandle = ThreadPool.RegisterWaitForSingleObject(flushSignal, (state, timedOut) => Flush(), null, -1, false);
            flushSignal.Set(); // signal to start pump right away
        }

        private readonly ConcurrentQueue<ByteBuffer> bufferQueue = new ConcurrentQueue<ByteBuffer>();
        private readonly AutoResetEvent flushSignal = new AutoResetEvent(false);
        private readonly RegisteredWaitHandle flushSignalWaitHandle;
        private volatile int flushIsRunning = 0; // 0 = false, 1 = true
        private readonly Action<ByteBuffer, Action> writeToSocketAsync;

        private Exception handledException;

        public void Write(ByteBuffer buffer)
        {
            if (handledException != null)
            {
                throw new InvalidOperationException("WriteBuffer loop has stopped due to an exception.", handledException);
            }
            bufferQueue.Enqueue(buffer);
            flushSignal.Set();
        }

        private void Flush()
        {
            if (Interlocked.CompareExchange(ref flushIsRunning, 1, 0) != 0)
                return;
            TryFlushAnother();
        }

        private void TryFlushAnother()
        {
            try
            {
                ByteBuffer buffer;
                if (!bufferQueue.TryDequeue(out buffer))
                {
                    // possible race condition where Enqueue and Set() are called before we return
                    Interlocked.Exchange(ref flushIsRunning, 0);
                    return;
                }
                writeToSocketAsync(buffer, TryFlushAnother);
            }
            catch (Exception ex)
            {
                trace.Error(ex, "WriteBuffer Flush Loop Error");
                handledException = ex;
            }
        }
    }
}

