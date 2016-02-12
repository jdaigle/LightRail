namespace LightRail.Amqp.Types
{
    public abstract class DescribedMap : DescribedType
    {
        protected DescribedMap(Descriptor descriptor)
            : base(descriptor)
        {
        }

        private Map map = new Map();
        public Map Map { get { return this.map; } }

        protected override void EncodeValue(ByteBuffer buffer, bool arrayEncoding)
        {
            Encoder.WriteMap(buffer, Map, arrayEncoding);
        }

        protected override void DecodeValue(ByteBuffer buffer)
        {
            map = Encoder.ReadMap(buffer, AmqpCodec.DecodeFormatCode(buffer));
        }
    }
}
