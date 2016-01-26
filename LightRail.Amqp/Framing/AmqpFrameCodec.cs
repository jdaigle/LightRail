using System;
using LightRail.Amqp.Types;

namespace LightRail.Amqp.Framing
{
    public static class AmqpFrameCodec
    {
        public static AmqpFrame DecodeFrame(ByteBuffer buffer)
        {
            int frameStartOffset = buffer.ReadOffset;

            // frame header
            uint frameSize = Encoder.ReadUInt(buffer, FormatCode.UInt);
            byte dataOffset = Encoder.ReadUByte(buffer, FormatCode.UByte);
            byte frameType = Encoder.ReadUByte(buffer, FormatCode.UByte);
            ushort channelNumber = Encoder.ReadUShort(buffer, FormatCode.UShort);

            // data offset is always counted in 4-byte words. header total length is 8 bytes
            int bodyStartOffset = 4 * dataOffset;
            // forward the reader the number of bytes needed to reach the frame body
            buffer.CompleteRead((bodyStartOffset - 8));

            // we're expecting a described list...
            var formatCode = Encoder.ReadFormatCode(buffer);
            if (formatCode != FormatCode.Described)
            {
                throw new AmqpException(ErrorCode.FramingError, $"Expected Format Code = {FormatCode.Described.ToHex()} but was {formatCode.ToHex()}");
            }
            var descriptorFormatCode = Encoder.ReadFormatCode(buffer);
            if (descriptorFormatCode == FormatCode.ULong ||
                descriptorFormatCode == FormatCode.SmallULong)
            {
                var descriptor = Encoder.ReadULong(buffer, descriptorFormatCode);
                return (AmqpFrame)DescribedListCodec.DecodeDescribedList(buffer, descriptor);
            }
            else if (descriptorFormatCode == FormatCode.Symbol8 ||
                     descriptorFormatCode == FormatCode.Symbol32)
            {
                throw new NotImplementedException("Have Not Yet Implemented Symbol Descriptor Decoding");
            }
            else
            {
                throw new AmqpException(ErrorCode.FramingError, $"Invalid Frame Descriptor Format Code = {descriptorFormatCode.ToHex()}");
            }
        }
    }
}
