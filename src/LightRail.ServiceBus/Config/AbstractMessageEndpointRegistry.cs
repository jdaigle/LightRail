using System;
using System.Collections.Generic;

namespace LightRail.ServiceBus.Config
{
    public abstract class AbstractMessageEndpointRegistry
    {
        protected void AddMessageEndpointMapping<T>(string endpoint)
        {
            AddMessageEndpointMapping(endpoint, typeof(T));
        }

        protected void AddMessageEndpointMapping(string endpoint, Type type)
        {
            AddMessageEndpointMapping(endpoint, type.Assembly.FullName, type.FullName);
        }

        protected void AddMessageEndpointMapping(string endpoint, string assemblyName, string typeFullName = null)
        {
            AddMessageEndpointMapping(new MessageEndpointMapping(endpoint, assemblyName, typeFullName));
        }

        protected void AddMessageEndpointMapping(MessageEndpointMapping mapping)
        {
            MessageEndpointMappings.Add(mapping);
        }

        public IList<MessageEndpointMapping> MessageEndpointMappings { get; } = new List<MessageEndpointMapping>();
    }
}
