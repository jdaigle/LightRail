using LightRail.Amqp.Types;

namespace LightRail.Amqp.Framing
{
    /// <summary>
    /// For containers which do not implement address resolution (and do not admit spontaneous link attachment from
    /// their partners) but are instead only used as consumers of messages, it is unnecessary to provide spurious detail
    /// on the source.For this purpose it is possible to use a “minimal” target in which all the fields are left unset.
    /// 
    /// <type name="target" class="composite" source="list" provides="target">
    ///     <descriptor name = "amqp:target:list" code="0x00000000:0x00000029"/>
    ///     <field name = "address" type="*" requires="address"/>
    ///     <field name = "durable" type="terminus-durability" default="none"/>
    ///     <field name = "expiry-policy" type="terminus-expiry-policy" default="session-end"/>
    ///     <field name = "timeout" type="seconds" default="0"/>
    ///     <field name = "dynamic" type="boolean" default="false"/>
    ///     <field name = "dynamic-node-properties" type="node-properties"/>
    ///     <field name = "capabilities" type="symbol" multiple="true"/>
    /// </type>
    /// </summary>
    public sealed class Target : DescribedList
    {
        public Target() : base(DescribedTypeCodec.Target) { }

        /// <summary>
        /// The address of the target MUST NOT be set when sent on a attach frame sent by the sending
        /// link endpoint where the dynamic flag is set to true (that is where the sender is requesting the
        /// receiver to create an addressable node).
        /// 
        /// The address of the source MUST be set when sent on a attach frame sent by the receiving
        /// link endpoint where the dynamic flag is set to true (that is where the receiver has created an
        /// addressable node at the request of the sender and is now communicating the address of that
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
        /// The expiry policy of the target
        /// 
        /// "link-detach" = The expiry timer starts when terminus is detached.
        /// "session-end" = The expiry timer starts when the most recently associated session is ended.
        /// "connection-close" = The expiry timer starts when most recently associated connection is closed.
        /// "never" = The terminus never expires
        /// </summary>
        [AmqpDescribedListIndex(2)]
        public Symbol ExpiryPolicy { get; set; } = "session-end";

        /// <summary>
        /// Duration that an expiring target will be retained.
        /// 
        /// The target starts expiring as indicated by the expiry-policy.
        /// </summary>
        [AmqpDescribedListIndex(3)]
        public uint? Timeout { get; set; } = 0;

        /// <summary>
        /// When set to true by the sending link endpoint, this field constitutes a request for the receiving
        /// peer to dynamically create a node at the target. In this case the address field MUST NOT be set.
        /// When set to true by the receiving link endpoint this field indicates creation of a dynamically created
        /// node. In this case the address field will contain the address of the created node.The generated
        /// address SHOULD include the link name and other available information on the initiator of the
        /// request (such as the remote container-id) in some recognizable form for ease of traceability.
        /// </summary>
        [AmqpDescribedListIndex(4)]
        public bool? Dynamic { get; set; }

        /// <summary>
        /// If the dynamic field is not set to true this field MUST be left unset.
        /// 
        /// When set by the sending link endpoint, this field contains the desired properties of the node
        /// the sender wishes to be created.When set by the receiving link endpoint this field contains
        /// the actual properties of the dynamically created node.
        /// 
        /// See subsection 3.5.9 Node Properties for standard node properties.
        /// 
        /// A registry of other commonly used node-properties and their meanings
        /// is maintained [AMQPNODEPROP].
        /// </summary>
        [AmqpDescribedListIndex(5)]
        public Fields DynamicNodeProperties { get; set; }

        /// <summary>
        /// The extension capabilities the sender supports/desires.
        /// 
        /// A registry of commonly defined target capabilities and their meanings is maintained [AMQPTARGETCAP].
        /// </summary>
        [AmqpDescribedListIndex(6)]
        public Symbol[] Capabilities { get; set; }
    }
}