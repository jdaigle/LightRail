using System.Collections.Generic;

namespace LightRail.Client
{
    public class OutgoingTransportMessage
    {
        public OutgoingTransportMessage(IDictionary<string, string> headers, object message, byte[] serializedMessageBuffer)
        {
            Headers = headers;
            Message = message;
            SerializedMessageBuffer = serializedMessageBuffer;
        }

        public IDictionary<string, string> Headers { get; }
        public object Message { get; }
        public byte[] SerializedMessageBuffer { get; }
    }
}
