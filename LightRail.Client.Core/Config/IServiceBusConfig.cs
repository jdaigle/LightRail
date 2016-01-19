using System;
using System.Collections.Generic;
using LightRail.Client.Dispatch;
using LightRail.Client.Pipeline;

namespace LightRail.Client.Config
{
    public interface IServiceBusConfig
    {
        MessageHandlerCollection MessageHandlers { get; }
        IServiceLocator ServiceLocator { get; }
        IList<MessageEndpointMapping> MessageEndpointMappings { get; }
        IList<PipelinedBehavior> PipelinedBehaviors { get; }

        /// <summary>
        /// Adds a static route mapping a specific type to an endpoint address.
        /// </summary>
        void AddMessageEndpointMapping<T>(string endpoint);
        /// <summary>
        /// Adds a static route mapping a specific type to an endpoint address.
        /// </summary>
        void AddMessageEndpointMapping(string endpoint, Type type);
        /// <summary>
        /// Adds a static route mapping all types in an specific assembly, or just a single type, to an endpoint address.
        /// </summary>
        void AddMessageEndpointMapping(string endpoint, string assemblyName, string typeFullName = null);
        /// <summary>
        /// Adds a static route mapping.
        /// </summary>
        void AddMessageEndpointMapping(MessageEndpointMapping mapping);

        /// <summary>
        /// Adds a PipelinesBehavior which executes for each received message. Behaviors are executed in the order they are
        /// added to the list. A behavior can be added twice.
        /// </summary>
        void AddPipelinedBehavior(PipelinedBehavior behavior);

        void ReceiveFrom(object host, string address, Action<IQueueReceiverConfiguration> cfg);
    }
}