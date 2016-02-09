using System;
using System.Linq;
using System.Reflection;

namespace LightRail.Amqp.Types
{
    public static class DescribedListCodec
    {
        internal static Action<ByteBuffer, DescribedType> CompileEncoder(Descriptor descriptor, Type describedListType)
        {
            // TODO: this action ends up doing a lot of boxing since we use
            // reflection to set the property values. In the future this can optimized
            // by compiling something at runtime via emmitting IL or compiling a strongly typed expression.

            var properties = describedListType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.CanRead)
                .Where(x => x.GetCustomAttribute<AmqpDescribedListIndexAttribute>(false) != null)
                .Select(x => new PropertyEncodingInfo(x))
                .ToDictionary(x => x.DescribedListIndex);

            var encodeValueAtIndex = new Action<ByteBuffer, object, int, bool>((_buffer, _instance, _index, _arrayEncoding) =>
            {
                if (!properties.ContainsKey(_index))
                {
                    throw new AmqpException(ErrorCode.InternalError, $"Invalid AMQP Frame[{describedListType.FullName}] as Index[{_index}]");
                }
                EncodeProperty(_buffer, _instance, properties[_index], _arrayEncoding);
            });

            return new Action<ByteBuffer, DescribedType>((buffer, instance) =>
            {
                // When the trailing elements of the list representation are null, they MAY be omitted.
                // Find the last not null index (or -1 if all are null), list length = (index + 1)
                var lastNotNullIndex =
                    properties
                        .Where(x => x.Value.GetValue(instance) != null)
                        .Select(x => (int?)x.Key)
                        .OrderBy(x => x)
                        .LastOrDefault() ?? -1;
                Encoder.WriteList(buffer, (lastNotNullIndex + 1), (_buffer, _index, _arrayEncoding) => encodeValueAtIndex(_buffer, instance, _index, _arrayEncoding), true);
            });
        }

        public class PropertyEncodingInfo
        {
            public PropertyEncodingInfo(PropertyInfo property)
            {
                Property = property;
                PropertyType = property.PropertyType;

                IsNullable = Property.PropertyType.IsGenericType && Property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
                if (IsNullable)
                    PropertyType = Nullable.GetUnderlyingType(Property.PropertyType);

                IsArrayType = Property.PropertyType.IsArray && !(typeof(byte[]).IsAssignableFrom(Property.PropertyType)); // ignore byte[] is a special type
                if (IsArrayType)
                    PropertyType = Property.PropertyType.GetElementType();

                IsDescribedType = typeof(DescribedType).IsAssignableFrom(Property.PropertyType);
                DescribedListIndex = property.GetCustomAttribute<AmqpDescribedListIndexAttribute>().Index;
            }

            public PropertyInfo Property { get; }
            public Type PropertyType { get; }
            public bool IsNullable { get; }
            public bool IsArrayType { get; }

            public object GetValue(object instance) => Property.GetValue(instance);
            public void SetValue(object instance, object value) => Property.SetValue(instance, value);

            public bool IsDescribedType { get; }
            public int DescribedListIndex { get; }
        }

        private static void EncodeProperty(ByteBuffer buffer, object instance, PropertyEncodingInfo propertyInfo, bool arrayEncoding)
        {
            var value = propertyInfo.GetValue(instance); // TODO: boxing!

            // special handling of nested described type
            if (propertyInfo.IsDescribedType)
            {
                var describedValue = (value as DescribedType);
                if (describedValue == null)
                {
                    Encoder.WriteNull(buffer);
                    return;
                }
                describedValue.Encode(buffer);
                return;
            }

            // special handling of null prop values
            if (value == null)
            {
                Encoder.WriteNull(buffer);
                return;
            }

            var codec = Encoder.GetTypeCodec(propertyInfo.PropertyType);
            if (codec == null)
            {
                throw new AmqpException(ErrorCode.InternalError, $"Could Not Find Type Codec For {propertyInfo.Property.PropertyType} {propertyInfo.Property.Name}");
            }
            if (!propertyInfo.Property.PropertyType.IsAssignableFrom(codec.Type))
            {
                throw new AmqpException(ErrorCode.InternalError, $"Cannot Encode Type {codec.Type} into {propertyInfo.Property.PropertyType} {propertyInfo.Property.Name}");
            }

            codec.EncodeBoxedValue(buffer, value, arrayEncoding); // TODO boxing!!!
        }

        internal static Action<ByteBuffer, DescribedType> CompileDecoder(Descriptor descriptor, Type describedListType)
        {
            // TODO: this action ends up doing a lot of boxing since we use
            // reflection to set the property values. In the future this can optimized
            // by compiling something at runtime via emmitting IL or compiling a strongly typed expression.

            var properties = describedListType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.CanWrite)
                .Where(x => x.GetCustomAttribute<AmqpDescribedListIndexAttribute>(false) != null)
                .Select(x => new PropertyEncodingInfo(x))
                .ToDictionary(x => x.DescribedListIndex);

            var setIndexedValue = new Action<ByteBuffer, object, int>((_buffer, _instance, _index) =>
            {
                if (!properties.ContainsKey(_index))
                {
                    throw new AmqpException(ErrorCode.InternalError, $"Invalid AMQP Frame[{describedListType.FullName}] at Index[{_index}]");
                }
                DecodeProperty(_buffer, _instance, properties[_index]);
            });

            return new Action<ByteBuffer, DescribedType>((buffer, instance) =>
            {
                var formatCode = AmqpCodec.DecodeFormatCode(buffer);
                // ensure it is a list format code or null
                if (formatCode == FormatCode.List0 ||
                    formatCode == FormatCode.List8 ||
                    formatCode == FormatCode.List32 ||
                    formatCode == FormatCode.Null)
                {
                    Encoder.ReadList(buffer, formatCode, (_buffer, _index) => setIndexedValue(_buffer, instance, _index));
                }
                else
                {
                    throw new AmqpException(ErrorCode.FramingError, $"Invalid Format Code For Frame: {formatCode.ToHex()}");
                }
            });
        }

        private static void DecodeProperty(ByteBuffer buffer, object instance, PropertyEncodingInfo propertyInfo)
        {
            var formatCode = AmqpCodec.DecodeFormatCode(buffer);
            if (formatCode == FormatCode.Null)
            {
                // null value, return
                return;
            }

            // special handling of nested described type
            if (formatCode == FormatCode.Described)
            {
                propertyInfo.SetValue(instance, AmqpCodec.DecodeDescribedType(buffer, formatCode));
                return;
            }

            if (formatCode == FormatCode.Array32 || formatCode == FormatCode.Array8)
            {
                throw new NotImplementedException("Have Not Implemented Array Decoding");
            }

            var codec = Encoder.GetTypeCodec(formatCode);
            if (codec == null)
            {
                throw new AmqpException(ErrorCode.InternalError, $"Could Not Find Type Codec For FormateCode {formatCode.ToHex()} {propertyInfo.Property.PropertyType} {propertyInfo.Property.Name}");
            }

            if (!propertyInfo.PropertyType.IsAssignableFrom(codec.Type))
            {
                throw new AmqpException(ErrorCode.InternalError, $"Cannot Decode Type {codec.Type} into {propertyInfo.PropertyType} {propertyInfo.Property.PropertyType} {propertyInfo.Property.Name}");
            }

            var decodedValue = codec.DecodeBoxedValue(buffer, formatCode); // TODO: boxing!!!

            if (propertyInfo.IsArrayType)
            {
                var array = (System.Collections.IList)Activator.CreateInstance(propertyInfo.Property.PropertyType, new object[] { 1 });
                array[0] = decodedValue;
                decodedValue = array;
            }

            propertyInfo.SetValue(instance, decodedValue); // TODO boxing!!!
        }
    }
}
