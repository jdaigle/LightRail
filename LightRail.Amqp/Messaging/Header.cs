using System;
using LightRail.Amqp.Types;

namespace LightRail.Amqp.Messaging
{
    /// <summary>
    /// Transport headers for a message.
    /// </summary>
    /// <remarks>
    /// The header section carries standard delivery details about the transfer of a message through the AMQP network.
    /// If the header section is omitted the receiver MUST assume the appropriate default values(or the meaning implied
    /// by no value being set) for the fields within the header unless other target or node specific defaults have otherwise
    /// been set.
    /// </remarks>
    public class Header : DescribedList
    {
        public Header()
            :base(Descriptor.Header)
        {
        }

        /// <summary>
        /// Durability Requirements
        /// </summary>
        /// <remarks>
        /// Durable messages MUST NOT be lost even if an intermediary is unexpectedly terminated and
        /// restarted.A target which is not capable of fulfilling this guarantee MUST NOT accept messages
        /// where the durable header is set to true: if the source allows the rejected outcome then the
        /// message SHOULD be rejected with the precondition-failed error, otherwise the link MUST be
        /// detached by the receiver with the same error
        /// </remarks>
        public bool Durable { get; set; } = false;

        /// <summary>
        /// Relative message priority.
        /// </summary>
        public byte Priority { get; set; } = 4;

        /// <summary>
        /// Time to live in milliseconds.
        /// </summary>
        /// <remarks>
        /// Duration in milliseconds for which the message is to be considered “live”. If this is set then
        /// a message expiration time will be computed based on the time of arrival at an intermediary.
        /// Messages that live longer than their expiration time will be discarded (or dead lettered). When a
        /// message is transmitted by an intermediary that was received with a ttl, the transmitted message’s
        /// header SHOULD contain a ttl that is computed as the difference between the current time and the
        /// formerly computed message expiration time, i.e., the reduced ttl, so that messages will eventually
        /// die if they end up in a delivery loop.
        /// </remarks>
        public uint TTL { get; set; } = uint.MaxValue;

        /// <summary>
        /// If this value is true, then this message has not been acquired by any other link (see section 3.3). If
        /// this value is false, then this message MAY have previously been acquired by another link or links.
        /// </summary>
        public bool FirstAcquirer { get; set; } = false;

        /// <summary>
        /// The number of prior unsuccessful delivery attempts.
        /// </summary>
        /// <remarks>
        /// The number of unsuccessful previous attempts to deliver this message. If this value is non-zero
        /// it can be taken as an indication that the delivery might be a duplicate. On first delivery, the value
        /// is zero. It is incremented upon an outcome being settled at the sender, according to rules defined
        /// for each outcome.
        /// </remarks>
        public uint DeliveryCount { get; set; } = 0;

        protected override int CalculateListSize()
        {
            return 5;
        }

        protected override void EncodeListItem(ByteBuffer buffer, int index, bool arrayEncoding)
        {
            switch (index)
            {
                case 0:
                    Encoder.WriteBoolean(buffer, Durable, arrayEncoding);
                    return;
                case 1:
                    Encoder.WriteUByte(buffer, Priority);
                    return;
                case 2:
                    Encoder.WriteUInt(buffer, TTL, arrayEncoding);
                    return;
                case 3:
                    Encoder.WriteBoolean(buffer, FirstAcquirer, arrayEncoding);
                    return;
                case 4:
                    Encoder.WriteUInt(buffer, DeliveryCount, true);
                    return;
                default:
                    throw new InvalidOperationException("Too Many Fields");
            }
        }

        protected override void DecodeListItem(ByteBuffer buffer, int index)
        {
            throw new NotImplementedException();
        }
    }
}