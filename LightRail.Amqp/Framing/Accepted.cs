using LightRail.Amqp.Types;

namespace LightRail.Amqp.Framing
{
    public sealed class Accepted : Outcome
    {
        public Accepted() : base(DescribedTypeCodec.Accepted) { }
    }
}