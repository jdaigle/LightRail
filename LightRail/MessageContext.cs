using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LightRail
{
    public class MessageContext
    {
        public ILightRailClient Client { get; private set; }
        public string MessageID { get; private set; }

        private readonly IReadOnlyDictionary<string, string> headers;

        public IReadOnlyDictionary<string, string> Headers
        {
            get
            {
                return headers;
            }
        }

        /// <summary>
        /// Returns the current message header value for the given key.
        /// </summary>
        public string this[string key]
        {
            get
            {
                return headers[key];
            }
        }

        public MessageContext(ILightRailClient lightRailClient, string messageID, Dictionary<string, string> headers)
        {
            this.Client = lightRailClient;
            this.MessageID = messageID;
            this.headers = headers;
        }
    }
}
