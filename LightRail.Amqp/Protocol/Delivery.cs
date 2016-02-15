using System;
using LightRail.Amqp.Framing;

namespace LightRail.Amqp.Protocol
{
    public class Delivery : ConcurrentLinkedList<Delivery>.Node
    {
        public uint DeliveryId { get; internal set; }
        public byte[] DeliveryTag { get; internal set; }
        public ByteBuffer PayloadBuffer { get; internal set; }
        public AmqpLink Link { get; internal set; }
        public bool Settled { get; internal set; }
        public DeliveryState State { get; internal set; }
        public LinkReceiverSettlementModeEnum ReceiverSettlementMode { get; internal set; }
    }
}
