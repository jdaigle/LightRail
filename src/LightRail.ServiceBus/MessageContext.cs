using System;
using System.Collections.Generic;

namespace LightRail.ServiceBus
{
    public class MessageContext
    {
        public IBus Bus { get; }
        public string MessageID { get; }
        public IServiceLocator ServiceLocator { get; }
        public Type MessageType { get; }
        public object CurrentMessage { get; }

        private readonly IReadOnlyDictionary<string, string> _headers;

        public IReadOnlyDictionary<string, string> Headers
        {
            get
            {
                return _headers;
            }
        }

        /// <summary>
        /// Returns the current message header value for the given key.
        /// </summary>
        public string this[string key]
        {
            get
            {
                return _headers[key];
            }
        }

        public MessageContext(IBus bus, string messageID, Dictionary<string, string> headers, Type messageType, object currentMessage, IServiceLocator serviceLocator)
        {
            this.Bus = bus;
            this.MessageID = messageID;
            this._headers = headers;
            this.MessageType = messageType;
            this.CurrentMessage = currentMessage;
            this.ServiceLocator = serviceLocator;
        }
    }
}
