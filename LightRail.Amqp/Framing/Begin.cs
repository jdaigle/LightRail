using LightRail.Amqp.Types;

namespace LightRail.Amqp.Framing
{
    /// <summary>
    /// Begin a session on a channel.
    /// 
    /// <type name = "begin" class="composite" source="list" provides="frame">
    ///     <descriptor name = "amqp:begin:list" code="0x00000000:0x00000011"/>
    ///     <field name = "remote-channel" type="ushort"/>
    ///     <field name = "next-outgoing-id" type="transfer-number" mandatory="true"/>
    ///     <field name = "incoming-window" type="uint" mandatory="true"/>
    ///     <field name = "outgoing-window" type="uint" mandatory="true"/>
    ///     <field name = "handle-max" type="handle" default="4294967295"/>
    ///     <field name = "offered-capabilities" type="symbol" multiple="true"/>
    ///     <field name = "desired-capabilities" type="symbol" multiple="true"/>
    ///     <field name = "properties" type="fields"/>
    /// </type>
    /// 
    /// Indicate that a session has begun on the channel.
    /// </summary>
    public sealed class Begin : AmqpFrame
    {
        public Begin() : base(DescribedListCodec.Begin) { }

        /// <summary>
        /// If a session is locally initiated, the remote-channel MUST NOT be set. When an endpoint responds
        /// to a remotely initiated session, the remote-channel MUST be set to the channel on which the
        /// remote session sent the begin.
        /// </summary>
        [AmqpDescribedListIndex(0)]
        public ushort? RemoteChannel { get; set; }

        /// <summary>
        /// the transfer-id of the first transfer id the sender will send
        /// </summary>
        [AmqpDescribedListIndex(1)]
        public uint NextOutgoingId { get; set; }

        /// <summary>
        /// the initial incoming-window of the sender
        /// </summary>
        [AmqpDescribedListIndex(2)]
        public uint IncomingWindow { get; set; }

        /// <summary>
        /// the initial outgoing-window of the sender
        /// </summary>
        [AmqpDescribedListIndex(3)]
        public uint OutgoingWindow { get; set; }

        /// <summary>
        /// The handle-max value is the highest handle value that can be used on the session. A peer MUST
        /// NOT attempt to attach a link using a handle value outside the range that its partner can handle.
        /// A peer that receives a handle outside the supported range MUST close the connection with the
        /// framing-error error-code.
        /// </summary>
        [AmqpDescribedListIndex(4)]
        public uint HandleMax { get; set; } = 4294967295;

        /// <summary>
        /// the extension capabilities the sender supports
        /// 
        /// A registry of commonly defined session capabilities and their meanings is maintained [AMQPSESSCAP].
        /// </summary>
        [AmqpDescribedListIndex(5)]
        public Symbol[] OfferedCapabilities { get; set; }

        /// <summary>
        /// The sender MUST NOT attempt to use any capability other than those it has declared in desiredcapabilities
        /// field.
        /// </summary>
        [AmqpDescribedListIndex(6)]
        public Symbol[] DesiredCapabilities { get; set; }

        /// <summary>
        /// The properties map contains a set of fields intended to indicate information about the session and
        /// its container.
        /// 
        /// A registry of commonly defined session properties and their meanings is maintained
        /// [AMQPSESSPROP].
        /// </summary>
        [AmqpDescribedListIndex(7)]
        public Fields Properties { get; set; }
    }
}
