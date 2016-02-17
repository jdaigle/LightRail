using LightRail.Amqp.Types;

namespace LightRail.Amqp.Framing
{
    public sealed class Released : Outcome
    {
        public Released() : base(DescribedTypeCodec.Released) { }
    }
}