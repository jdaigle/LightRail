using System;
using Amqp;
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

        public static void AmqpAddressFromUri(this ServiceBusConfigurator<AmqpServiceBusConfiguration> configurator,
            string uri)
        {
            configurator.Config.AmqpAddress = new Address(uri);
        }

        public static void ReceiveFrom(this ServiceBusConfigurator<AmqpServiceBusConfiguration> configurator,
            string address,
            Action<MessageReceiverConfigurator<AmqpMessageReceiverConfiguration>> cfgAction)
        {
            configurator.ReceiveFrom<AmqpMessageReceiverConfiguration>(cfg =>
            {
                cfg.Config.ReceiverLinkAddress = address;
                if (cfg != null)
                    cfgAction(cfg);
            });
        }
    }
}
