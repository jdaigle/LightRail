using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LightRail.Amqp.Types;

namespace LightRail.Amqp.Framing
{
    public static class FrameCodec
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
            return (AmqpFrame)Encoder.ReadDescribed(buffer, formatCode);
        }

        static FrameCodec()
        {
            var describedTypes = typeof(FrameCodec).Assembly.GetTypes()
                .Where(x => x.IsSealed && typeof(DescribedType).IsAssignableFrom(x))
                .ToList();
            var descriptors = typeof(FrameCodec).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(x => x.FieldType == typeof(Descriptor))
                .Select(x => x.GetValue(null) as Descriptor);
            foreach (var descriptor in descriptors)
            {
                var className = descriptor.Name.Substring(5, descriptor.Name.LastIndexOf(':') - 5);
                var describedType = describedTypes.FirstOrDefault(x => string.Equals(x.Name, className, StringComparison.InvariantCultureIgnoreCase));
                Encoder.RegisterKnownDescriptor(descriptor, describedType);
            }
        }

        // transport performatives
        public static readonly Descriptor Open = new Descriptor(0x0000000000000010, "amqp:open:list");
        public static readonly Descriptor Begin = new Descriptor(0x0000000000000011, "amqp:begin:list");
        public static readonly Descriptor Attach = new Descriptor(0x0000000000000012, "amqp:attach:list");
        public static readonly Descriptor Flow = new Descriptor(0x0000000000000013, "amqp:flow:list");
        public static readonly Descriptor Transfer = new Descriptor(0x0000000000000014, "amqp:transfer:list");
        public static readonly Descriptor Dispose = new Descriptor(0x0000000000000015, "amqp:disposition:list");
        public static readonly Descriptor Detach = new Descriptor(0x0000000000000016, "amqp:detach:list");
        public static readonly Descriptor End = new Descriptor(0x0000000000000017, "amqp:end:list");
        public static readonly Descriptor Close = new Descriptor(0x0000000000000018, "amqp:close:list");

        public static readonly Descriptor Error = new Descriptor(0x000000000000001d, "amqp:error:list");

        // outcome
        public static readonly Descriptor Received = new Descriptor(0x0000000000000023, "amqp:received:list");
        public static readonly Descriptor Accepted = new Descriptor(0x0000000000000024, "amqp:accepted:list");
        public static readonly Descriptor Rejected = new Descriptor(0x0000000000000025, "amqp:rejected:list");
        public static readonly Descriptor Released = new Descriptor(0x0000000000000026, "amqp:released:list");
        public static readonly Descriptor Modified = new Descriptor(0x0000000000000027, "amqp:modified:list");

        public static readonly Descriptor Source = new Descriptor(0x0000000000000028, "amqp:source:list");
        public static readonly Descriptor Target = new Descriptor(0x0000000000000029, "amqp:target:list");

        // message
        public static readonly Descriptor Header = new Descriptor(0x0000000000000070, "amqp:header:list");
        public static readonly Descriptor DeliveryAnnotations = new Descriptor(0x0000000000000071, "amqp:delivery-annotations:map");
        public static readonly Descriptor MessageAnnotations = new Descriptor(0x0000000000000072, "amqp:message-annotations:map");
        public static readonly Descriptor Properties = new Descriptor(0x0000000000000073, "amqp:properties:list");
        public static readonly Descriptor ApplicationProperties = new Descriptor(0x0000000000000074, "amqp:application-properties:map");
        public static readonly Descriptor Data = new Descriptor(0x0000000000000075, "amqp:data:binary");
        public static readonly Descriptor AmqpSequence = new Descriptor(0x0000000000000076, "amqp:amqp-sequence:list");
        public static readonly Descriptor AmqpValue = new Descriptor(0x0000000000000077, "amqp:amqp-value:*");
        public static readonly Descriptor Footer = new Descriptor(0x0000000000000078, "amqp:footer:map");

        // sasl
        public static readonly Descriptor SaslMechanisms = new Descriptor(0x0000000000000040, "amqp:sasl-mechanisms:list");
        public static readonly Descriptor SaslInit = new Descriptor(0x0000000000000041, "amqp:sasl-init:list");
        public static readonly Descriptor SaslChallenge = new Descriptor(0x0000000000000042, "amqp:sasl-challenge:list");
        public static readonly Descriptor SaslResponse = new Descriptor(0x0000000000000043, "amqp:sasl-response:list");
        public static readonly Descriptor SaslOutcome = new Descriptor(0x0000000000000044, "amqp:sasl-outcome:list");

        // transactions
        public static readonly Descriptor Coordinator = new Descriptor(0x0000000000000030, "amqp:coordinator:list");
        public static readonly Descriptor Declare = new Descriptor(0x0000000000000031, "amqp:declare:list");
        public static readonly Descriptor Discharge = new Descriptor(0x0000000000000032, "amqp:discharge:list");
        public static readonly Descriptor Declared = new Descriptor(0x0000000000000033, "amqp:declared:list");
        public static readonly Descriptor TransactionalState = new Descriptor(0x0000000000000034, "amqp:transactional-state:list");
    }
}
