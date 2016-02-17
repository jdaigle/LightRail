using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LightRail.MessageBroker.Queuing
{
    public class QueueLogWriter
    {
        private static TraceSource trace = TraceSource.FromClass();
        const string aof = @"c:\temp\queue.txt";

        private readonly ConcurrentQueue<string> buffer = new ConcurrentQueue<string>();
        private readonly AutoResetEvent flushSignal = new AutoResetEvent(false);
        private readonly RegisteredWaitHandle flushSignalWaitHandle;
        private volatile int flushIsRunning = 0; // 0 = false, 1 = true

        private FileStream fileStream;

        public QueueLogWriter()
        {
            File.Delete(aof);

            fileStream = new FileStream(aof, FileMode.OpenOrCreate, FileAccess.Write);

            flushSignalWaitHandle
                = ThreadPool.RegisterWaitForSingleObject(flushSignal, (state, timedOut) => Flush(), null, -1, false);
            flushSignal.Set(); // signal to start pump right away
        }

        public void WriteCreated(ConcurrentQueue queue)
        {
            buffer.Enqueue(
            string.Format("Queue {0} Created", queue.QueueID));
            flushSignal.Set();
        }

        public void WriteEnqueue(ConcurrentQueue queue, QueueEntry entry)
        {
            buffer.Enqueue(
            string.Format("Queue {0} Enqueued {1} DateTime {2} TTL {3} DeliveryCount {4}", queue.QueueID, entry.SeqNum, entry.EnqueueDateTime, entry.TTL, entry.DeliveryCount));
            flushSignal.Set();
        }

        public void WriteArchived(ConcurrentQueue queue, QueueEntry entry)
        {
            buffer.Enqueue(
            string.Format("Queue {0} Archived {1} DateTime {2} TTL {3} DeliveryCount {4}", queue.QueueID, entry.SeqNum, entry.EnqueueDateTime, entry.TTL, entry.DeliveryCount));
            flushSignal.Set();
        }

        private void Flush()
        {
            if (Interlocked.CompareExchange(ref flushIsRunning, 1, 0) != 0)
            {
                return;
            }
            try
            {
                while (true)
                {
                    string log;
                    if (!buffer.TryDequeue(out log))
                    {
                        return;
                    }
                    var b = Encoding.UTF8.GetBytes(log+Environment.NewLine);
                    fileStream.Write(b, 0, b.Length);
                    fileStream.Flush(true);
                }
            }
            catch (Exception ex)
            {
                // TODO: log and bubble up
                trace.Error(ex);
                return;
            }
            finally
            {
                Interlocked.Exchange(ref flushIsRunning, 0);
            }
        }
    }
}
