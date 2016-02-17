using System;
using System.Threading;

namespace LightRail.MessageBroker.Queuing
{
    public class QueueEntry
    {
        public QueueEntry(object item, DateTime enqueuedDateTime, uint ttl, int initialDeliveryCount, QueueEntryStateEnum initialState)
        {
            Item = item;
            EnqueueDateTime = enqueuedDateTime;
            TTL = ttl;
            deliveryCount = initialDeliveryCount;
            state = (int)initialState;
        }

        /// <summary>
        /// Monotonically increasing sequence number (can wrap) to indicate order in which the entry was was enqueued.
        /// </summary>
        public uint SeqNum { get; set; }


        private volatile int state = (int)QueueEntryStateEnum.AVAILABLE;
        public bool IsAvailable { get { return state == (int)QueueEntryStateEnum.AVAILABLE; } }
        public bool IsArchived { get { return state == (int)QueueEntryStateEnum.ARCHIVED; } }
        public bool IsAcquired { get { return state == (int)QueueEntryStateEnum.ACQUIRED; } }

        /// <summary>
        /// The DateTime in which the entry was enqueued.
        /// </summary>
        public DateTime EnqueueDateTime { get; }

        /// <summary>
        /// The queued item.
        /// </summary>
        public object Item { get; }

        /// <summary>
        /// Time to live in milliseconds.
        /// </summary>
        public uint TTL { get; }

        /// <summary>
        /// Returns true of the TTL has expired this message.
        /// </summary>
        public bool IsExpired { get { return EnqueueDateTime.AddMilliseconds(TTL) < DateTime.UtcNow; } }

        private volatile int deliveryCount = 0;
        /// <summary>
        /// The number of prior unsuccessful delivery attempts.
        /// </summary>
        public uint DeliveryCount { get { return (uint)deliveryCount; } }

        private volatile QueueEntry next;
        /// <summary>
        /// Returns the current next QueueEntry from the ConcurrentQueueEntryList
        /// </summary>
        public QueueEntry Next { get { return next; } }

        /// <summary>
        /// Wraps up call to Interlocked.CompareExchange to compare & swap a new
        /// "next" QueueEntry pointer.
        /// 
        /// Returns true if the compare & swap succeeded, false other.
        /// </summary>
        public bool CompareAndSwapNext(QueueEntry n_next, QueueEntry p_next)
        {
            return Interlocked.CompareExchange(ref next, n_next, p_next) == p_next;
        }

        /// <summary>
        /// Returns the next non-archived entry. Or null. This method a the side-effect
        /// of updating "Next" to "Next.Next" in a loop whenver it is ARCHIVED.
        /// </summary>
        public QueueEntry GetNextValidEntry()
        {
            var currentNext = next;

            // loop, replacing "next" until we reach a non-archived entry, or null
            while (currentNext != null && (currentNext.IsArchived || currentNext.IsExpired))
            {
                var newNext = currentNext.Next;
                if (newNext != null)
                {
                    Interlocked.CompareExchange(ref next, newNext, currentNext);
                    currentNext = next;
                }
                else
                {
                    currentNext = null;
                }
            }

            return currentNext;
        }

        /// <summary>
        /// Compares the sequence number of this QueueEntry to another QueueEntry for ordering purposes.
        /// </summary>
        public int CompareTo(QueueEntry item)
        {
            // TODO: sequence number wrapping
            return SeqNum > item.SeqNum ? 1 : SeqNum < item.SeqNum ? -1 : 0;
        }

        /// <summary>
        /// Attempts to Acquire the entry by the specified consumer. Returns true of success, otherwise failure.
        /// </summary>
        public bool TryAcquire(Consumer consumer)
        {
            // can only acquire if available
            var acquired =
                Interlocked.CompareExchange(ref state, (int)QueueEntryStateEnum.ACQUIRED, (int)QueueEntryStateEnum.AVAILABLE)
                    == (int)QueueEntryStateEnum.AVAILABLE;
            return acquired;
        }

        /// <summary>
        /// Archives the current entry. It will not be delivered again, and may be scavenged.
        /// </summary>
        public void Archive()
        {
            Interlocked.Exchange(ref state, (int)QueueEntryStateEnum.ARCHIVED);
        }

        /// <summary>
        /// Releases the current entry to be delivered again. It may also increment the delivery count.
        /// </summary>
        public void Release(bool incrementDeliveryCount)
        {
            Interlocked.Exchange(ref state, (int)QueueEntryStateEnum.AVAILABLE);
            if (incrementDeliveryCount)
                Interlocked.Increment(ref deliveryCount);
        }
    }
}
