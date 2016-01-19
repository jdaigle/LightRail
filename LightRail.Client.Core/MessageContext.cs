using System.Collections.Generic;

namespace LightRail.Client
{
    public class MessageContext
    {
        public IBus Bus { get; }
        public string MessageID { get; }
        public IServiceLocator ServiceLocator { get; }
        public object CurrentMessage { get; internal set; }
        public string SerializedMessageData { get; internal set; }

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

        public MessageContext(IBus bus, string messageID, Dictionary<string, string> headers, IServiceLocator serviceLocator)
        {
            this.Bus = bus;
            this.MessageID = messageID;
            this.headers = headers;
            this.ServiceLocator = serviceLocator;
        }
    }
}
