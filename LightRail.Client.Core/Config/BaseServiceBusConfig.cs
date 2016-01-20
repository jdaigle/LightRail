using System;
using System.Collections.Generic;
using LightRail.Client.Dispatch;
using LightRail.Client.FastServiceLocator;
using LightRail.Client.Pipeline;
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
        }

        public IServiceLocator ServiceLocator { get; }
        public MessageHandlerCollection MessageHandlers { get; }
        public IList<MessageEndpointMapping> MessageEndpointMappings { get; }
        public IList<PipelinedBehavior> PipelinedBehaviors { get; }
        public IList<IMessageReceiverConfiguration> MessageReceivers { get; }
        public ITransportHost Host { get; set; }
        public abstract ITransportSender CreateTransportSender();
    }
}
