using LightRail.Amqp.Types;

namespace LightRail.Amqp.Framing
{
    public abstract class Modified : DescribedList
    {
        protected Modified() : base(DescribedListCodec.Modified) { }

        [AmqpDescribedListIndex(0)]
        public bool DeliveryFailed { get; set; }
        [AmqpDescribedListIndex(1)]
        public bool UndeliverableHere { get; set; }
        [AmqpDescribedListIndex(2)]
        public Fields MessageAnnotations { get; set; }
    }
}