using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using LightRail.Amqp;
using LightRail.Amqp.Framing;
using LightRail.Amqp.Messaging;
using LightRail.Amqp.Protocol;
using LightRail.Server.Queuing;

namespace LightRail.Server
{
    public class HostContainer : IContainer
    {
        public static readonly HostContainer Instance = new HostContainer();

        private HostContainer()
        {
            ContainerId = Guid.NewGuid().ToString(); // TODO: Generate once and then persist?

            logWriter = new QueueLogWriter();
        }

        private readonly QueueLogWriter logWriter;
        private readonly ConcurrentDictionary<string, ConcurrentQueue> queues = new ConcurrentDictionary<string, ConcurrentQueue>();
        private readonly Dictionary<string, ConcurrentQueue> linkNameToQueue = new Dictionary<string, ConcurrentQueue>();

        public string ContainerId { get; }

        public void OnLinkAttached(AmqpLink link)
        {
            if (link.IsReceiverLink)
            {
                link.SetLinkCredit(25);
                linkNameToQueue.Add(link.Name, queues[link.TargetAddress.ToLowerInvariant()]);
            }
        }

        public bool CanAttachLink(AmqpLink newLink, Attach attach)
        {
            var queueName = "";

            if (attach.IsReceiver)
                queueName = attach.Source.Address.ToLowerInvariant();
            if (!attach.IsReceiver)
                queueName = attach.Target.Address.ToLowerInvariant();

            queues.GetOrAdd(queueName, x => new ConcurrentQueue(0, logWriter));

            return true;
        }

        public void OnDelivery(AmqpLink link, Delivery delivery)
        {
            var message = AnnotatedMessage.Decode(delivery.PayloadBuffer);
            var queue = linkNameToQueue[link.Name];
            queue.Enqueue(message);

            link.SendDeliveryDisposition(delivery, new Accepted(), true);
        }
    }
}
