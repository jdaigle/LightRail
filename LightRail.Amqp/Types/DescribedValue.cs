namespace LightRail.Amqp.Types
{
    public sealed class DescribedValue<T> : DescribedType
    {
        public DescribedValue(Descriptor descriptor, T value)
            : base(descriptor)
        {
            Value = value;
        }

        public T Value { get; private set; }

        protected override void EncodeValue(ByteBuffer buffer, bool arrayEncoding)
        {
            Encoder.GetTypeCodec<T>().Encode(buffer, Value, arrayEncoding);
        }

        protected override void DecodeValue(ByteBuffer buffer)
        {
            Value = AmqpCodec.DecodeObject<T>(buffer);
        }

        public override int GetHashCode()
        {
            return Descriptor.GetHashCode() ^ Value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is DescribedValue<T>)
            {
                return Equals(this, obj as DescribedValue<T>);
            }
            return false;
        }

        public static bool Equals(DescribedValue<T> first, DescribedValue<T> second)
        {
            if (first == null && second == null)
            {
                return true;
            }
            if (first == null && second != null)
            {
                return false;
            }
            if (first != null && second == null)
            {
                return false;
            }
            return first.Descriptor.Equals(second.Descriptor) && first.Value.Equals(second.Value);
        }
    }
}
