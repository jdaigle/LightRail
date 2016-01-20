using System;
using LightRail.Client.Amqp.Config;
using LightRail.Client.Config;

namespace LightRail.Client.Amqp
{
    public static class AmqpConfiguratorExtensions
    {
        public static IBusControl CreateFromAmqp(this ServiceBusFactory factory, Action<ServiceBusConfigurator<AmqpServiceBusConfiguration>> cfg)
        {
            return factory.Create(cfg);
        }

        public static AmqpHost Host(this ServiceBusConfigurator<AmqpServiceBusConfiguration> _configurator,
            string uri,
            Action<AmqpHost> configurator)
        {
            return _configurator.Host<AmqpHost>(uri, configurator);
        }

        public static void ReceiveFrom(this ServiceBusConfigurator<AmqpServiceBusConfiguration> configurator,
            AmqpHost host, string address, Action<MessageReceiverConfigurator<AmqpMessageReceiverConfiguration>> cfg)
        {
            configurator.ReceiveFrom<AmqpMessageReceiverConfiguration>(host, address, cfg);
        }
    }
}
