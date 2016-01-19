using System;
using System.Collections.Generic;

namespace LightRail.Client
{
    public interface ITransport
    {
        /// <summary>
        /// Starts message receiver threads.
        /// </summary>
        void Start();
        /// <summary>
        /// Stops message receiver threads and drains any currently executing messages up until the timeSpan elapses
        /// </summary>
        void Stop(TimeSpan timeSpan);

        /// <summary>
        /// Sends the specified message to the set of addresses
        /// </summary>
        void Send(OutgoingTransportMessage transportMessage, IEnumerable<string> addresses);

        /// <summary>
        /// Called when a message is read and available
        /// </summary>
        event EventHandler<MessageAvailableEventArgs> MessageAvailable;
        /// <summary>
        /// Called when a poison message is detected (too many failures).
        /// </summary>
        event EventHandler<PoisonMessageDetectedEventArgs> PoisonMessageDetected;
    }
}
