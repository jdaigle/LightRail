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

        protected override void EncodeValue(ByteBuffer buffer)
        {
            Encoder.WriteMap(buffer, Map, true);
        }

        protected override void DecodeValue(ByteBuffer buffer)
        {
            map = Encoder.ReadMap(buffer, Encoder.ReadFormatCode(buffer));
        }
    }
}
