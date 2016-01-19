using System;

namespace LightRail.Client.Config
{
    public class ServiceBusConfigurator<TConfig> 
        where TConfig : IServiceBusConfig, new()
    {
        public ServiceBusConfigurator()
        {

        }

        public IBusControl CreateServiceBus()
        {
            throw new NotImplementedException();
        }
    }
}