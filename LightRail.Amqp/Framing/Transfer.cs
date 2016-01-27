using LightRail.Amqp.Types;

namespace LightRail.Amqp.Framing
{
    /// <summary>
    /// </summary>
    public sealed class Transfer : AmqpFrame
    {
        public Transfer() : base(DescribedListCodec.Transfer) { }
    }
}
