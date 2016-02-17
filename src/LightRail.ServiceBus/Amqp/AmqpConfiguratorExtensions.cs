using System;
using Amqp;
using LightRail.ServiceBus.Amqp.Config;
using LightRail.ServiceBus.Config;

namespace LightRail.ServiceBus.Amqp
{
    public static class AmqpConfiguratorExtensions
    {
        public static IBusControl CreateFromAmqp(this ServiceBusFactory factory, Action<AmqpServiceBusConfiguration> cfg)
        {
            return factory.Create(cfg);
        }

        public static void AmqpAddressFromUri(this AmqpServiceBusConfiguration configurator,
            string uri)
        {
            configurator.AmqpAddress = new Address(uri);
        }

        public static void ReceiveFrom(this AmqpServiceBusConfiguration configurator,
            string address,
            Action<AmqpMessageReceiverConfiguration> cfgAction)
        {
            configurator.ReceiveFrom<AmqpMessageReceiverConfiguration>(cfg =>
            {
                cfg.ReceiverLinkAddress = address;
                if (cfg != null)
                    cfgAction(cfg);
            });
        }
    }
}
