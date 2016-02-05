using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LightRail.Amqp.Types
{
    public static class DescribedTypeCodec
    {
        // transport performatives
        public static readonly Descriptor Open = new Descriptor(0x0000000000000010, "amqp:open:list");
        public static readonly Descriptor Begin = new Descriptor(0x0000000000000011, "amqp:begin:list");
        public static readonly Descriptor Attach = new Descriptor(0x0000000000000012, "amqp:attach:list");
        public static readonly Descriptor Flow = new Descriptor(0x0000000000000013, "amqp:flow:list");
        public static readonly Descriptor Transfer = new Descriptor(0x0000000000000014, "amqp:transfer:list");
        public static readonly Descriptor Disposition = new Descriptor(0x0000000000000015, "amqp:disposition:list");
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

        public static void EncodeValue(ByteBuffer buffer, DescribedType instance)
        {
            Action<ByteBuffer, DescribedType> encoder;
            if (!knownEncoders.TryGetValue(instance.Descriptor.Code, out encoder))
            {
                throw new AmqpException(ErrorCode.InternalError, $"Missing Encoder For Known Described Type {instance.Descriptor.ToString()}");
            }
            encoder(buffer, instance);
        }

        public static void DecodeValue(ByteBuffer buffer, DescribedType instance)
        {
            Action<ByteBuffer, DescribedType> decoder;
            if (!knownDecoders.TryGetValue(instance.Descriptor.Code, out decoder))
            {
                throw new AmqpException(ErrorCode.InternalError, $"Missing Decoder For Known Described Type {instance.Descriptor.ToString()}");
            }
            decoder(buffer, instance);
        }

        static DescribedTypeCodec()
        {
            var describedTypes = typeof(DescribedTypeCodec).Assembly.GetTypes()
                .Where(x => x.IsSealed && typeof(DescribedType).IsAssignableFrom(x))
                .ToList();
            var descriptors = typeof(DescribedTypeCodec).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(x => x.FieldType == typeof(Descriptor))
                .Select(x => x.GetValue(null) as Descriptor);
            foreach (var descriptor in descriptors)
            {
                var className = descriptor.Name.Substring(5, descriptor.Name.LastIndexOf(':') - 5).Replace("-", "");
                var describedType = describedTypes.FirstOrDefault(x => string.Equals(x.Name, className, StringComparison.InvariantCultureIgnoreCase));

                knownDescribedTypeDescriptors.Add(descriptor.Code, descriptor);
                if (describedType != null)
                {
                    var ctor = CompilerConstructor(descriptor, describedType);
                    if (ctor != null)
                        knownDescribedTypeConstructors.Add(descriptor.Code, ctor);
                    knownEncoders.Add(descriptor.Code, CompileEncoder(descriptor, describedType));
                    knownDecoders.Add(descriptor.Code, CompileDecoder(descriptor, describedType));
                }
            }
        }

        private static readonly Dictionary<ulong, Descriptor> knownDescribedTypeDescriptors = new Dictionary<ulong, Descriptor>();
        private static readonly Dictionary<ulong, Func<object>> knownDescribedTypeConstructors = new Dictionary<ulong, Func<object>>();
        private static readonly Dictionary<ulong, Action<ByteBuffer, DescribedType>> knownEncoders = new Dictionary<ulong, Action<ByteBuffer, DescribedType>>();
        private static readonly Dictionary<ulong, Action<ByteBuffer, DescribedType>> knownDecoders = new Dictionary<ulong, Action<ByteBuffer, DescribedType>>();

        private static Func<object> CompilerConstructor(Descriptor descriptor, Type describedType)
        {
            var ctor = describedType.GetConstructor(new Type[0]);
            if (ctor != null)
            {
                return () => ctor.Invoke(null);
            }
            return null;
        }

        private static Action<ByteBuffer, DescribedType> CompileDecoder(Descriptor descriptor, Type describedType)
        {
            if (typeof(DescribedList).IsAssignableFrom(describedType))
                return DescribedListCodec.CompileDecoder(descriptor, describedType);
            else
                return (buffer, instance) => instance.Decode(buffer);
        }

        private static Action<ByteBuffer, DescribedType> CompileEncoder(Descriptor descriptor, Type describedType)
        {
            if (typeof(DescribedList).IsAssignableFrom(describedType))
                return DescribedListCodec.CompileEncoder(descriptor, describedType);
            else
                return (buffer, instance) => instance.Encode(buffer);
        }

        internal static bool IsKnownDescribedType(Descriptor descriptor)
        {
            return knownDescribedTypeDescriptors.ContainsKey(descriptor.Code);
        }

        internal static object DecodeDescribedType(ByteBuffer buffer, ulong code)
        {
            var descriptor = knownDescribedTypeDescriptors[code];
            Func<object> ctor;
            if (knownDescribedTypeConstructors.TryGetValue(descriptor.Code, out ctor))
            {
                var instance = ctor() as DescribedType;
                instance.Decode(buffer);
                return instance;
            }
            else
            {
                throw new AmqpException(ErrorCode.DecodeError, $"Missing Constructor For Known Described Type {descriptor.ToString()}");
            }
        }

        internal static ulong ReadDescriptorCode(ByteBuffer buffer)
        {
            var descriptorFormatCode = Encoder.ReadFormatCode(buffer);
            if (descriptorFormatCode == FormatCode.ULong ||
                descriptorFormatCode == FormatCode.SmallULong)
            {
                return Encoder.ReadULong(buffer, descriptorFormatCode);
            }
            if (descriptorFormatCode == FormatCode.Symbol8 ||
                descriptorFormatCode == FormatCode.Symbol32)
            {
                throw new NotImplementedException("Have Not Yet Implemented Symbol Descriptor Decoding");
            }
            throw new AmqpException(ErrorCode.FramingError, $"Invalid Descriptor Format Code{descriptorFormatCode.ToHex()}");
        }
    }
}
