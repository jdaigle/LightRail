using System.Collections.Generic;

namespace LightRail.Client
{
    public class IncomingTransportMessage
    {
        public IncomingTransportMessage(string messageId, Dictionary<string, string> headers, byte[] serializedMessageBuffer)
        {
            MessageId = messageId;
            Headers = headers;
            //Headers[StandardHeaders.MessageId] = messageId;
            SerializedMessageBuffer = serializedMessageBuffer;
        }

        public string MessageId { get; }
        public Dictionary<string, string> Headers { get; }
        public byte[] SerializedMessageBuffer { get; }
    }
}
