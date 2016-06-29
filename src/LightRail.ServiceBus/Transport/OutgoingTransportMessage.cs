using System;
using System.Collections.Generic;

namespace LightRail.ServiceBus.Transport
{
    public class OutgoingTransportMessage
    {
        public OutgoingTransportMessage(IDictionary<string, string> headers, object message, Type messageType)
        {
            Headers = headers;
            Message = message;
            MessageType = messageType;
        }

        public IDictionary<string, string> Headers { get; }
        public object Message { get; }
        public Type MessageType { get; }
    }
}
