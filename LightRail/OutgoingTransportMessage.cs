using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightRail
{
    public class OutgoingTransportMessage
    {
        public OutgoingTransportMessage(IDictionary<string, string> headers, string serializedMessagedata)
        {
            Headers = headers;
            SerializedMessageData = serializedMessagedata;
        }

        public IDictionary<string, string> Headers { get; private set; }
        public string SerializedMessageData { get; private set; }
    }
}
