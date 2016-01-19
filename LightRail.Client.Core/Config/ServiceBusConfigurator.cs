namespace LightRail.Client.Config
{
    public class ServiceBusConfigurator<TConfig> 
        where TConfig : IServiceBusConfig, new()
    {
        public TConfig Config { get; }

        public ServiceBusConfigurator()
        {
            Config = new TConfig();
        }

        public IBusControl CreateServiceBus()
        {
            return new PipelineServiceBus(Config);
        }
    }
}