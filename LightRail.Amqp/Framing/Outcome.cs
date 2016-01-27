using LightRail.Amqp.Types;

namespace LightRail.Amqp.Framing
{
    public abstract class Outcome : DescribedList
    {
        protected Outcome(Descriptor descriptor) : base(descriptor) { }
    }
}