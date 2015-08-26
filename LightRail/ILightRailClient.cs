using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LightRail
{
    public interface ILightRailClient
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
        /// Sends a message back to the destination where the current message originated.
        /// </summary>
        void Reply(object message);
        /// <summary>
        /// constructs a new message and sends back to the destination where the current message originated.
        /// </summary>
        void Reply<T>(Action<T> messageConstructor);
    }
}
