using System;

namespace LightRail.Client.Config
{
    public class ServiceBusFactory
    {
        public static readonly ServiceBusFactory Factory = new ServiceBusFactory();

        private ServiceBusFactory() { }

        public IBusControl Create<TConfig>(Action<TConfig> cfg)
            where TConfig : class, IServiceBusConfig, new()
        {
            var _config = new TConfig();
            cfg(_config);
            var bus = _config.CreateServiceBus();
            return bus;
        }
    }
}
