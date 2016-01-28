using LightRail.Amqp.Types;

namespace LightRail.Amqp.Framing
{
    public abstract class Released : Outcome
    {
        protected Released() : base(DescribedListCodec.Released) { }
    }
}