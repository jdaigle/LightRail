using LightRail.Amqp.Types;

namespace LightRail.Amqp.Framing
{
    /// <summary>
    /// Negotiate connection parameters.
    /// 
    /// <type name = "open" class="composite" source="list" provides="frame">
    ///     <descriptor name = "amqp:open:list" code="0x00000000:0x00000010"/>
    ///     <field name = "container-id" type="string" mandatory="true"/>
    ///     <field name = "hostname" type="string"/>
    ///     <field name = "max-frame-size" type="uint" default="4294967295"/>
    ///     <field name = "channel-max" type="ushort" default="65535"/>
    ///     <field name = "idle-time-out" type="milliseconds"/>
    ///     <field name = "outgoing-locales" type="ietf-language-tag" multiple="true"/>
    ///     <field name = "incoming-locales" type="ietf-language-tag" multiple="true"/>
    ///     <field name = "offered-capabilities" type="symbol" multiple="true"/>
    ///     <field name = "desired-capabilities" type="symbol" multiple="true"/>
    ///     <field name = "properties" type="fields"/>
    /// </type>
    /// 
    /// The first frame sent on a connection in either direction MUST contain an open performative. Note that the connection
    /// header which is sent first on the connection is not a frame.
    /// 
    /// The fields indicate the capabilities and limitations of the sending peer.
    /// </summary>
    public sealed class Open : AmqpFrame
    {
        public Open() : base(DescribedTypeCodec.Open) { }

        /// <summary>
        /// The ID of the source container
        /// </summary>
        [AmqpDescribedListIndex(0)]
        public string ContainerID { get; set; } = "";

        /// <summary>
        /// The name of the target host.
        /// </summary>
        [AmqpDescribedListIndex(1)]
        public string Hostname { get; set; }

        /// <summary>
        /// The largest frame size that the sending peer is able to accept on this connection. If this field
        /// is not set it means that the peer does not impose any specific limit. A peer MUST NOT send
        /// frames larger than its partner can handle. A peer that receives an oversized frame MUST close
        /// the connection with the framing-error error-code.
        /// 
        /// Both peers MUST accept frames of up to 512 (MIN-MAX-FRAME-SIZE) octets.
        /// </summary>
        [AmqpDescribedListIndex(2)]
        public uint MaxFrameSize { get; set; } = uint.MaxValue;

        /// <summary>
        /// The channel-max value is the highest channel number that can be used on the connection. This
        /// value plus one is the maximum number of sessions that can be simultaneously active on the
        /// connection. A peer MUST not use channel numbers outside the range that its partner can handle.
        /// A peer that receives a channel number outside the supported range MUST close the connection
        /// with the framing-error error-code.
        /// </summary>
        [AmqpDescribedListIndex(3)]
        public ushort ChannelMax { get; set; } = 65535;

        /// <summary>
        /// The idle timeout REQUIRED by the sender (see subsection 2.4.5). A value of zero is the same
        /// as if it was not set(null). If the receiver is unable or unwilling to support the idle time-out then it
        /// If the value is not set, then the sender does not have an idle time-out. However, senders doing
        /// this SHOULD be aware that implementations MAY choose to use an internal default to efficiently
        /// manage a peer’s resources.
        /// </summary>
        [AmqpDescribedListIndex(4)]
        public uint? IdleTimeOut { get; set; }

        /// <summary>
        /// A list of the locales that the peer supports for sending informational text. This includes connection,
        /// session and link error descriptions. A peer MUST support at least the en-US locale (see subsection
        /// 2.8.12 IETF Language Tag). Since this value is always supported, it need not be supplied in
        /// the outgoing-locales. A null value or an empty list implies that only en-US is supported.
        /// </summary>
        [AmqpDescribedListIndex(5)]
        public Symbol[] OutgoingLocales { get; set; }

        /// <summary>
        /// A list of locales that the sending peer permits for incoming informational text. This list is ordered
        /// in decreasing level of preference. The receiving partner will choose the first (most preferred)
        /// incoming locale from those which it supports. If none of the requested locales are supported, enUS
        /// will be chosen. Note that en-US need not be supplied in this list as it is always the fallback.A
        /// peer MAY determine which of the permitted incoming locales is chosen by examining the partner’s
        /// supported locales as specified in the outgoing-locales field.A null value or an empty list implies
        /// that only en-US is supported.
        /// </summary>
        [AmqpDescribedListIndex(6)]
        public Symbol[] IncomingLocales { get; set; }

        /// <summary>
        /// If the receiver of the offered-capabilities requires an extension capability which is not present in
        /// the offered-capability list then it MUST close the connection.
        /// 
        /// A registry of commonly defined connection capabilities and their meanings is maintained [AMQPCONNCAP].
        /// </summary>
        [AmqpDescribedListIndex(7)]
        public Symbol[] OfferedCapabilities { get; set; }

        /// <summary>
        /// The desired-capability list defines which extension capabilities the sender MAY use if the receiver
        /// offers them (i.e., they are in the offered-capabilities list received by the sender of the desired capabilities).
        /// The sender MUST NOT attempt to use any capabilities it did not declare in the
        /// desired-capabilities field.If the receiver of the desired-capabilities offers extension capabilities
        /// which are not present in the desired-capabilities list it received, then it can be sure those (undesired)
        /// capabilities will not be used on the connection.
        /// </summary>
        [AmqpDescribedListIndex(8)]
        public Symbol[] DesiredCapabilities { get; set; }

        /// <summary>
        /// The properties map contains a set of fields intended to indicate information about the connection
        /// and its container.
        /// 
        /// A registry of commonly defined connection properties and their meanings is maintained [AMQPCONNPROP].
        /// </summary>
        [AmqpDescribedListIndex(9)]
        public Fields Properties { get; set; }
    }
}
