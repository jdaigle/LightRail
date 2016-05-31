using System;
using System.Linq;
using System.Reflection;
using LightRail.ServiceBus.Dispatch;
using LightRail.ServiceBus.Pipeline;
using LightRail.ServiceBus.Transport;

namespace LightRail.ServiceBus.Config
{
    public abstract class BaseMessageReceiverConfiguration
    {
        public BaseServiceBusConfig ServiceBusConfig { get; internal set; }

        /// <summary>
        /// A collection of message handlers specific to this message receiver.
        /// </summary>
        public MessageHandlerCollection MessageHandlers { get; } = new MessageHandlerCollection();

        /// <summary>
        /// The maximum number of concurrent threads that will handle received messages.
        /// Default value is "1".
        /// </summary>
        public int MaxConcurrency { get; set; } = 1;
        /// <summary>
        /// The maximum number of times a received message will be retried by
        /// this process before declaring the message "poison".
        /// Default value is "5".
        /// </summary>
        public int MaxRetries { get; set; } = 5;

        public void ScanForHandlersFromAssembly(Assembly assembly)
        {
            MessageHandlers.ScanAssemblyAndMapMessageHandlers(assembly);
        }

        public void ScanForMessageHandlersFromCurrentAssembly()
        {
            MessageHandlers.ScanAssemblyAndMapMessageHandlers(Assembly.GetCallingAssembly());
        }

        public void Handle<TMessage>(Action<TMessage> messageHandler)
        {
            MessageHandlers.AddMessageHandler(MessageHandlerMethodDispatcher.FromDelegate(messageHandler));
        }

        public void Handle<TMessage>(Action<TMessage, MessageContext> messageHandler)
        {
            MessageHandlers.AddMessageHandler(MessageHandlerMethodDispatcher.FromDelegate(messageHandler));
        }

        /// <summary>
        /// Returns a combined set of message handlers including shared message handlers.
        /// </summary>
        public MessageHandlerCollection GetCombinedMessageHandlers()
        {
            foreach (var item in ServiceBusConfig.MessageHandlers)
            {
                MessageHandlers.AddMessageHandler(item);
            }
            return MessageHandlers;
        }

        /// <summary>
        /// Returns a compiled message pipeline.
        /// </summary>
        public Action<MessageContext> GetCompiledMessageHandlerPipeline()
        {
            var messageHandlerPipelinedBehaviors = ServiceBusConfig.PipelinedBehaviors.ToList(); // copy the list
            messageHandlerPipelinedBehaviors.RemoveAll(x => x is MessageHandlerDispatchBehavior);
            messageHandlerPipelinedBehaviors.Add(new MessageHandlerDispatchBehavior());
            return PipelinedBehavior.CompileMessageHandlerPipeline(messageHandlerPipelinedBehaviors);
        }

        /// <summary>
        /// Returns an instance of the configured transport receiver.
        /// </summary>
        public abstract ITransportReceiver CreateTransportReceiver();
    }
}
