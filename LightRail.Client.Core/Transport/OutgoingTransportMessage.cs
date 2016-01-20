using System.Collections.Generic;

namespace LightRail.Client.Transport
{
    public class OutgoingTransportMessage
    {
        public OutgoingTransportMessage(IDictionary<string, string> headers, object message)
        {
            Headers = headers;
            Message = message;
        }

        public IDictionary<string, string> Headers { get; }
        public object Message { get; }
    }
}
