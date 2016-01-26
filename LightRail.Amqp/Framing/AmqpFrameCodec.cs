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
            uint frameSize = AmqpBitConverter.ReadUInt(buffer);
            byte dataOffset = AmqpBitConverter.ReadUByte(buffer);
            byte frameType = AmqpBitConverter.ReadUByte(buffer);
            ushort channelNumber = AmqpBitConverter.ReadUShort(buffer);

            // data offset is always counted in 4-byte words. header total length is 8 bytes
            int bodyStartOffset = 4 * dataOffset;
            // forward the reader the number of bytes needed to reach the frame body
            buffer.CompleteRead((bodyStartOffset - 8));

            if (frameSize == bodyStartOffset)
            {
                // empty frame body
                return null;
            }

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

        public static void EncodeFrame(ByteBuffer buffer, AmqpFrame frame, ushort channelNumber)
        {
            buffer.ValidateWrite(8);

            var frameStartOffset = buffer.WriteOffset;

            // header
            buffer.AppendWrite(FixedWidth.UInt); // placeholder for frame size
            AmqpBitConverter.WriteUByte(buffer, 0x02); // data offset (2*4 = 8 bytes to account for header)
            AmqpBitConverter.WriteUByte(buffer, 0x00); // frame type = AMQP frame
            AmqpBitConverter.WriteUShort(buffer, channelNumber);

            // frame body, may be null/empty
            if (frame != null)
            {
                frame.Encode(buffer);
            }

            // frame size
            int frameSize = buffer.WriteOffset - frameStartOffset;
            AmqpBitConverter.WriteInt(buffer.Buffer, frameStartOffset, frameSize);
        }
    }
}
