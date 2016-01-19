using System.Collections.Generic;
using LightRail.Client.Dispatch;
using LightRail.Client.Pipeline;

namespace LightRail.Client.Config
{
    public interface IServiceBusConfig
    {
        /// <summary>
        /// The collection of message handlers shared by all message receivers.
        /// </summary>
        MessageHandlerCollection MessageHandlers { get; }
        /// <summary>
        /// A service locator used to resolve message handler dependencies.
        /// </summary>
        IServiceLocator ServiceLocator { get; }
        /// <summary>
        /// A set of static message endpoint mappings for resolving static message routes.
        /// </summary>
        IList<MessageEndpointMapping> MessageEndpointMappings { get; }
        /// <summary>
        /// An ordered list of behaviors that will execute for each message.
        /// </summary>
        IList<PipelinedBehavior> PipelinedBehaviors { get; }
        /// <summary>
        /// A set of message receivers configs.
        /// </summary>
        IList<IMessageReceiverConfiguration> MessageReceivers { get; }
    }
}