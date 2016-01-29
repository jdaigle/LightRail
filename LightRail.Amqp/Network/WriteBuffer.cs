using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace LightRail.Amqp.Network
{
    public class WriteBuffer
    {
        public WriteBuffer(ISocket socket)
        {
            this.socket = socket;
            flushLoopTask = Task.Factory.StartNew(FlushLoop);
        }

        private Task flushLoopTask;
        private readonly ISocket socket;
        private readonly ConcurrentQueue<ByteBuffer> buffers = new ConcurrentQueue<ByteBuffer>();
        private readonly AutoResetEvent flushSignal = new AutoResetEvent(false);

        private Exception handledException;

        public void Write(byte[] buffer, int offset, int count)
        {
            Write(new ByteBuffer(buffer, offset, count, count, false));
        }

        public void Write(ByteBuffer buffer)
        {
            if (handledException != null)
            {
                throw new InvalidOperationException("WriteBuffer loop has stopped due to an exception.", handledException);
            }
            buffers.Enqueue(buffer);
            flushSignal.Set();
        }

        private void FlushLoop()
        {
            while (true)
            {
                try
                {
                    ByteBuffer buffer;
                    if (!buffers.TryDequeue(out buffer))
                    {
                        flushSignal.WaitOne(); // wait for someone to enqueue a buffer to write
                        continue;
                    }
                    socket.SendAsync(buffer).Wait();
                }
                catch (Exception ex)
                {
                    // TODO: log and bubble up
                    handledException = ex;
                    return;
                }
            }
        }
    }
}
