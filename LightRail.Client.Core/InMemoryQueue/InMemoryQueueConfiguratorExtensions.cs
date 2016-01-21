using System;
using LightRail.Client.Config;
using LightRail.Client.InMemoryQueue.Config;

namespace LightRail.Client.InMemoryQueue
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
