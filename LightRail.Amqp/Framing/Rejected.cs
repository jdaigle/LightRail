using LightRail.Amqp.Types;

namespace LightRail.Amqp.Framing
{
    public sealed class Rejected : Outcome
    {
        public Rejected() : base(DescribedTypeCodec.Rejected) { }

        [AmqpDescribedListIndex(0)]
        public Error Error { get; set; }
    }
}