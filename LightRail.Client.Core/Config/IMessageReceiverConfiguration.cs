using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LightRail.Client.Dispatch;
using LightRail.Client.Pipeline;

namespace LightRail.Client.Config
{
    public interface IMessageReceiverConfiguration
    {
        /// <summary>
        /// A collection of message handlers specific to this message receiver.
        /// </summary>
        MessageHandlerCollection MessageHandlers { get; }

        /// <summary>
        /// Returns a combined set of message handlers including shared message handlers.
        /// </summary>
        MessageHandlerCollection GetCombinedMessageHandlers();
        /// <summary>
        /// Returns a compiled message pipeline.
        /// </summary>
        Func<MessageContext, Task> GetCompiledMessageHandlerPipeline();
    }
}