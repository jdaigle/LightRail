using System;
using System.Collections.Generic;
using LightRail.Client.Dispatch;
using LightRail.Client.FastServiceLocator;
using LightRail.Client.Pipeline;
using LightRail.Client.Reflection;
using LightRail.Client.Transport;

namespace LightRail.Client.Config
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
    }
}
