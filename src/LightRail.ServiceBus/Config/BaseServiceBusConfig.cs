using System;
using System.Collections.Generic;
using LightRail.ServiceBus.Dispatch;
using LightRail.ServiceBus.FastServiceLocator;
using LightRail.ServiceBus.Pipeline;
using LightRail.ServiceBus.Reflection;
using LightRail.ServiceBus.Transport;

namespace LightRail.ServiceBus.Config
{
    public abstract class BaseServiceBusConfig : IServiceBusConfig
    {
        protected BaseServiceBusConfig()
        {
            MessageHandlers = new MessageHandlerCollection();
            MessageEndpointMappings = new List<MessageEndpointMapping>();
            PipelinedBehaviors = new List<PipelinedBehavior>();
            MessageReceivers = new List<IMessageReceiverConfiguration>();

            // defaults
            ServiceLocator = new FastServiceLocatorImpl(new FastContainer());
            MessageMapper = new ReflectionMessageMapper();
        }

        public IMessageMapper MessageMapper { get; set; }
        public IServiceLocator ServiceLocator { get; set; }
        public MessageHandlerCollection MessageHandlers { get; }
        public IList<MessageEndpointMapping> MessageEndpointMappings { get; }
        public IList<PipelinedBehavior> PipelinedBehaviors { get; }
        public IList<IMessageReceiverConfiguration> MessageReceivers { get; }

        public abstract ITransportSender CreateTransportSender();

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
            MessageEndpointMappings.Add(mapping);
        }

        public void AddPipelinedBehavior(PipelinedBehavior behavior)
        {
            PipelinedBehaviors.Add(behavior);
        }

        public void ReceiveFrom<T>(Action<T> cfg)
            where T : IMessageReceiverConfiguration, new()
        {
            var _config = new T();
            if (_config is BaseMessageReceiverConfiguration)
            {
                (_config as BaseMessageReceiverConfiguration).ServiceBusConfig = this;
            }
            cfg(_config);
            MessageReceivers.Add(_config);
        }

        public IBusControl CreateServiceBus()
        {
            return new PipelineServiceBus(this);
        }
    }
}
