using System;
using System.Collections.Generic;
using System.Reflection;
using LightRail.ServiceBus.Dispatch;
using LightRail.ServiceBus.FastServiceLocator;
using LightRail.ServiceBus.Pipeline;
using LightRail.ServiceBus.Reflection;
using LightRail.ServiceBus.Transport;

namespace LightRail.ServiceBus.Config
{
    public abstract class BaseServiceBusConfig
    {
        /// <summary>
        /// The collection of message handlers shared by all message receivers.
        /// </summary>
        public MessageHandlerCollection MessageHandlers { get; } = new MessageHandlerCollection();
        /// <summary>
        /// Enables looking up interfaced mapped to generated concrete types.
        /// and vice versa.
        ///  Defaults to a new instance of "LightRail.ServiceBus.Reflection.ReflectionMessageMapper"
        /// </summary>
        public IMessageMapper MessageMapper { get; } = new ReflectionMessageMapper();
        /// <summary>
        /// A service locator used to resolve message handler dependencies.
        /// Defaults to a new instance of "LightRail.ServiceBus.FastServiceLocator.FastServiceLocatorImpl"
        /// </summary>
        public IServiceLocator ServiceLocator { get; set; } = new FastServiceLocatorImpl(new FastContainer());

        /// <summary>
        /// A set of static message endpoint mappings for resolving static message routes.
        /// </summary>
        public IList<MessageEndpointMapping> MessageEndpointMappings { get; } = new List<MessageEndpointMapping>();
        /// <summary>
        /// An ordered list of behaviors that will execute for each message.
        /// </summary>
        public IList<PipelinedBehavior> PipelinedBehaviors { get; } = new List<PipelinedBehavior>();
        /// <summary>
        /// A set of message receivers configs.
        /// </summary>
        public IList<BaseMessageReceiverConfiguration> MessageReceivers { get; } = new List<BaseMessageReceiverConfiguration>();
        /// <summary>
        /// Creates an instance of the configured transport sender.
        /// </summary>
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

        public void AddMessageEndpointMappingFromAssembly(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes(t => !t.IsAbstract && typeof(AbstractMessageEndpointRegistry).IsAssignableFrom(t)))
            {
                var registry = Activator.CreateInstance(type) as AbstractMessageEndpointRegistry;
                foreach (var mapping in registry.MessageEndpointMappings)
                {
                    MessageEndpointMappings.Add(mapping);
                }
            }
        }

        public void AddMessageEndpointMappingFromAssemblyContaining<T>()
        {
            AddMessageEndpointMappingFromAssembly(typeof(T).Assembly);
        }

        public void AddPipelinedBehavior(PipelinedBehavior behavior)
        {
            PipelinedBehaviors.Add(behavior);
        }

        public void ReceiveFrom<T>(Action<T> cfg)
            where T : BaseMessageReceiverConfiguration, new()
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
