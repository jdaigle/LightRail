using System;

namespace LightRail.Client.Transport
{
    public interface ITransportReceiver
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
        /// Called when a message is read and available
        /// </summary>
        event EventHandler<MessageAvailableEventArgs> MessageAvailable;
        /// <summary>
        /// Called when a poison message is detected (too many failures).
        /// </summary>
        event EventHandler<PoisonMessageDetectedEventArgs> PoisonMessageDetected;
    }
}
