using LightRail.Amqp.Types;

namespace LightRail.Amqp.Framing
{
    public abstract class Modified : DescribedList
    {
        protected Modified() : base(DescribedListCodec.Modified) { }
    }
}