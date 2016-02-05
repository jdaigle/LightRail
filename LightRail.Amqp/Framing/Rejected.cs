using LightRail.Amqp.Types;

namespace LightRail.Amqp.Framing
{
    public abstract class Rejected : Outcome
    {
        protected Rejected() : base(DescribedTypeCodec.Rejected) { }

        [AmqpDescribedListIndex(0)]
        public Error Error { get; set; }
    }
}