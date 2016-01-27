using LightRail.Amqp.Types;

namespace LightRail.Amqp.Framing
{
    /// <summary>
    /// Attach a link to a session.
    /// 
    /// <type name = "attach" class="composite" source="list" provides="frame">
    ///     <descriptor name = "amqp:attach:list" code="0x00000000:0x00000012"/>
    ///     <field name = "name" type="string" mandatory="true"/>
    ///     <field name = "handle" type="handle" mandatory="true"/>
    ///     <field name = "role" type="role" mandatory="true"/>
    ///     <field name = "snd-settle-mode" type="sender-settle-mode" default="mixed"/>
    ///     <field name = "rcv-settle-mode" type="receiver-settle-mode" default="first"/>
    ///     <field name = "source" type="*" requires="source"/>
    ///     <field name = "target" type="*" requires="target"/>
    ///     <field name = "unsettled" type="map"/>
    ///     <field name = "incomplete-unsettled" type="boolean" default="false"/>
    ///     <field name = "initial-delivery-count" type="sequence-no"/>
    ///     <field name = "max-message-size" type="ulong"/>
    ///     <field name = "offered-capabilities" type="symbol" multiple="true"/>
    ///     <field name = "desired-capabilities" type="symbol" multiple="true"/>
    ///     <field name = "properties" type="fields"/>
    /// </type>
    /// 
    /// The attach frame indicates that a link endpoint has been attached to the session.
    /// </summary>
    public sealed class Attach : AmqpFrame
    {
        public Attach() : base(DescribedListCodec.Attach) { }

        /// <summary>
        /// This name uniquely identifies the link from the container of the source to the container of the target
        /// node, e.g., if the container of the source node is A, and the container of the target node is B, the
        /// link MAY be globally identified by the(ordered) tuple(A, B,<name>).
        /// </summary>
        [AmqpDescribedListIndex(0)]
        public string Name { get; set; }

        /// <summary>
        /// The numeric handle assigned by the the peer as a shorthand to refer to the link in all performatives
        /// that reference the link until the it is detached.
        /// 
        /// The handle MUST NOT be used for other open links.An attempt to attach using a handle which
        /// is already associated with a link MUST be responded to with an immediate close carrying a
        /// handle-in-use session-error.
        /// 
        /// To make it easier to monitor AMQP link attach frames, it is RECOMMENDED that implementations
        /// always assign the lowest available handle to this field.
        /// </summary>
        [AmqpDescribedListIndex(1)]
        public uint Handle { get; set; }

        /// <summary>
        /// The role being played by the peer, i.e., whether the peer is the sender or the receiver of messages
        /// on the link.
        /// 
        /// TRUE = Receiver
        /// FALSE = Sender
        /// </summary>
        [AmqpDescribedListIndex(2)]
        public bool Role { get; set; }

        /// <summary>
        /// The delivery settlement policy for the sender. When set at the receiver this indicates the desired
        /// value for the settlement mode at the sender.When set at the sender this indicates the actual
        /// settlement mode in use. The sender SHOULD respect the receiver’s desired settlement mode if
        /// the receiver initiates the attach exchange and the sender supports the desired mode.
        /// 
        /// 0 = unsettled - The sender will send all deliveries initially unsettled to the receiver.
        /// 1 = settled - The sender will send all deliveries settled to the receiver.
        /// 2 = mixed - The sender MAY send a mixture of settled and unsettled deliveries to the receiver.
        /// </summary>
        [AmqpDescribedListIndex(3)]
        public byte? SendSettleMode { get; set; } = 2;

        /// <summary>
        /// The delivery settlement policy for the receiver. When set at the sender this indicates the desired
        /// value for the settlement mode at the receiver .When set at the receiver this indicates the actual
        /// settlement mode in use. The receiver SHOULD respect the sender’s desired settlement mode if
        /// the sender initiates the attach exchange and the receiver supports the desired mode.
        /// 
        /// 0 = first - The receiver will spontaneously settle all incoming transfers.
        /// 1 = second - The receiver will only settle after sending the disposition to the sender and
        ///              receiving a disposition indicating settlement of the delivery from the sender.
        /// </summary>
        [AmqpDescribedListIndex(4)]
        public byte? ReceiveSettleMode { get; set; } = 0;

        /// <summary>
        /// If no source is specified on an outgoing link, then there is no source currently attached to the link.
        /// 
        /// A link with no source will never produce outgoing messages.
        /// </summary>
        [AmqpDescribedListIndex(5)]
        public Source Source { get; set; }

        /// <summary>
        /// If no target is specified on an incoming link, then there is no target currently attached to the link.
        /// 
        /// A link with no target will never permit incoming messages.
        /// </summary>
        [AmqpDescribedListIndex(6)]
        public Target Target { get; set; }

        /// <summary>
        /// This is used to indicate any unsettled delivery states when a suspended link is resumed. The
        /// map is keyed by delivery-tag with values indicating the delivery state.The local and remote
        /// delivery states for a given delivery-tag MUST be compared to resolve any in-doubt deliveries. If
        /// necessary, deliveries MAY be resent, or resumed based on the outcome of this comparison.
        /// 
        /// If the local unsettled map is too large to be encoded within a frame of the agreed maximum
        /// frame size then the session MAY be ended with the frame-size-too-small error. The endpoint
        /// SHOULD make use of the ability to send an incomplete unsettled map (see below) to avoid sending
        /// an error.
        /// 
        /// The unsettled map MUST NOT contain null valued keys.
        /// 
        /// When reattaching (as opposed to resuming), the unsettled map MUST be null.
        /// </summary>
        [AmqpDescribedListIndex(7)]
        public Map Unsettled { get; set; }

        /// <summary>
        /// If set to true this field indicates that the unsettled map provided is not complete. When the map
        /// is incomplete the recipient of the map cannot take the absence of a delivery tag from the map
        /// as evidence of settlement.On receipt of an incomplete unsettled map a sending endpoint MUST
        /// NOT send any new deliveries (i.e.deliveries where resume is not set to true) to its partner (and
        /// a receiving endpoint which sent an incomplete unsettled map MUST detach with an error on
        /// receiving a transfer which does not have the resume flag set to true).
        /// 
        /// Note that if this flag is set to true then the endpoints MUST detach and reattach at least once
        /// in order to send new deliveries.This flag can be useful when there are too many entries in the
        /// unsettled map to fit within a single frame.An endpoint can attach, resume, settle, and detach until
        /// enough unsettled state has been cleared for an attach where this flag is set to false.
        /// </summary>
        [AmqpDescribedListIndex(8)]
        public bool? IncompleteUnsettled { get; set; } = false;

        /// <summary>
        /// This MUST NOT be null if role is sender, and it is ignored if the role is receiver.
        /// </summary>
        [AmqpDescribedListIndex(9)]
        public uint? InitialDelieveryCount { get; set; }

        /// <summary>
        /// This field indicates the maximum message size supported by the link endpoint. Any attempt to
        /// deliver a message larger than this results in a message-size-exceeded link-error. If this field is
        /// zero or unset, there is no maximum size imposed by the link endpoint
        /// </summary>
        [AmqpDescribedListIndex(10)]
        public ulong? MaxMessageSize { get; set; }

        /// <summary>
        /// The extension capabilities the sender supports.
        /// 
        /// A registry of commonly defined link capabilities and their meanings is maintained[AMQPLINKCAP].
        /// </summary>
        [AmqpDescribedListIndex(11)]
        public Symbol[] OfferedCapabilities { get; set; }

        /// <summary>
        /// The extension capabilities the sender can use if the receiver supports them.
        /// 
        /// The sender MUST NOT attempt to use any capability other than those it has declared in desired capabilities
        /// field.
        /// </summary>
        [AmqpDescribedListIndex(12)]
        public Symbol[] DesiredCapabilities { get; set; }

        /// <summary>
        /// The properties map contains a set of fields intended to indicate information about the link and its
        /// container.
        /// 
        /// A registry of commonly defined link properties and their meanings is maintained[AMQPLINKPROP].
        /// </summary>
        [AmqpDescribedListIndex(13)]
        public Fields Properties { get; set; }
    }
}
