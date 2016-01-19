﻿using LightRail.Client.Config;
using LightRail.Client.Transport;

namespace LightRail.Client.InMemoryQueue
{
    public class InMemoryQueueMessageReceiverConfiguration : BaseMessageReceiverConfiguration
    {
        public override ITransportReceiver CreateTransportReceiver()
        {
            return new InMemoryQueueTransportReceiver(this, this.ServiceBusConfig);
        }
    }
}
