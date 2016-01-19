using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightRail.Client.Config;

namespace LightRail.Client
{
    public static class ServiceBus
    {
        public static IBusControl Create<TConfig>(Action<ServiceBusConfigurator<TConfig>> cfg)
            where TConfig : IServiceBusConfig, new()
        {
            var configurator = new ServiceBusConfigurator<TConfig>();
            cfg(configurator);
            var bus = configurator.CreateServiceBus();
            return bus;
        }
    }
}
