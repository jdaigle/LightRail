﻿using System;
using System.Threading.Tasks;

namespace LightRail.MessageBroker.Queuing
{
    public abstract class Consumer
    {
        private static TraceSource trace = TraceSource.FromClass();

        public Consumer(ConcurrentQueue queue)
        {
            this.queue = queue;
            this.queue.RegisterConsumer(this);
        }

        private readonly object syncRoot = new object();

        protected readonly ConcurrentQueue queue;
        private volatile QueueEntry lastEntry;

        public bool WillDeliver(QueueEntry entry)
        {
            lock (syncRoot)
            {
                // TODO is the queue still active?
                var lastEntrySeen = lastEntry;
                while (lastEntrySeen != null && !lastEntrySeen.IsAvailable) // TODO filter out entries that this subscription doesn't care about
                {
                    var nextEntry = lastEntrySeen.GetNextValidEntry(); // loop until we get to an available entry to deliver
                    if (nextEntry != null)
                    {
                        lastEntrySeen = lastEntry = nextEntry;
                    }
                    else
                    {
                        lastEntrySeen = null;
                    }
                }
                if (lastEntrySeen == entry)
                {
                    // If the first entry that subscription can process is the one we are trying to deliver to it, then we are
                    // good
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public void TryDelivery(QueueEntry head)
        {
            // TODO: check link credit to determine if we can deliver
            if (!HasCreditToDeliver())
                return;

            lock (syncRoot)
            {
                var next = head.GetNextValidEntry();
                while (next != null)
                {
                    if (next.IsAvailable)
                    {
                        // TODO: FILTER out entries we don't care about

                        if (queue.TryAcquire(next, this))
                            break; // if not acquired, we'll try again with the next one
                    }

                    // loop until we get an available entry to deliver
                    // or null
                    next = next.GetNextValidEntry();
                }

                if (next == null)
                    return; // nothing to deliver

                if (next != null)
                {
                    // message acquired and ready to be delivered
                    OnMessageAquired(next);
                }
            }
        }

        protected abstract bool HasCreditToDeliver();
        protected abstract void OnMessageAquired(QueueEntry next);
    }
}
