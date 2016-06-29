using System;
using System.Collections.Generic;

namespace LightRail.ServiceBus.Transport
{
    public class IncomingTransportMessage
    {
        public IncomingTransportMessage(string messageId, Dictionary<string, string> headers, Type messageType, object decodedMessage)
        {
            MessageId = messageId;
            Headers = headers;
            //Headers[StandardHeaders.MessageId] = messageId;
            MessageType = messageType;
            DecodedMessage = decodedMessage;
        }

        public string MessageId { get; }
        public Dictionary<string, string> Headers { get; }
        public Type MessageType { get; }
        public object DecodedMessage { get; }
    }
}
