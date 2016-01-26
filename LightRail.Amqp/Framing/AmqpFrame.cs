using LightRail.Amqp.Types;

namespace LightRail.Amqp.Framing
{
    public abstract class AmqpFrame : DescribedList
    {
        protected AmqpFrame(Descriptor descriptor) : base(descriptor) { }
    }
}
