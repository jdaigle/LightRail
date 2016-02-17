using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LightRail
{
    public class IncomingTransportMessage
    {
        public IncomingTransportMessage(string messageId, Dictionary<string, string> headers, string serializedMessagedata)
        {
            MessageId = messageId;
            Headers = headers;
            Headers[StandardHeaders.MessageId] = messageId;
            SerializedMessageData = serializedMessagedata;
        }

        public string MessageId { get; private set; }
        public Dictionary<string, string> Headers { get; private set; }
        public string SerializedMessageData { get; private set; }
    }
}
