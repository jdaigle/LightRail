using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LightRail.Amqp.Framing;

namespace LightRail.Amqp.Types
{
    public static class DescribedListCodec
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

        static DescribedListCodec()
        {
            var describedTypes = typeof(DescribedListCodec).Assembly.GetTypes()
                .Where(x => x.IsSealed && typeof(DescribedType).IsAssignableFrom(x))
                .ToList();
            var descriptors = typeof(DescribedListCodec).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(x => x.FieldType == typeof(Descriptor))
                .Select(x => x.GetValue(null) as Descriptor);
            foreach (var descriptor in descriptors)
            {
                var className = descriptor.Name.Substring(5, descriptor.Name.LastIndexOf(':') - 5);
                var describedType = describedTypes.FirstOrDefault(x => string.Equals(x.Name, className, StringComparison.InvariantCultureIgnoreCase));

                knownDescribedTypeDescriptors.Add(descriptor.Code, descriptor);
                if (describedType != null)
                {
                    CompilerConstructor(descriptor, describedType);
                    CompileEncoder(descriptor, describedType);
                    CompileDecoder(descriptor, describedType);
                }
            }
        }

        public static void EncodeValue(ByteBuffer buffer, DescribedList instance)
        {
            Action<ByteBuffer, DescribedList> encoder;
            if (!knownFrameEncoders.TryGetValue(instance.Descriptor.Code, out encoder))
            {
                throw new AmqpException(ErrorCode.InternalError, $"Missing Encoder For Described List {instance.Descriptor.ToString()}");
            }
            encoder(buffer, instance);
        }

        public static void DecodeValue(ByteBuffer buffer, DescribedList instance)
        {
            Action<ByteBuffer, DescribedList> decoder;
            if (!knownFrameDecoders.TryGetValue(instance.Descriptor.Code, out decoder))
            {
                throw new AmqpException(ErrorCode.InternalError, $"Missing Decoder For Described List {instance.Descriptor.ToString()}");
            }
            decoder(buffer, instance);
        }

        private static readonly Dictionary<ulong, Descriptor> knownDescribedTypeDescriptors = new Dictionary<ulong, Descriptor>();
        private static readonly Dictionary<ulong, Func<object>> knownDescribedTypeConstructors = new Dictionary<ulong, Func<object>>();
        private static readonly Dictionary<ulong, Action<ByteBuffer, DescribedList>> knownFrameDecoders = new Dictionary<ulong, Action<ByteBuffer, DescribedList>>();
        private static readonly Dictionary<ulong, Action<ByteBuffer, DescribedList>> knownFrameEncoders = new Dictionary<ulong, Action<ByteBuffer, DescribedList>>();

        private static void CompilerConstructor(Descriptor descriptor, Type describedType)
        {
            var ctor = describedType.GetConstructor(new Type[0]);
            if (ctor != null)
            {
                knownDescribedTypeConstructors.Add(descriptor.Code, () => ctor.Invoke(null));
            }
        }

        private static void CompileEncoder(Descriptor descriptor, Type describedListType)
        {
            // TODO: this action ends up doing a lot of boxing since we use
            // reflection to set the property values. In the future this can optimized
            // by compiling something at runtime via emmitting IL or compiling a strongly typed expression.

            var properties = describedListType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.CanRead)
                .Where(x => x.GetCustomAttribute<AmqpDescribedListIndexAttribute>(false) != null)
                .Select(x => new
                {
                    Name = x.Name,
                    PropertyType = x.PropertyType,
                    Getter = new Func<object, object>(x.GetValue), // TODO boxing!!!
                    ListIndex = x.GetCustomAttribute<AmqpDescribedListIndexAttribute>().Index,
                })
                .ToDictionary(x => x.ListIndex);

            var getIndexedValue = new Action<object, ByteBuffer, int, bool>((_instance, _buffer, _index, _arrayEncoding) =>
            {
                if (!properties.ContainsKey(_index))
                {
                    throw new AmqpException(ErrorCode.InternalError, $"Invalid AMQP Frame[{describedListType.FullName}] as Index[{_index}]");
                }
                var prop = properties[_index];
                var propertyType = prop.PropertyType;

                // special handling of nested described type
                if (typeof(DescribedType).IsAssignableFrom(propertyType))
                {
                    var describedValue = (prop.Getter(_instance) as DescribedType);
                    if (describedValue == null)
                    {
                        Encoder.WriteNull(_buffer);
                        return;
                    }
                    describedValue.Encode(_buffer);
                    return;
                }

                var propValue = prop.Getter(_instance);

                // special handling of null prop values
                if (propValue == null)
                {
                    Encoder.WriteNull(_buffer);
                    return;
                }

                // special handling of Nullable<>
                if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    propertyType = Nullable.GetUnderlyingType(propertyType);

                var codec = Encoder.GetTypeCodec(propertyType);
                if (codec == null)
                {
                    throw new AmqpException(ErrorCode.InternalError, $"Could Not Find Type Codec For {prop.PropertyType} at Index[{_index}].{prop.Name}");
                }
                if (!propertyType.IsAssignableFrom(codec.Type))
                {
                    throw new AmqpException(ErrorCode.InternalError, $"Cannot Encode Type {codec.Type} into {prop.PropertyType} at Index[{_index}].{prop.Name}");
                }

                codec.EncodeBoxedValue(_buffer, propValue, _arrayEncoding); // TODO boxing!!!
            });

            var encoder = new Action<ByteBuffer, DescribedList>((buffer, instance) =>
            {
                // When the trailing elements of the list representation are null, they MAY be omitted.
                // Find the last not null index (or -1 if all are null), list length = (index + 1)
                var lastNotNullIndex =
                    properties
                        .Where(x => x.Value.Getter(instance) != null)
                        .Select(x => (int?)x.Key)
                        .OrderBy(x => x)
                        .LastOrDefault() ?? -1;
                Encoder.WriteList(buffer, (lastNotNullIndex + 1), (_buffer, _index, _arrayEncoding) => getIndexedValue(instance, _buffer, _index, _arrayEncoding), true);
            });

            knownFrameEncoders.Add(descriptor.Code, encoder);
        }

        private static void CompileDecoder(Descriptor descriptor, Type describedListType)
        {
            // TODO: this action ends up doing a lot of boxing since we use
            // reflection to set the property values. In the future this can optimized
            // by compiling something at runtime via emmitting IL or compiling a strongly typed expression.

            var properties = describedListType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.CanWrite)
                .Where(x => x.GetCustomAttribute<AmqpDescribedListIndexAttribute>(false) != null)
                .Select(x => new
                {
                    Name = x.Name,
                    PropertyType = x.PropertyType,
                    Setter = new Action<object, object>(x.SetValue), // TODO boxing!!!
                    ListIndex = x.GetCustomAttribute<AmqpDescribedListIndexAttribute>().Index,
                })
                .ToDictionary(x => x.ListIndex);

            var setIndexedValue = new Action<object, ByteBuffer, int>((_instance, _buffer, _index) =>
            {
                if (!properties.ContainsKey(_index))
                {
                    throw new AmqpException(ErrorCode.InternalError, $"Invalid AMQP Frame[{describedListType.FullName}] at Index[{_index}]");
                }
                var prop = properties[_index];
                var propertyType = prop.PropertyType;
                bool isArrayProperty = propertyType.IsArray;

                var formatCode = Encoder.ReadFormatCode(_buffer);
                if (formatCode == FormatCode.Null)
                {
                    // null value, return
                    return;
                }

                // special handling of nested described type
                if (formatCode == FormatCode.Described)
                {
                    prop.Setter(_instance, Encoder.ReadDescribed(_buffer, formatCode));
                    return;
                }

                if (formatCode == FormatCode.Array32 || formatCode == FormatCode.Array8)
                {
                    throw new NotImplementedException("Have Not Implemented Array Decoding");
                }

                var codec = Encoder.GetTypeCodec(formatCode);
                if (codec == null)
                {
                    throw new AmqpException(ErrorCode.InternalError, $"Could Not Find Type Codec For FormateCode {formatCode.ToHex()} at Index[{_index}].{prop.Name}");
                }

                // special handling of Nullable<>
                if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    propertyType = Nullable.GetUnderlyingType(propertyType);
                // special handling of single value into an array
                if (isArrayProperty)
                    propertyType = propertyType.GetElementType();

                if (!propertyType.IsAssignableFrom(codec.Type))
                {
                    throw new AmqpException(ErrorCode.InternalError, $"Cannot Decode Type {codec.Type} into {prop.PropertyType} at Index[{_index}].{prop.Name}");
                }

                var decodedValue = codec.DecodeBoxedValue(_buffer, formatCode); // TODO: boxing!!!
                if (isArrayProperty)
                {
                    var array = (System.Collections.IList)Activator.CreateInstance(prop.PropertyType, new object[] { 1 });
                    array[0] = decodedValue;
                    decodedValue = array;
                }
                prop.Setter(_instance, decodedValue); // TODO boxing!!!
            });

            var decoder = new Action<ByteBuffer, DescribedList>((buffer, instance) =>
            {
                var formatCode = Encoder.ReadFormatCode(buffer);
                // ensure it is a list format code or null
                if (formatCode == FormatCode.List0 ||
                    formatCode == FormatCode.List8 ||
                    formatCode == FormatCode.List32 ||
                    formatCode == FormatCode.Null)
                {
                    Encoder.ReadList(buffer, formatCode, (_buffer, _index) => setIndexedValue(instance, _buffer, _index));
                }
                else
                {
                    throw new AmqpException(ErrorCode.FramingError, $"Invalid Format Code For Frame: {formatCode.ToHex()}");
                }
            });

            knownFrameDecoders.Add(descriptor.Code, decoder);
        }

        internal static bool IsKnownDescribedList(Descriptor descriptor)
        {
            return knownDescribedTypeDescriptors.ContainsKey(descriptor.Code);
        }

        internal static object DecodeDescribedList(ByteBuffer buffer, ulong code)
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
