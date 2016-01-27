using LightRail.Amqp.Types;

namespace LightRail.Amqp.Framing
{
    public abstract class Accepted : DescribedList
    {
        protected Accepted() : base(DescribedListCodec.Accepted) { }
    }
}