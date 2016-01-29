using LightRail.Amqp.Types;

namespace LightRail.Amqp.Messaging
{
    /// <summary>
    /// Transport headers for a message.
    /// </summary>
    /// <remarks>
    /// <type name="header" class="composite" source="list" provides="section">
    ///     <descriptor name = "amqp:header:list" code="0x00000000:0x00000070"/>
    ///     <field name = "durable" type="boolean" default="false"/>
    ///     <field name = "priority" type="ubyte" default="4"/>
    ///     <field name = "ttl" type="milliseconds"/>
    ///     <field name = "first-acquirer" type="boolean" default="false"/>
    ///     <field name = "delivery-count" type="uint" default="0"/>
    /// </type>
    /// 
    /// The header section carries standard delivery details about the transfer of a message through the AMQP network.
    /// If the header section is omitted the receiver MUST assume the appropriate default values(or the meaning implied
    /// by no value being set) for the fields within the header unless other target or node specific defaults have otherwise
    /// been set.
    /// </remarks>
    public class Header : DescribedList
    {
        public Header() : base(MessagingDescriptors.Header) { }

        /// <summary>
        /// Durability Requirements
        /// </summary>
        /// <remarks>
        /// Durable messages MUST NOT be lost even if an intermediary is unexpectedly terminated and
        /// restarted. A target which is not capable of fulfilling this guarantee MUST NOT accept messages
        /// where the durable header is set to true: if the source allows the rejected outcome then the
        /// message SHOULD be rejected with the precondition-failed error, otherwise the link MUST be
        /// detached by the receiver with the same error
        /// </remarks>
        [AmqpDescribedListIndex(0)]
        public bool Durable { get; set; } = false;

        /// <summary>
        /// Relative message priority.
        /// </summary>
        [AmqpDescribedListIndex(1)]
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
        [AmqpDescribedListIndex(2)]
        public uint TTL { get; set; } = uint.MaxValue;

        /// <summary>
        /// If this value is true, then this message has not been acquired by any other link (see section 3.3). If
        /// this value is false, then this message MAY have previously been acquired by another link or links.
        /// </summary>
        [AmqpDescribedListIndex(3)]
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
        [AmqpDescribedListIndex(4)]
        public uint DeliveryCount { get; set; } = 0;
    }
}