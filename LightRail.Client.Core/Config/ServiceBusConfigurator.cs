using System;
using LightRail.Client.Pipeline;
using LightRail.Client.Transport;

namespace LightRail.Client.Config
{
    public class ServiceBusConfigurator<TConfig>
        where TConfig : class, IServiceBusConfig, new()
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

        public TTransportHost Host<TTransportHost>(string uri, Action<TTransportHost> configuator)
            where TTransportHost : class, ITransportHost
        {
            TTransportHost host;
            Config.Host = host = (TTransportHost)Activator.CreateInstance(typeof(TTransportHost), uri);
            if (configuator != null)
            {
                configuator(host);
            }
            return host;
        }

        public void ReceiveFrom<T>(ITransportHost host, string address, Action<MessageReceiverConfigurator<T>> cfg)
            where T : IMessageReceiverConfiguration, new()
        {
            var _configurator = new MessageReceiverConfigurator<T>();
            if (_configurator.Config is BaseMessageReceiverConfiguration)
            {
                (_configurator.Config as BaseMessageReceiverConfiguration).ServiceBusConfig = this.Config;
            }
            _configurator.Config.Address = address;
            cfg(_configurator);
            Config.MessageReceivers.Add(_configurator.Config);
        }

        public IBusControl CreateServiceBus()
        {
            return new PipelineServiceBus(Config);
        }
    }
}