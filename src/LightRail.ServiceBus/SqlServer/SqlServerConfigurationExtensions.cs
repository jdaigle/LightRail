using System;
using LightRail.ServiceBus.Config;
using LightRail.ServiceBus.SqlServer.Config;

namespace LightRail.ServiceBus.SqlServer
{
    public static class SqlServerConfigurationExtensions
    {
        public static IBusControl CreateFromServiceBroker(this ServiceBusFactory factory, Action<ServiceBrokerServiceBusConfiguration> cfg)
        {
            return factory.Create(cfg);
        }

        public static void ReceiveFrom(this ServiceBrokerServiceBusConfiguration configurator,
            Action<ServiceBrokerServiceBusReceiverConfiguration> cfgAction)
        {
            configurator.ReceiveFrom<ServiceBrokerServiceBusReceiverConfiguration>(cfg =>
            {
                if (cfg != null)
                    cfgAction(cfg);
            });
        }
    }
}
