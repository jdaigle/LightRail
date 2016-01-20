using System;
using System.Linq;
using LightRail.Client.Dispatch;
using LightRail.Client.Pipeline;
using LightRail.Client.Transport;

namespace LightRail.Client.Config
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
        public int MaxRetries { get; set; } = 3;

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
