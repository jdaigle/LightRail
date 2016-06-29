using System;
using System.Linq;
using LightRail.ServiceBus.Dispatch;
using LightRail.ServiceBus.Logging;

namespace LightRail.ServiceBus.Pipeline
{
    public class MessageHandlerDispatchBehavior : PipelinedBehavior
    {
        private static readonly ILogger logger = LogManager.GetLogger("LightRail.ServiceBus.MessageHandlerDispatchBehavior");

        protected override void Invoke(MessageContext context, Action next)
        {
            // GetDispatchersForMessageType simply returns a enumerable of all
            // handlers which can accept the message as a parameter in which ever order they exist internally
            var dispatchers = context.ServiceLocator
                .Resolve<MessageHandlerCollection>()
                .GetDispatchersForMessageType(context.MessageType);
            if (!dispatchers.Any())
            {
                logger.Warn("No Mapped Message Handlers For Message {0}", context.MessageType);
            }
            foreach (var dispatcher in dispatchers)
            {
                dispatcher.Execute(context.ServiceLocator.Resolve(dispatcher.MessageHandlerType), context.CurrentMessage);
            }
            next(); // it's best practice to call next, even though this is likely the most inner behavior to execute
        }
    }
}
