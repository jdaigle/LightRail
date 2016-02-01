using System;
using System.Threading;

namespace LightRail.Server.Queuing
{
    public class QueueEntry
    {
        public uint SeqNum { get; set; }

        public ConcurrentQueueEntryList QueueEntryList { get; set; }
        private volatile QueueEntry next;

        private volatile int state = (int)QueueEntryStateEnum.AVAILABLE;
        public bool IsAvailable { get { return state == (int)QueueEntryStateEnum.AVAILABLE; } }
        public bool IsArchived { get { return state == (int)QueueEntryStateEnum.ARCHIVED; } }
        public bool IsAcquired { get { return state == (int)QueueEntryStateEnum.ACQUIRED; } }

        private volatile int deliveryCount = 0;

        /// <summary>
        /// The DateTime in which the entry was enqueued.
        /// </summary>
        public DateTime EnqueueDateTime { get; set; }

        /// <summary>
        /// Time to live in milliseconds.
        /// </summary>
        public uint TTL { get; set; } = uint.MaxValue;

        /// <summary>
        /// The number of prior unsuccessful delivery attempts.
        /// </summary>
        public uint DeliveryCount { get { return (uint)deliveryCount; } }

        /// <summary>
        /// Returns true of the TTL has expired this message.
        /// </summary>
        public bool IsExpired { get { return EnqueueDateTime.AddMilliseconds(TTL) < DateTime.UtcNow; } }

        public bool TrySetNext(QueueEntry entry, QueueEntry previous)
        {
            return Interlocked.CompareExchange(ref next, entry, previous) == previous;
        }

        /// <summary>
        /// Returns the next QueueEntry from the ConcurrentQueueEntryList
        /// </summary>
        public QueueEntry Next { get { return next; } }

        /// <summary>
        /// Returns the next non-archived entry. Or null. This method has the side-effect
        /// of updating .Next in case the current .Next is ARCHIVED.
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

        public int CompareTo(QueueEntry item)
        {
            return SeqNum > item.SeqNum ? 1 : SeqNum < item.SeqNum ? -1 : 0;
        }

        /// <summary>
        /// Attempts to Acquire the entry by the specified consumer. Returns true of success, otherwise failure.
        /// </summary>
        public bool TryAcquire(Consumer consumer)
        {
            // can only acquire if available
            var acquired = Interlocked.CompareExchange(ref state, (int)QueueEntryStateEnum.ACQUIRED, (int)QueueEntryStateEnum.AVAILABLE) == (int)QueueEntryStateEnum.AVAILABLE;
            if (acquired)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Archives the current entry. It will not be delivered again, and may be scavenged.
        /// </summary>
        public void Archive()
        {
            Interlocked.Exchange(ref state, (int)QueueEntryStateEnum.ARCHIVED);
            QueueEntryList.OnEntryArchived(this);
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
