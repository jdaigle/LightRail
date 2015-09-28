using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LightRail
{
    public class OutgoingTransportMessage
    {
        public OutgoingTransportMessage(IDictionary<string, string> headers, object message, string serializedMessagedata)
        {
            Headers = headers;
            Message = message;
            SerializedMessageData = serializedMessagedata;
        }

        public IDictionary<string, string> Headers { get; private set; }
        public object Message { get; private set; }
        public string SerializedMessageData { get; private set; }
    }
}
