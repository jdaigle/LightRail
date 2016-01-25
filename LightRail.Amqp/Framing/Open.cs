using System;
using LightRail.Amqp.Types;

namespace LightRail.Amqp.Framing
{
    public sealed class Open : AmqpFrame
    {
        public Open()
            : base(FrameCodec.Open)
        {
        }

        /// <summary>
        /// The ID of the source container
        /// </summary>
        [AmqpFrameIndex(0)]
        public string ContainerID { get; set; }
        /// <summary>
        /// The name of the target host.
        /// </summary>
        [AmqpFrameIndex(1)]
        public string Hostname { get; set; }
        /// <summary>
        /// The largest frame size that the sending peer is able to accept on this connection. If this field
        /// is not set it means that the peer does not impose any specific limit. A peer MUST NOT send
        /// frames larger than its partner can handle. A peer that receives an oversized frame MUST close
        /// the connection with the framing-error error-code.
        /// 
        /// Both peers MUST accept frames of up to 512 (MIN-MAX-FRAME-SIZE) octets.        /// </summary>
        [AmqpFrameIndex(2)]
        public uint MaxFrameSize { get; set; } = 4294967295;
        /// <summary>
        /// The channel-max value is the highest channel number that can be used on the connection. This
        /// value plus one is the maximum number of sessions that can be simultaneously active on the
        /// connection. A peer MUST not use channel numbers outside the range that its partner can handle.
        /// A peer that receives a channel number outside the supported range MUST close the connection
        /// with the framing-error error-code.
        /// </summary>
        [AmqpFrameIndex(3)]
        public ushort ChannelMax { get; set; } = 65535;
        /// <summary>
        /// The idle timeout REQUIRED by the sender (see subsection 2.4.5). A value of zero is the same
        /// as if it was not set(null). If the receiver is unable or unwilling to support the idle time-out then it
        /// If the value is not set, then the sender does not have an idle time-out. However, senders doing
        /// this SHOULD be aware that implementations MAY choose to use an internal default to efficiently
        /// manage a peer’s resources.
        /// </summary>
        [AmqpFrameIndex(4)]
        public uint IdleTimeOut { get; set; }
        [AmqpFrameIndex(5)]
        public object OutgoingLocales { get; set; }
        [AmqpFrameIndex(6)]
        public object IncomingLocales { get; set; }
        [AmqpFrameIndex(7)]
        public object OfferedCapabilities { get; set; }
        [AmqpFrameIndex(8)]
        public object DesiredCapabilities { get; set; }
        [AmqpFrameIndex(9)]
        public object Properties { get; set; }

        protected override int CalculateListSize()
        {
            throw new NotImplementedException();
        }

        protected override void EncodeListItem(ByteBuffer buffer, int index, bool arrayEncoding)
        {
            throw new NotImplementedException();
        }

        protected override void DecodeListItem(ByteBuffer buffer, int index)
        {
            throw new NotImplementedException();
            //if (index == 0)
            //    ContainerID = Encoder.ReadString(buffer, Encoder.ReadFormatCode(buffer));
            //if (index == 1)
            //    Hostname = Encoder.ReadString(buffer, Encoder.ReadFormatCode(buffer));
            //if (index == 2)
            //    MaxFrameSize = Encoder.ReadUInt(buffer, Encoder.ReadFormatCode(buffer));
            //if (index == 3)
            //    ChannelMax = Encoder.ReadUShort(buffer, Encoder.ReadFormatCode(buffer));
            //if (index == 4)
            //    IdleTimeOut = Encoder.ReadUInt(buffer, Encoder.ReadFormatCode(buffer));
            //if (index == 5)
            //    OutgoingLocales = Encoder.ReadBoxedObject(buffer, Encoder.ReadFormatCode(buffer));
            //if (index == 6)
            //    IncomingLocales = Encoder.ReadBoxedObject(buffer, Encoder.ReadFormatCode(buffer));
            //if (index == 7)
            //    OfferedCapabilities = Encoder.ReadBoxedObject(buffer, Encoder.ReadFormatCode(buffer));
            //if (index == 8)
            //    DesiredCapabilities = Encoder.ReadBoxedObject(buffer, Encoder.ReadFormatCode(buffer));
            //if (index == 9)
            //    Properties = Encoder.ReadBoxedObject(buffer, Encoder.ReadFormatCode(buffer));
        }
    }
}
