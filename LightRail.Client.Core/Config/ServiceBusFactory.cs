using System;

namespace LightRail.Client.Config
{
    public class ServiceBusFactory
    {
        public static readonly ServiceBusFactory Factory = new ServiceBusFactory();

        private ServiceBusFactory() { }

        public IBusControl Create<TConfig>(Action<ServiceBusConfigurator<TConfig>> cfg)
            where TConfig : class, IServiceBusConfig, new()
        {
            var configurator = new ServiceBusConfigurator<TConfig>();
            cfg(configurator);
            var bus = configurator.CreateServiceBus();
            return bus;
        }
    }
}
