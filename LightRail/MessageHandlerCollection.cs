using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LightRail
{
    public class MessageHandlerCollection
    {
        public void Register<TMessage>(Action<TMessage, MessageContext> messageHandler)
        {
            var dispatchInfo = new GenericDispatchInfo<TMessage, MessageContext>(messageHandler);
            messageHandlers.Add(dispatchInfo);
        }

        private List<DispatchInfo> messageHandlers = new List<DispatchInfo>();

        public IEnumerable<DispatchInfo> GetOrderedDispatchInfoFor(params Type[] parameters)
        {
            return messageHandlers.Where(x => x.IsMatchByParameterType(parameters));
        }
    }
}
