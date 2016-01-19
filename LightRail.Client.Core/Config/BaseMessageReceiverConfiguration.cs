using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LightRail.Client.Dispatch;
using LightRail.Client.Pipeline;

namespace LightRail.Client.Config
{
    public class BaseMessageReceiverConfiguration : IMessageReceiverConfiguration
    {
        protected BaseMessageReceiverConfiguration()
        {
            MessageHandlers = new MessageHandlerCollection();
        }

        public IServiceBusConfig ServiceBusConfig { get; set; }
        public MessageHandlerCollection MessageHandlers { get; }

        public MessageHandlerCollection GetCombinedMessageHandlers()
        {
            foreach (var item in ServiceBusConfig.MessageHandlers)
            {
                MessageHandlers.AddMessageHandler(item);
            }
            return MessageHandlers;
        }

        public Func<MessageContext, Task> GetCompiledMessageHandlerPipeline()
        {
            var messageHandlerPipelinedBehaviors = ServiceBusConfig.PipelinedBehaviors.ToList(); // copy the list
            // ensure MessageHandlerDispatchBehavior is added to the end
            messageHandlerPipelinedBehaviors.RemoveAll(x => x is MessageHandlerDispatchBehavior);
            messageHandlerPipelinedBehaviors.Add(new MessageHandlerDispatchBehavior());
            return PipelinedBehavior.CompileMessageHandlerPipeline(messageHandlerPipelinedBehaviors);
        }
    }
}
