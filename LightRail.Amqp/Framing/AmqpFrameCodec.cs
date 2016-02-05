﻿using System;
using LightRail.Amqp.Types;

namespace LightRail.Amqp.Framing
{
    public static class AmqpFrameCodec
    {
        public static AmqpFrame DecodeFrame(ByteBuffer buffer, out ushort channelNumber)
        {
            int frameStartOffset = buffer.ReadOffset;

            // frame header
            uint frameSize = AmqpBitConverter.ReadUInt(buffer);
            byte dataOffset = AmqpBitConverter.ReadUByte(buffer);
            byte frameType = AmqpBitConverter.ReadUByte(buffer);
            channelNumber = AmqpBitConverter.ReadUShort(buffer); // out param

            if (dataOffset < 2)
            {
                throw new AmqpException(ErrorCode.FramingError, $"DOFF must be >= 2. Value is {dataOffset.ToHex()}");
            }

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
                throw new AmqpException(ErrorCode.FramingError, $"Expected Format Code = {FormatCode.Described.ToHex()} but was {formatCode.ToHex()}");

            // decode
            var descriptor = DescribedTypeCodec.ReadDescriptorCode(buffer);
            return (AmqpFrame)DescribedTypeCodec.DecodeDescribedType(buffer, descriptor);
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
