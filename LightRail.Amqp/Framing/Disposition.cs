using LightRail.Amqp.Types;

namespace LightRail.Amqp.Framing
{
    /// <summary>
    /// </summary>
    public sealed class Disposition : AmqpFrame
    {
        public Disposition() : base(DescribedListCodec.Disposition) { }
    }
}
