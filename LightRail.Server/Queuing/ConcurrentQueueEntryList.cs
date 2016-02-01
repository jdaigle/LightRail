using System;
using System.Threading;

namespace LightRail.Server.Queuing
{
    public class ConcurrentQueueEntryList
    {
        private static TraceSource trace = TraceSource.FromClass();

        public ConcurrentQueueEntryList()
        {
            head = tail = new QueueEntry()
            {
                SeqNum = 1,
            };
        }

        /// <summary>
        /// The "first" entry in the queue. This is the entry that is popped.
        /// </summary>
        private volatile QueueEntry head;
        /// <summary>
        /// The "last" entry in the queue. Entries are enqueued here.
        /// </summary>
        private volatile QueueEntry tail;

        public QueueEntry Head { get { return head; } }

        public QueueEntry Enqueue(object item)
        {
            var entry = new QueueEntry();
            entry.QueueEntryList = this;
            entry.EnqueueDateTime = DateTime.UtcNow;
            for (;;)
            {
                var prevTail = tail;
                var prevTailNext = prevTail.Next;
                if (prevTail == tail)
                {
                    if (prevTailNext == null)
                    {
                        entry.SeqNum = prevTail.SeqNum + 1;
                        if (prevTail.TrySetNext(entry, null)) // compare and swap, returns if successful
                        {
                            // it shouldn't be possible for two threads to get here at the same time
                            Interlocked.CompareExchange(ref tail, entry, tail); // compare and swap
                            return entry;
                        }
                    }
                    else
                    {
                        // the tail has been updated before we had a chance. so compare and swap
                        // if it fails... that's okay
                        Interlocked.CompareExchange(ref tail, prevTailNext, prevTail);
                    }
                }
            }
        }

        private volatile QueueEntry unscavengedEntry;
        private volatile int scavenges = 0;

        /// <summary>
        /// When an entry is archived, we need to attempt scavenging to cleanup the list
        /// </summary>
        public void OnEntryArchived(QueueEntry entry)
        {
            var next = head.Next;
            var newNext = head.GetNextValidEntry();

            if (next == newNext)
            {
                // the head of the queue has not been archived, hence the archival must have been mid queue.

                // so update unscavengedEntry if entry is further back in the queue than the current unscavengedEntry value
                var currentUnscavengedEntry = unscavengedEntry;
                while (currentUnscavengedEntry == null || currentUnscavengedEntry.CompareTo(entry) < 0)
                {
                    Interlocked.CompareExchange(ref unscavengedEntry, entry, currentUnscavengedEntry);
                    currentUnscavengedEntry = unscavengedEntry;
                }

                // only going to scavenge() after N entries have been scavenged
                if (Interlocked.Increment(ref scavenges) > 10)
                {
                    Interlocked.Exchange(ref scavenges, 0);
                    Scavenge();
                }
            }
            else
            {
                // the head has been scavenged
                var currentUnscavengedEntry = unscavengedEntry;
                if (currentUnscavengedEntry != null && (next == null || currentUnscavengedEntry.CompareTo(next) < 0))
                {
                    Interlocked.CompareExchange(ref unscavengedEntry, null, currentUnscavengedEntry);
                    currentUnscavengedEntry = unscavengedEntry;
                }
            }
        }

        private void Scavenge()
        {
            trace.Debug("Scavenging Archived Entries...");
            var hwm = Interlocked.Exchange(ref unscavengedEntry, null);
            var next = head.GetNextValidEntry();
            int scavengedCount = 0;
            if (hwm != null)
            {
                // start at the head, loop until we get to the record we want to scavange
                while (next != null && hwm.CompareTo(next) > 0)
                {
                    scavengedCount++;
                    // GetNextValidEntry() will progress the pointer AND remove Archived entries from the linked list
                    next = next.GetNextValidEntry();
                }
            }
            trace.Debug("Scavenged {0} entries", scavengedCount.ToString());
        }
    }
}
