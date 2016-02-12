using LightRail.Amqp.Types;

namespace LightRail.Amqp.Framing
{
    public sealed class Modified : Outcome
    {
        public Modified() : base(DescribedTypeCodec.Modified) { }

        [AmqpDescribedListIndex(0)]
        public bool DeliveryFailed { get; set; }
        [AmqpDescribedListIndex(1)]
        public bool UndeliverableHere { get; set; }
        [AmqpDescribedListIndex(2)]
        public Map MessageAnnotations { get; set; }
    }
}