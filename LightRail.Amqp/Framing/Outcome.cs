using LightRail.Amqp.Types;

namespace LightRail.Amqp.Framing
{
    public abstract class Outcome : DeliveryState
    {
        protected Outcome(Descriptor descriptor) : base(descriptor) { }
    }
}