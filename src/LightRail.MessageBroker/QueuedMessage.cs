using System;

namespace LightRail.MessageBroker
{
    /// <summary>
    /// An immutable structure representing a QueuedMessage. Modifications result in a new QueueMessage.
    /// </summary>
    public class QueuedMessage
    {
        public static readonly QueuedMessage Empty = new QueuedMessage(Guid.Empty, null, DateTime.MinValue);

        public QueuedMessage(
            Guid id
            , object body
            , DateTime enqueuedDateTime
            , uint? ttl = null
            , MessageStatus status = MessageStatus.AVAILABLE
            , byte failedDeliveryCount = 0)
        {
            ID = id;
            Body = body;
            EnqueuedDateTime = enqueuedDateTime;
            ExpiresDateTime = ttl.HasValue ? enqueuedDateTime.AddMilliseconds(ttl.Value) : DateTime.MaxValue;
            Status = status;
            FailedDeliveryCount = failedDeliveryCount;
        }

        public Guid ID { get; }

        public MessageStatus Status { get; }

        public DateTime EnqueuedDateTime { get; }

        /// <remarks>
        /// Duration in milliseconds for which the message is to be considered “live”. If this is set then
        /// a message expiration time will be computed based on the time of arrival at an intermediary.
        /// Messages that live longer than their expiration time will be discarded (or dead lettered). When a
        /// message is transmitted by an intermediary that was received with a ttl, the transmitted message’s
        /// header SHOULD contain a ttl that is computed as the difference between the current time and the
        /// formerly computed message expiration time, i.e., the reduced ttl, so that messages will eventually
        /// die if they end up in a delivery loop.
        /// </remarks>
        public DateTime ExpiresDateTime { get; }

        /// <summary>
        /// the number of prior unsuccessful delivery attempts
        /// </summary>
        /// <remarks>
        /// The number of unsuccessful previous attempts to deliver this message. If this value is non-zero
        /// it can be taken as an indication that the delivery might be a duplicate.On first delivery, the value
        /// is zero.It is incremented upon an outcome being settled at the sender, according to rules defined
        /// for each outcome.
        /// </remarks>
        public byte FailedDeliveryCount { get; }

        public object Body { get; }
    }
}
