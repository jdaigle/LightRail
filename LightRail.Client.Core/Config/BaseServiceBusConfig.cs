using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightRail.Client.Dispatch;
using LightRail.Client.FastServiceLocator;
using LightRail.Client.Pipeline;

namespace LightRail.Client.Config
{
    public abstract class BaseServiceBusConfig : IServiceBusConfig
    {
        protected BaseServiceBusConfig()
        {
            MessageHandlers = new MessageHandlerCollection();
            MessageEndpointMappings = new List<MessageEndpointMapping>();
            PipelinedBehaviors = new List<PipelinedBehavior>();

            // defaults
            ServiceLocator = new FastServiceLocatorImpl(new FastContainer());
        }

        public IServiceLocator ServiceLocator { get; }
        public MessageHandlerCollection MessageHandlers { get; }
        public IList<MessageEndpointMapping> MessageEndpointMappings { get; }
        public IList<PipelinedBehavior> PipelinedBehaviors { get; }

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
            this.MessageEndpointMappings.Add(mapping);
        }

        public void AddPipelinedBehavior(PipelinedBehavior behavior)
        {
            PipelinedBehaviors.Add(behavior);
        }

        public abstract void ReceiveFrom(object host, string address, Action<IQueueReceiverConfiguration> cfg);
    }
}
