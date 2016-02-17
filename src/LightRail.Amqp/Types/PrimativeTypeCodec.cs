using System;

namespace LightRail.Amqp.Types
{
    internal abstract class PrimativeTypeCodec
    {
        public abstract Type Type { get; }
        public abstract void EncodeBoxedValue(ByteBuffer buffer, object value, bool arrayEncoding);
        public abstract object DecodeBoxedValue(ByteBuffer buffer, byte formatCode);
    }

    internal class PrimativeTypeCodec<T> : PrimativeTypeCodec
    {
        public PrimativeTypeCodec()
        {
            Type = typeof(T);
        }
        public override Type Type { get; }
        public Encode<T> Encode { get; set; }
        public Decode<T> Decode { get; set; }

        public override void EncodeBoxedValue(ByteBuffer buffer, object value, bool arrayEncoding)
        {
            Encode(buffer, (T)value, arrayEncoding);
        }

        public override object DecodeBoxedValue(ByteBuffer buffer, byte formatCode)
        {
            return Decode(buffer, formatCode);
        }
    }

    internal class NullTypeCodec : PrimativeTypeCodec<object>
    {
        public NullTypeCodec()
        {
            Encode = (buffer, value, arrayEncoding) => Encoder.WriteNull(buffer);
            Decode = (buffer, _byte) => null;
        }

        public override Type Type { get { return null; } }
    }

    delegate void Encode<T>(ByteBuffer buffer, T value, bool arrayEncoding);
    delegate T Decode<T>(ByteBuffer buffer, byte formatCode);
}
