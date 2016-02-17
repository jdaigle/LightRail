using System;
using System.Threading;

namespace LightRail.MessageBroker.Queuing
{
    /// <summary>
    /// An implementation based on "Implemented Lock-Free Queues" by John D. Valois
    /// http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.53.8674&rep=rep1&type=pdf
    /// 
    /// Other references:
    /// https://www.research.ibm.com/people/m/michael/podc-1996.pdf
    /// </summary>
    public class ConcurrentQueueEntryList
    {
        private static TraceSource trace = TraceSource.FromClass();

        public ConcurrentQueueEntryList()
        {
            head = tail = new QueueEntry(null, DateTime.MinValue, uint.MinValue, 0, QueueEntryStateEnum.ARCHIVED)
            {
                SeqNum = 0,
            };
        }

        /// <summary>
        /// The head of the queue is a sentinel. It's "Next"
        /// property is the first item in the queue or NULL.
        /// </summary>
        private volatile QueueEntry head;
        /// <summary>
        /// The "last" entry in the queue. Entries are enqueued here.
        /// </summary>
        private volatile QueueEntry tail;

        /// <summary>
        /// The head of the queue is always a fixed object. It's "Next"
        /// property is the first item in the queue.
        /// </summary>
        public QueueEntry Head { get { return head; } }

        public QueueEntry Enqueue(object item)
        {
            var n = new QueueEntry(item, DateTime.UtcNow, uint.MaxValue, 0, QueueEntryStateEnum.AVAILABLE);
            while (true)
            {
                // p = previous node
                var p = tail;
                var p_next = p.Next;
                if (ReferenceEquals(p, tail)) //  Are tail and next consistent?
                {
                    if (p_next == null) // Was Tail pointing to the last node?
                    {
                        n.SeqNum = p.SeqNum + 1;
                        if (p.CompareAndSwapNext(n, p_next)) // compare and swap, returns if successful
                        {
                            // Enqueue is done. Try to swing Tail to the inserted node
                            if (Interlocked.CompareExchange(ref tail, n, p) != p)
                                System.Diagnostics.Debug.Fail("CAS(tail, p, n) failed!");
                            return n; // exit loop
                        }
                    }
                    else
                    {
                        // Tail was not pointing to the last node
                        // Try to swing Tail to the next node
                        Interlocked.CompareExchange(ref tail, p_next, p);
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
