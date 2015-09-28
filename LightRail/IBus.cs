using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LightRail
{
    public interface IBus
    {
        /// <summary>
        /// Sends a message to the configured destination
        /// </summary>
        void Send(object message);
        /// <summary>
        /// Sends a message to a specific destination
        /// </summary>
        void Send(object message, string destination);
        /// <summary>
        /// Constructs a new message (can be an Interface) and sends to the configured destination
        /// </summary>
        void Send<T>(Action<T> messageConstructor);
        /// <summary>
        /// Constructs a new message (can be an Interface) and sends to a specific destination
        /// </summary>
        void Send<T>(Action<T> messageConstructor, string destination);

        /// <summary>
        /// Publishes a message to the current list of subscribers
        /// </summary>
        void Publish(object message);
        /// <summary>
        /// Constructs a new message (can be an Interface) and publishes to the current list of subscribers
        /// </summary>
        void Publish<T>(Action<T> messageConstructor);

        /// <summary>
        /// Gets the list of key/value pairs that will be in the header of
        /// messages being sent by the same thread.
        /// 
        /// This value will be cleared when a thread receives a message.
        /// </summary>
        IDictionary<string, string> OutgoingHeaders { get; }

        /// <summary>
        /// Gets the message context containing the Id, return address, and headers
        /// of the message currently being handled on this thread.
        /// 
        /// This may be null if a message is not currently being handled on this thread.
        /// </summary>
        MessageContext CurrentMessageContext { get; }

        /// <summary>
        /// Requests a timeout message to be returned to the current endpoint after the
        /// specified number of seconds. The message will contain a correlation ID
        /// that matches the one returned here.
        /// </summary>
        string RequestTimeoutMessage(int secondsToWait, object timeoutMessage);

        /// <summary>
        /// Clears the specified timeout, preventing it from firing.
        /// </summary>
        void ClearTimeout(string timeoutCorrelationID);

        /// <summary>
        /// Returns a reference the underyling transport
        /// </summary>
        ITransport Transport { get; }
    }
}
