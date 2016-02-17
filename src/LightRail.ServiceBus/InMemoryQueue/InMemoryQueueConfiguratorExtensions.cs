using System;
using LightRail.ServiceBus.Config;
using LightRail.ServiceBus.InMemoryQueue.Config;

namespace LightRail.ServiceBus.InMemoryQueue
{
    public static class InMemoryQueueConfiguratorExtensions
    {
        public static IBusControl CreateFromInMemory(this ServiceBusFactory factory, Action<InMemoryQueueServiceBusConfiguration> cfg)
        {
            return factory.Create(cfg);
        }

        public static void ReceiveFrom(this InMemoryQueueServiceBusConfiguration configurator,
            string address,
            Action<InMemoryQueueMessageReceiverConfiguration> cfgAction)
        {
            configurator.ReceiveFrom<InMemoryQueueMessageReceiverConfiguration>(cfg =>
            {
                cfg.Address = address;
                if (cfg != null)
                    cfgAction(cfg);
            });
        }
    }
}
