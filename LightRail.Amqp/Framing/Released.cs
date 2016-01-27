using LightRail.Amqp.Types;

namespace LightRail.Amqp.Framing
{
    public abstract class Released : DescribedList
    {
        protected Released() : base(DescribedListCodec.Released) { }
    }
}