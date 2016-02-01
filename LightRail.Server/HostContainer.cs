using System;
using LightRail.Amqp;
using LightRail.Amqp.Framing;
using LightRail.Amqp.Protocol;

namespace LightRail.Server
{
    public class HostContainer : IContainer
    {
        public static readonly HostContainer Instance = new HostContainer();

        private HostContainer()
        {
            ContainerId = Guid.NewGuid().ToString(); // TODO: Generate once and then persist?
        }

        public string ContainerId { get; }


        public void OnLinkAttached(AmqpLink link)
        {
            if (link.IsReceiverLink)
            {
                link.SetLinkCredit(1000);
            }
        }

        public void OnTransferReceived(AmqpLink link, Transfer transfer, ByteBuffer buffer)
        {
        }
    }
}
