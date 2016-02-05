using LightRail.Amqp.Types;

namespace LightRail.Amqp.Framing
{
    public abstract class Accepted : Outcome
    {
        protected Accepted() : base(DescribedTypeCodec.Accepted) { }
    }
}