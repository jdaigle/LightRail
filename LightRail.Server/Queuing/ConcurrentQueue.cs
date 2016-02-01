using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace LightRail.Server.Queuing
{
    public class ConcurrentQueue
    {
        private static TraceSource trace = TraceSource.FromClass();

        private readonly QueueLogWriter logWriter;

        private readonly ConcurrentQueueEntryList queueList = new ConcurrentQueueEntryList();

        private readonly ConcurrentBag<Consumer> consumers = new ConcurrentBag<Consumer>();

        private readonly AutoResetEvent messageDeliveryPumpSignal = new AutoResetEvent(false);
        private readonly RegisteredWaitHandle messageDeliveryPumpSignalWaitHandle;
        private volatile int messageDeliveryPumpLoopIsRunning = 0; // 0 = false, 1 = true

        public ConcurrentQueue(ushort queueID, QueueLogWriter logWriter)
        {
            QueueID = queueID;
            this.logWriter = logWriter;
            messageDeliveryPumpSignalWaitHandle
                = ThreadPool.RegisterWaitForSingleObject(messageDeliveryPumpSignal, (state, timedOut) => MessageDeliveryPumpLoop(), null, -1, false);
            messageDeliveryPumpSignal.Set(); // signal to start pump right away
            logWriter.WriteCreated(this);
        }

        public ushort QueueID { get; }

        public void Enqueue(object item)
        {
            var queueEntry = queueList.Enqueue(item);
            logWriter.WriteEnqueue(this, queueEntry);
            messageDeliveryPumpSignal.Set(); // signal to pump new message to consumers
        }

        public void Release(QueueEntry entry, bool incrementDeliveryCount)
        {
            entry.Release(incrementDeliveryCount);
            logWriter.WriteReleased(this, entry);
            messageDeliveryPumpSignal.Set(); // new message available, signal to pump to consumers
        }

        public void Archive(QueueEntry entry)
        {
            logWriter.WriteArchived(this, entry);
            entry.Archive();
        }

        public bool TryAcquire(QueueEntry entry, Consumer consumer)
        {
            return entry.TryAcquire(consumer);
        }

        public void RegisterConsumer(Consumer consumer)
        {
            lock (consumers) // lock to prevent looping while we're adding
            {
                consumers.Add(consumer);
            }
            messageDeliveryPumpSignal.Set(); // signal to start pump with new consumer
        }

        private void MessageDeliveryPumpLoop()
        {
            if (Interlocked.CompareExchange(ref messageDeliveryPumpLoopIsRunning, 1, 0) != 0)
            {
                return; // if the pump is already running, so we don't want to execute again
            }
            trace.Debug("MessageDeliveryPumpLoop Started.");
            try
            {
                while (true)
                {
                    var head = queueList.Head;
                    var next = head.GetNextValidEntry();
                    if (next == null)
                    {
                        trace.Debug("Queue Is Empty");
                        return; // no more work to do
                    }

                    lock (consumers) // lock to prevent adding new consumers
                    {
                        if (consumers.IsEmpty)
                        {
                            trace.Debug("Consumer List Is Empty");
                            return;
                        }
                    }

                    // Loop over consumers and attempt delivery
                    var consumersToIterate = new List<Consumer>(consumers);
                    foreach (var consumer in consumersToIterate)
                    {
                        consumer.TryDelivery(head);
                    }
                }
            }
            catch (Exception ex)
            {
                trace.Error(ex, "Error in MessageDeliveryPumpLoop()");
            }
            finally
            {
                Interlocked.Exchange(ref messageDeliveryPumpLoopIsRunning, 0); // indicate that the pump has stopped
                trace.Debug("MessageDeliveryPumpLoop Finished.");
            }
        }
    }
}
