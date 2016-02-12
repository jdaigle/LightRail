using System;
using LightRail.Amqp.Framing;

namespace LightRail.Amqp.Protocol
{
    public class TestContainer : IContainer
    {
        public TestContainer()
        {
            ContainerId = Guid.NewGuid().ToString();
        }

        public string ContainerId { get; }

        public void OnLinkAttached(AmqpLink link)
        {
        }

        public bool CanAttachLink(AmqpLink newLink, Attach attach)
        {
            return true;
        }

        public void OnDelivery(AmqpLink link, Delivery delivery)
        {
        }
    }
}
