using LightRail.Amqp.Types;

namespace LightRail.Amqp.Framing
{
    public abstract class DeliveryState : DescribedList
    {
        protected DeliveryState(Descriptor descriptor) : base(descriptor) { }
    }
}
