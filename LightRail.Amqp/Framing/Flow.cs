﻿using LightRail.Amqp.Types;

namespace LightRail.Amqp.Framing
{
    /// <summary>
    /// Update link state.
    /// 
    /// <type name = "flow" class="composite" source="list" provides="frame">
    ///     <descriptor name = "amqp:flow:list" code="0x00000000:0x00000013"/>
    ///     <field name = "next-incoming-id" type="transfer-number"/>
    ///     <field name = "incoming-window" type="uint" mandatory="true"/>
    ///     <field name = "next-outgoing-id" type="transfer-number" mandatory="true"/>
    ///     <field name = "outgoing-window" type="uint" mandatory="true"/>
    ///     <field name = "handle" type="handle"/>
    ///     <field name = "delivery-count" type="sequence-no"/>
    ///     <field name = "link-credit" type="uint"/>
    ///     <field name = "available" type="uint"/>
    ///     <field name = "drain" type="boolean" default="false"/>
    ///     <field name = "echo" type="boolean" default="false"/>
    ///     <field name = "properties" type="fields"/>
    /// </type>
    /// 
    /// Updates the flow state for the specified link.
    /// </summary>
    public sealed class Flow : AmqpFrame
    {
        public Flow() : base(DescribedTypeCodec.Flow) { }

        /// <summary>
        /// Identifies the expected transfer-id of the next incoming transfer frame. This value MUST be set
        /// if the peer has received the begin frame for the session, and MUST NOT be set if it has not.
        /// </summary>
        [AmqpDescribedListIndex(0)]
        public uint? NextIncomingId { get; set; }

        /// <summary>
        /// Defines the maximum number of incoming transfer frames that the endpoint can currently receive.
        /// </summary>
        [AmqpDescribedListIndex(1)]
        public uint IncomingWindow { get; set; }

        /// <summary>
        /// The transfer-id that will be assigned to the next outgoing transfer frame.
        /// </summary>
        [AmqpDescribedListIndex(2)]
        public uint NextOutgoingId { get; set; }

        /// <summary>
        /// Defines the maximum number of outgoing transfer frames that the endpoint could potentially
        /// currently send, if it was not constrained by restrictions imposed by its peer’s incoming-window.
        /// </summary>
        [AmqpDescribedListIndex(3)]
        public uint OutgoingWindow { get; set; }

        /// <summary>
        /// If set, indicates that the flow frame carries flow state information for the local link endpoint associated
        /// with the given handle.If not set, the flow frame is carrying only information pertaining to the
        /// session endpoint.
        /// 
        /// If set to a handle that is not currently associated with an attached link, the recipient MUST respond
        /// by ending the session with an unattached-handle session error.
        /// </summary>
        [AmqpDescribedListIndex(4)]
        public uint? Handle { get; set; }

        /// <summary>
        /// When the handle field is not set, this field MUST NOT be set.
        /// 
        /// When the handle identifies that the flow state is being sent from the sender link endpoint to
        /// receiver link endpoint this field MUST be set to the current delivery-count of the link endpoint.
        /// 
        /// When the flow state is being sent from the receiver endpoint to the sender endpoint this field
        /// MUST be set to the last known value of the corresponding sending endpoint. In the event that the
        /// receiving link endpoint has not yet seen the initial attach frame from the sender this field MUST
        /// NOT be set.
        /// </summary>
        [AmqpDescribedListIndex(5)]
        public uint? DeliveryCount { get; set; }

        /// <summary>
        /// The current maximum number of messages that can be handled at the receiver endpoint of the
        /// link. Only the receiver endpoint can independently set this value. The sender endpoint sets this
        /// to the last known value seen from the receiver.
        /// 
        /// When the handle field is not set, this field MUST NOT be set.
        /// </summary>
        [AmqpDescribedListIndex(6)]
        public uint? LinkCredit { get; set; }

        /// <summary>
        /// The number of messages awaiting credit at the link sender endpoint. Only the sender can independently
        /// set this value.The receiver sets this to the last known value seen from the sender.
        /// 
        /// When the handle field is not set, this field MUST NOT be set.
        /// </summary>
        [AmqpDescribedListIndex(7)]
        public uint? Available { get; set; }

        /// <summary>
        /// When flow state is sent from the sender to the receiver, this field contains the actual drain mode of
        /// the sender. When flow state is sent from the receiver to the sender, this field contains the desired
        /// drain mode of the receiver.
        /// 
        /// When the handle field is not set, this field MUST NOT be set.
        /// </summary>
        [AmqpDescribedListIndex(8)]
        public bool? Drain { get; set; }

        /// <summary>
        /// If set to true then the receiver SHOULD send its state at the earliest convenient opportunity.
        /// If set to true, and the handle field is not set, then the sender only requires session endpoint state
        /// to be echoed, however, the receiver MAY fulfil this requirement by sending a flow performative
        /// carrying link-specific state (since any such flow also carries session state).
        /// 
        /// If a sender makes multiple requests for the same state before the receiver can reply, the receiver
        /// MAY send only one flow in return.
        /// 
        /// Note that if a peer responds to echo requests with flows which themselves have the echo field set
        /// to true, an infinite loop could result if its partner adopts the same policy (therefore such a policy
        /// SHOULD be avoided).
        /// </summary>
        [AmqpDescribedListIndex(9)]
        public bool Echo { get; set; }

        /// <summary>
        /// A registry of commonly defined link state properties and their meanings is maintained [AMQPLINKSTATEPROP].
        /// 
        /// When the handle field is not set, this field MUST NOT be set.
        /// </summary>
        [AmqpDescribedListIndex(10)]
        public Map Properties { get; set; }
    }
}
