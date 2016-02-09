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
    public class BrokeredMessage
    {
        /// <summary>
        /// The header section carries standard delivery details about the
        /// transfer of a message through the AMQP network.
        /// </summary>
        public Header Header;

        /// <summary>
        /// The delivery-annotations section is used for delivery-specific
        /// non-standard properties at the head of the message.
        /// </summary>
        public DeliveryAnnotations DeliveryAnnotations;

        /// <summary>
        /// The message-annotations section is used for properties of the
        /// message which are aimed at the infrastructure and SHOULD be
        /// propagated across every delivery step.
        /// </summary>
        public MessageAnnotations MessageAnnotations;

        /// <summary>
        /// Immutable properties of the message. The properties section is
        /// used for a defined set of standard properties of the message.
        /// </summary>
        public Properties Properties;

        /// <summary>
        /// The application-properties section is a part of the bare message
        /// used for structured application data.
        /// </summary>
        public ApplicationProperties ApplicationProperties;

        /// <summary>
        /// The body consists of one of the following three choices: one or more data sections, one or more amqp-sequence
        /// sections, or a single amqp-value section.
        /// </summary>
        public DescribedType BodySection;

        /// <summary>
        /// Transport footers for a message.
        /// </summary>
        public Footer Footer;
        
        public void Encode(ByteBuffer buffer)
        {
            EncodeIfNotNull(this.Header, buffer);
            EncodeIfNotNull(this.DeliveryAnnotations, buffer);
            EncodeIfNotNull(this.MessageAnnotations, buffer);
            EncodeIfNotNull(this.Properties, buffer);
            EncodeIfNotNull(this.ApplicationProperties, buffer);
            EncodeIfNotNull(this.BodySection, buffer);
            EncodeIfNotNull(this.Footer, buffer);
        }

        static void EncodeIfNotNull(DescribedType section, ByteBuffer buffer)
        {
            if (section != null)
            {
                section.Encode(buffer);
            }
        }

        public static BrokeredMessage Decode(ByteBuffer buffer)
        {
            var message = new BrokeredMessage();

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
                    message.Properties = (Properties)DescribedTypeCodec.DecodeDescribedType(buffer, descriptorCode);
                    continue;
                }

                if (descriptorCode == DescribedTypeCodec.ApplicationProperties.Code)
                {
                    message.ApplicationProperties = (ApplicationProperties)DescribedTypeCodec.DecodeDescribedType(buffer, descriptorCode);
                    continue;
                }

                if (descriptorCode == DescribedTypeCodec.Data.Code)
                {
                    throw new NotImplementedException("TODO: Decode Described amqp:data:binary");
                    // continue;
                }

                if (descriptorCode == DescribedTypeCodec.AmqpValue.Code)
                {
                    throw new NotImplementedException("TODO: Decode Described amqp:amqp-value:*");
                    // continue;
                }

                if (descriptorCode == DescribedTypeCodec.AmqpSequence.Code)
                {
                    throw new NotImplementedException("TODO: Decode Described amqp:amqp-sequence:list");
                    //continue;
                }
            }

            return message;
        }
    }
}
