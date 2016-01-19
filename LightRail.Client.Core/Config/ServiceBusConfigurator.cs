using System;
using LightRail.Client.Pipeline;

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

        public void AddMessageEndpointMapping<T>(string endpoint)
        {
            AddMessageEndpointMapping(endpoint, typeof(T));
        }

        public void AddMessageEndpointMapping(string endpoint, Type type)
        {
            AddMessageEndpointMapping(endpoint, type.Assembly.FullName, type.FullName);
        }

        public void AddMessageEndpointMapping(string endpoint, string assemblyName, string typeFullName = null)
        {
            AddMessageEndpointMapping(new MessageEndpointMapping(endpoint, assemblyName, typeFullName));
        }

        public void AddMessageEndpointMapping(MessageEndpointMapping mapping)
        {
            Config.MessageEndpointMappings.Add(mapping);
        }

        public void AddPipelinedBehavior(PipelinedBehavior behavior)
        {
            Config.PipelinedBehaviors.Add(behavior);
        }

        public void ReceiveFrom<T>(object host, string address, Action<MessageReceiverConfigurator<T>> cfg)
            where T : IMessageReceiverConfiguration, new()
        {
            var _configurator = new MessageReceiverConfigurator<T>();
            if (_configurator.Config is BaseMessageReceiverConfiguration)
            {
                (_configurator.Config as BaseMessageReceiverConfiguration).ServiceBusConfig = this.Config;
            }
            cfg(_configurator);
            Config.MessageReceivers.Add(_configurator.Config);
        }

        public IBusControl CreateServiceBus()
        {
            return new PipelineServiceBus(Config);
        }
    }
}