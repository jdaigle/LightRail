using System;
using System.Linq;
using System.Reflection;
using LightRail.ServiceBus.Dispatch;
using LightRail.ServiceBus.Pipeline;
using LightRail.ServiceBus.Transport;

namespace LightRail.ServiceBus.Config
{
    public abstract class BaseMessageReceiverConfiguration : IMessageReceiverConfiguration
    {
        protected BaseMessageReceiverConfiguration()
        {
            MessageHandlers = new MessageHandlerCollection();
        }

        public IServiceBusConfig ServiceBusConfig { get; set; }
        public MessageHandlerCollection MessageHandlers { get; }

        public int MaxConcurrency { get; set; } = 1;
        public int MaxRetries { get; set; } = 5;

        public void ScanForHandlersFromAssembly(Assembly assembly)
        {
            MessageHandlers.ScanAssemblyAndMapMessageHandlers(assembly);
        }

        public void ScanForHandlersFromCurrentAssembly()
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

        public MessageHandlerCollection GetCombinedMessageHandlers()
        {
            foreach (var item in ServiceBusConfig.MessageHandlers)
            {
                MessageHandlers.AddMessageHandler(item);
            }
            return MessageHandlers;
        }

        public Action<MessageContext> GetCompiledMessageHandlerPipeline()
        {
            var messageHandlerPipelinedBehaviors = ServiceBusConfig.PipelinedBehaviors.ToList(); // copy the list
            // ensure MessageHandlerDispatchBehavior is added to the end
            messageHandlerPipelinedBehaviors.RemoveAll(x => x is MessageHandlerDispatchBehavior);
            messageHandlerPipelinedBehaviors.Add(new MessageHandlerDispatchBehavior());
            return PipelinedBehavior.CompileMessageHandlerPipeline(messageHandlerPipelinedBehaviors);
        }

        public abstract ITransportReceiver CreateTransportReceiver();
    }
}
