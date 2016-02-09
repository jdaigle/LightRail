using System;
using LightRail.Amqp.Types;

namespace LightRail.Amqp.Messaging
{
    /// <summary>
    /// An annotated message consists of the bare message plus sections for annotation at the head and tail of the
    /// bare message.
    /// 
    /// The bare message itself is simply kept as an immutable byte array.
    /// </summary>
    public sealed class AnnotatedMessage
    {
        /// <summary>
        /// The header section carries standard delivery details about the
        /// transfer of a message through the AMQP network.
        /// </summary>
        public Header Header { get; set; } = new Header();

        /// <summary>
        /// The delivery-annotations section is used for delivery-specific
        /// non-standard properties at the head of the message.
        /// </summary>
        public DeliveryAnnotations DeliveryAnnotations { get; set; }

        /// <summary>
        /// The message-annotations section is used for properties of the
        /// message which are aimed at the infrastructure and SHOULD be
        /// propagated across every delivery step.
        /// </summary>
        public MessageAnnotations MessageAnnotations { get; set; }

        /// <summary>
        /// The bare message is immutable within the AMQP network. That is, none of the sections can be changed by any
        /// node acting as an AMQP intermediary.
        /// </summary>
        public byte[] BareMessage { get; set; }

        /// <summary>
        /// Transport footers for a message.
        /// </summary>
        public Footer Footer { get; set; }

        public static AnnotatedMessage Decode(ByteBuffer buffer)
        {
            var message = new AnnotatedMessage();

            int bareMessageStartOffset = -1;
            int bareMessageEndOffset = -1;

            while (buffer.LengthAvailableToRead > 0)
            {
                int offOfDescribedList = buffer.ReadOffset;

                // peak at the type of the described list
                var formatCode = AmqpCodec.DecodeFormatCode(buffer);
                if (formatCode != FormatCode.Described)
                    throw new AmqpException(ErrorCode.FramingError, $"Expected Format Code = {FormatCode.Described.ToHex()} but was {formatCode.ToHex()}");

                var descriptorCode = DescribedTypeCodec.ReadDescriptorCode(buffer);

                if (descriptorCode == DescribedTypeCodec.Header.Code)
                {
                    message.Header = (Header)DescribedTypeCodec.DecodeDescribedType(buffer, descriptorCode);
                    continue;
                }

                if (descriptorCode == DescribedTypeCodec.DeliveryAnnotations.Code)
                {
                    message.DeliveryAnnotations = (DeliveryAnnotations)DescribedTypeCodec.DecodeDescribedType(buffer, descriptorCode);
                    continue;
                }

                if (descriptorCode == DescribedTypeCodec.MessageAnnotations.Code)
                {
                    message.MessageAnnotations = (MessageAnnotations)DescribedTypeCodec.DecodeDescribedType(buffer, descriptorCode);
                    continue;
                }

                if (descriptorCode == DescribedTypeCodec.Footer.Code)
                {
                    message.Footer = (Footer)DescribedTypeCodec.DecodeDescribedType(buffer, descriptorCode);
                    continue;
                }

                if (descriptorCode == DescribedTypeCodec.Properties.Code)
                {
                    if (bareMessageStartOffset < 0)
                        bareMessageStartOffset = offOfDescribedList; // the first described list in the bare message
                    SkipDescribedList(buffer);
                    bareMessageEndOffset = buffer.ReadOffset;
                    continue;
                }

                if (descriptorCode == DescribedTypeCodec.ApplicationProperties.Code)
                {
                    if (bareMessageStartOffset < 0)
                        bareMessageStartOffset = offOfDescribedList; // the first described list in the bare message
                    SkipDescribedList(buffer);
                    bareMessageEndOffset = buffer.ReadOffset;
                    continue;
                }

                if (descriptorCode == DescribedTypeCodec.Data.Code)
                {
                    if (bareMessageStartOffset < 0)
                        bareMessageStartOffset = offOfDescribedList; // the first described list in the bare message
                    SkipBinaryBuffer(buffer);
                    continue;
                }

                if (descriptorCode == DescribedTypeCodec.AmqpValue.Code)
                {
                    if (bareMessageStartOffset < 0)
                        bareMessageStartOffset = offOfDescribedList; // the first described list in the bare message
                    throw new NotImplementedException();
                    //continue;
                }

                if (descriptorCode == DescribedTypeCodec.AmqpSequence.Code)
                {
                    if (bareMessageStartOffset < 0)
                        bareMessageStartOffset = offOfDescribedList; // the first described list in the bare message
                    throw new NotImplementedException();
                    //continue;
                }
            }

            if (bareMessageStartOffset > -1)
            {
                message.BareMessage = new byte[bareMessageEndOffset - bareMessageStartOffset];
                Array.Copy(buffer.Buffer, bareMessageEndOffset, message.BareMessage, 0, message.BareMessage.Length);
            }

            return message;
        }

        private static void SkipDescribedList(ByteBuffer buffer)
        {
            // read the list length and move forward
            var listFormatCode = AmqpCodec.DecodeFormatCode(buffer);
            int size = 0;
            if (listFormatCode == FormatCode.List0)
                size = 0;
            else if (listFormatCode == FormatCode.List8)
                size = AmqpBitConverter.ReadUByte(buffer);
            else if (listFormatCode == FormatCode.List32)
                size = (int)AmqpBitConverter.ReadUInt(buffer);
            buffer.CompleteRead(size);
        }

        private static void SkipBinaryBuffer(ByteBuffer buffer)
        {
            var binaryFormatCode = AmqpCodec.DecodeFormatCode(buffer);
            int size = 0;
            if (binaryFormatCode == FormatCode.Binary8)
                size = AmqpBitConverter.ReadUByte(buffer);
            else if (binaryFormatCode == FormatCode.Binary32)
                size = (int)AmqpBitConverter.ReadUInt(buffer);
            buffer.CompleteRead(size);
        }
    }
}
