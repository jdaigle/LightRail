using System;
using System.Threading.Tasks;
using LightRail.Client.Dispatch;
using LightRail.Client.Transport;

namespace LightRail.Client.Config
{
    public interface IMessageReceiverConfiguration
    {
        /// <summary>
        /// The address from which the message receiver will receiver messages.
        /// </summary>
        string Address { get; set; }

        /// <summary>
        /// The maximum number of concurrent threads that will handle received messages.
        /// </summary>
        int MaxConcurrency { get; set; }

        /// <summary>
        /// The maximum number of times a received message will be retried by
        /// this process before declaring the message "poison".
        /// </summary>
        int MaxRetries { get; set; }

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
        Action<MessageContext> GetCompiledMessageHandlerPipeline();

        /// <summary>
        /// Returns an instance of the configured transport receiver.
        /// </summary>
        ITransportReceiver CreateTransportReceiver();
    }
}