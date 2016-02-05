using LightRail.Amqp.Types;

namespace LightRail.Amqp.Framing
{
    public abstract class Received : DeliveryState
    {
        protected Received() : base(DescribedTypeCodec.Received) { }

        [AmqpDescribedListIndex(0)]
        public uint SectionNumber { get; set; }

        [AmqpDescribedListIndex(1)]
        public ulong SectionOffset { get; set; }
    }
}