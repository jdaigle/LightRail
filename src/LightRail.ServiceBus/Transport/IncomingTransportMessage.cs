using System.Collections.Generic;

namespace LightRail.ServiceBus.Transport
{
    public class IncomingTransportMessage
    {
        public IncomingTransportMessage(string messageId, Dictionary<string, string> headers, object decodedMessage)
        {
            MessageId = messageId;
            Headers = headers;
            //Headers[StandardHeaders.MessageId] = messageId;
            DecodedMessage = decodedMessage;
        }

        public string MessageId { get; }
        public Dictionary<string, string> Headers { get; }
        public object DecodedMessage { get; }
    }
}
