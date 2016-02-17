using LightRail.Amqp.Types;

namespace LightRail.Amqp.Framing
{
    /// <summary>
    /// For containers which do not implement address resolution (and do not admit spontaneous link attachment from
    /// their partners) but are instead only used as producers of messages, it is unnecessary to provide spurious detail
    /// on the source. For this purpose it is possible to use a “minimal” source in which all the fields are left unset.
    /// 
    /// <type name="source" class="composite" source="list" provides="source">
    ///     <descriptor name = "amqp:source:list" code="0x00000000:0x00000028"/>
    ///     <field name = "address" type="*" requires="address"/>
    ///     <field name = "durable" type="terminus-durability" default="none"/>
    ///     <field name = "expiry-policy" type="terminus-expiry-policy" default="session-end"/>
    ///     <field name = "timeout" type="seconds" default="0"/>
    ///     <field name = "dynamic" type="boolean" default="false"/>
    ///     <field name = "dynamic-node-properties" type="node-properties"/>
    ///     <field name = "distribution-mode" type="symbol" requires="distribution-mode"/>
    ///     <field name = "filter" type="filter-set"/>
    ///     <field name = "default-outcome" type="*" requires="outcome"/>
    ///     <field name = "outcomes" type="symbol" multiple="true"/>
    ///     <field name = "capabilities" type="symbol" multiple="true"/>
    /// </type>
    /// </summary>
    public sealed class Source : DescribedList
    {
        public Source() : base(DescribedTypeCodec.Source) { }

        /// <summary>
        /// The address of the source MUST NOT be set when sent on a attach frame sent by the receiving
        /// link endpoint where the dynamic flag is set to true (that is where the receiver is requesting the
        /// sender to create an addressable node).
        /// 
        /// The address of the source MUST be set when sent on a attach frame sent by the sending
        /// link endpoint where the dynamic flag is set to true (that is where the sender has created an
        /// addressable node at the request of the receiver and is now communicating the address of that
        /// created node). The generated name of the address SHOULD include the link name and the
        /// container-id of the remote container to allow for ease of identification.
        /// </summary>
        [AmqpDescribedListIndex(0)]
        public string Address { get; set; }

        /// <summary>
        /// Indicates what state of the terminus will be retained durably: the state of durable messages, only
        /// existence and configuration of the terminus, or no state at all.
        /// 
        /// 0 = None - No terminus state is retained durably.
        /// 1 = Configuration - Only the existence and configuration of the terminus is retained durably.
        /// 2 = Unsettled-State - In addition to the existence and configuration of the terminus, the unsettled state
        ///                       for durable messages is retained durably
        /// </summary>
        [AmqpDescribedListIndex(1)]
        public uint? Durable { get; set; } = 0;

        /// <summary>
        /// The expiry policy of the source
        /// 
        /// "link-detach" = The expiry timer starts when terminus is detached.
        /// "session-end" = The expiry timer starts when the most recently associated session is ended.
        /// "connection-close" = The expiry timer starts when most recently associated connection is closed.
        /// "never" = The terminus never expires
        /// </summary>
        [AmqpDescribedListIndex(2)]
        public Symbol ExpiryPolicy { get; set; } = "session-end";

        /// <summary>
        /// Duration that an expiring source will be retained.
        /// 
        /// The source starts expiring as indicated by the expiry-policy.
        /// </summary>
        [AmqpDescribedListIndex(3)]
        public uint? Timeout { get; set; } = 0;

        /// <summary>
        /// When set to true by the receiving link endpoint, this field constitutes a request for the sending
        /// peer to dynamically create a node at the source.In this case the address field MUST NOT be set.
        /// When set to true by the sending link endpoint this field indicates creation of a dynamically created
        /// node. In this case the address field will contain the address of the created node.The generated
        /// address SHOULD include the link name and other available information on the initiator of the
        /// request (such as the remote container-id) in some recognizable form for ease of traceability.
        /// </summary>
        [AmqpDescribedListIndex(4)]
        public bool? Dynamic { get; set; }

        /// <summary>
        /// If the dynamic field is not set to true this field MUST be left unset.
        /// 
        /// When set by the receiving link endpoint, this field contains the desired properties of the node
        /// the receiver wishes to be created.When set by the sending link endpoint this field contains
        /// the actual properties of the dynamically created node.
        /// 
        /// See subsection 3.5.9 Node Properties for standard node properties.
        /// 
        /// A registry of other commonly used node-properties and their meanings
        /// is maintained [AMQPNODEPROP].
        /// </summary>
        [AmqpDescribedListIndex(5)]
        public Map DynamicNodeProperties { get; set; }

        /// <summary>
        /// This field MUST be set by the sending end of the link if the endpoint supports more than one
        /// distribution-mode. This field MAY be set by the receiving end of the link to indicate a preference
        /// when a node supports multiple distribution modes.
        /// 
        /// "move" = once successfully transferred over the link, the message will no longer be available
        ///          to other links from the same node
        /// "copy" = once successfully transferred over the link, the message is still available for other
        ///          links from the same node
        /// </summary>
        [AmqpDescribedListIndex(6)]
        public Symbol DistributionMode { get; set; }

        /// <summary>
        /// A set of predicates to filter the messages admitted onto the link
        /// 
        /// The receiving endpoint sets its desired filter, the sending endpoint
        /// sets the filter actually in place (including any filters defaulted at the node). The receiving endpoint
        /// MUST check that the filter in place meets its needs and take responsibility for detaching if it does
        /// not.
        /// </summary>
        [AmqpDescribedListIndex(7)]
        public Map Filter { get; set; }

        /// <summary>
        /// Indicates the outcome to be used for transfers that have not reached a terminal state at the
        /// receiver when the transfer is settled, including when the source is destroyed. The value MUST be
        /// a valid outcome(e.g., released or rejected).
        /// </summary>
        [AmqpDescribedListIndex(8)]
        public Outcome DefaultOutcome { get; set; }

        /// <summary>
        /// The values in this field are the symbolic descriptors of the outcomes that can be chosen on this
        /// link.This field MAY be empty, indicating that the default-outcome will be assumed for all message
        /// transfers (if the default-outcome is not set, and no outcomes are provided, then the accepted
        /// outcome MUST be supported by the source).
        /// 
        /// When present, the values MUST be a symbolic descriptor of a valid outcome, e.g.,
        /// “amqp:accepted:list”.
        /// </summary>
        [AmqpDescribedListIndex(9)]
        public Symbol[] Outcomes { get; set; }

        /// <summary>
        /// The extension capabilities the sender supports/desires.
        /// 
        /// A registry of commonly defined source capabilities and their meanings is maintained [AMQPSOURCECAP].
        /// </summary>
        [AmqpDescribedListIndex(10)]
        public Symbol[] Capabilities { get; set; }
    }
}