using LightRail.Amqp.Types;

namespace LightRail.Amqp.Framing
{
    public abstract class Received : DescribedList
    {
        protected Received() : base(DescribedListCodec.Received) { }
    }
}