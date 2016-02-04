using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace LightRail.Amqp.Network
{
    public class WriteBuffer
    {
        private static readonly TraceSource trace = TraceSource.FromClass();

        public WriteBuffer(ISocket socket)
        {
            this.socket = socket;
            socket.OnClosed += OnSocketClosed;
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;
            flushLoopTask = Task.Factory.StartNew(FlushLoop, cancellationTokenSource.Token);
        }

        private Task flushLoopTask;
        private readonly ISocket socket;
        private readonly ConcurrentQueue<ByteBuffer> buffers = new ConcurrentQueue<ByteBuffer>();
        private readonly AutoResetEvent flushSignal = new AutoResetEvent(false);
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly CancellationToken cancellationToken;

        private Exception handledException;

        public void Write(ByteBuffer buffer)
        {
            if (handledException != null)
            {
                throw new InvalidOperationException("WriteBuffer loop has stopped due to an exception.", handledException);
            }
            buffers.Enqueue(buffer);
            flushSignal.Set();
            while (!buffers.IsEmpty)
            {
                Thread.Sleep(10);
            }
        }

        private void FlushLoop()
        {
            trace.Debug("WriteBuffer Flush Loop Started");
            try
            {
                while (true)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;
                    try
                    {
                        ByteBuffer buffer;
                        if (!buffers.TryDequeue(out buffer))
                        {
                            flushSignal.WaitOne(); // wait for someone to enqueue a buffer to write
                            continue;
                        }
                        if (cancellationToken.IsCancellationRequested)
                            return;
                        socket.SendAsync(buffer.Buffer, buffer.ReadOffset, buffer.LengthAvailableToRead).Wait();
                    }
                    catch (Exception ex)
                    {
                        // TODO: log and bubble up
                        trace.Error(ex, "WriteBuffer Flush Loop Error");
                        handledException = ex;
                        return;
                    }
                }
            }
            finally
            {
                cancellationTokenSource.Dispose();
                flushSignal.Dispose();
                trace.Debug("WriteBuffer Flush Loop Finished");
            }
        }

        private void OnSocketClosed(object sender, EventArgs args)
        {
            Stop();
        }

        public void Stop()
        {
            try
            {
                cancellationTokenSource.Cancel();
                flushSignal.Set(); // signal to force a loop and exit
            }
            finally
            {
                socket.OnClosed -= OnSocketClosed;
            }
        }
    }
}

