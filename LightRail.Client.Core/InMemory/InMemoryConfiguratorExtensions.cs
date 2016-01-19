using System;
using LightRail.Client.Config;

namespace LightRail.Client.InMemory
{
    public static class InMemoryConfiguratorExtensions
    {
        public static IBusControl CreateFromInMemory(this ServiceBusFactory factory, Action<ServiceBusConfigurator<InMemoryServiceBusConfiguration>> cfg)
        {
            return factory.Create(cfg);
        }

        public static void ReceiveFrom(this ServiceBusConfigurator<InMemoryServiceBusConfiguration> configurator,
            object host, string address, Action<MessageReceiverConfigurator<InMemoryMessageReceiverConfiguration>> cfg)
        {
            configurator.ReceiveFrom<InMemoryMessageReceiverConfiguration>(host, address, cfg);
        }
    }
}
