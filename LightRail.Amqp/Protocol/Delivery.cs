using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightRail.Amqp.Framing;

namespace LightRail.Amqp.Protocol
{
    public class Delivery : ConcurrentLinkedList<Delivery>.Node
    {
        public uint? DeliveryId { get; internal set; }
        public byte[] DeliveryTag { get; internal set; }
        public ByteBuffer MessageBuffer { get; internal set; }
        public bool Role { get; internal set; }
        public bool Settled { get; internal set; }
        public DeliveryState State { get; internal set; }
    }
}
