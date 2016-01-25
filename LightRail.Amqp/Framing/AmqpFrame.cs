using System;
using System.Linq;
using System.Reflection;
using LightRail.Amqp.Types;

namespace LightRail.Amqp.Framing
{
    public abstract class AmqpFrame : DescribedList
    {
        protected AmqpFrame(Descriptor descriptor) : base(descriptor) { }

        public static Action<ByteBuffer, AmqpFrame> CompileDecoder(Type amqpFrameType)
        {
            // TODO: this action ends up doing a lot of boxing since we use
            // reflection to set the property values. In the future this can optimized
            // by compiling something at runtime via emmitting IL or compiling a strongly typed expression.

            var properties = amqpFrameType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.CanWrite)
                .Where(x => x.GetCustomAttribute<AmqpFrameIndexAttribute>(false) != null)
                .Select(x => new
                {
                    Name = x.Name,
                    ListIndex = x.GetCustomAttribute<AmqpFrameIndexAttribute>().Index,
                    PropertyType = x.PropertyType,
                    Setter = new Action<object, object>(x.SetValue), // TODO boxing!!!
                })
                .ToDictionary(x => x.ListIndex);

            var setIndexedValue = new Action<object, ByteBuffer, int>((_instance, _buffer, index) =>
            {
                if (!properties.ContainsKey(index))
                {
                    throw new AmqpException(ErrorCode.InternalError, $"Invalid AMQP Frame[{amqpFrameType.FullName}] Index: {index}");
                }
                var prop = properties[index];
                var formatCode = Encoder.ReadFormatCode(_buffer);
                var codec = Encoder.GetTypeCodec(formatCode);
                if (!prop.PropertyType.IsAssignableFrom(codec.Type))
                {
                    throw new InvalidOperationException($"Cannot Decode Type {codec.Type} into {prop.PropertyType} for Index {index} Property = {prop.Name}");
                }
                prop.Setter(_instance, codec.DecodeBoxedValue(_buffer, formatCode)); // TODO boxing!!!
            });

            return (buffer, instance) =>
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
            };
        }
    }
}
