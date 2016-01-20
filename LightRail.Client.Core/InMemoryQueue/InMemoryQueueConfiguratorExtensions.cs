using System;
using LightRail.Client.Config;
using LightRail.Client.Transport;

namespace LightRail.Client.InMemoryQueue
{
    public static class InMemoryQueueConfiguratorExtensions
    {
        public static IBusControl CreateFromInMemory(this ServiceBusFactory factory, Action<ServiceBusConfigurator<InMemoryQueueServiceBusConfiguration>> cfg)
        {
            return factory.Create(cfg);
        }

        public static void ReceiveFrom(this ServiceBusConfigurator<InMemoryQueueServiceBusConfiguration> configurator,
            ITransportHost host, string address, Action<MessageReceiverConfigurator<InMemoryQueueMessageReceiverConfiguration>> cfg)
        {
            configurator.ReceiveFrom<InMemoryQueueMessageReceiverConfiguration>(host, address, cfg);
        }
    }
}
