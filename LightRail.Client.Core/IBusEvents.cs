using System;
using LightRail.Client.Transport;

namespace LightRail.Client
{
    public interface IBusEvents
    {
        /// <summary>
        /// Called whenever a message is successfully processed.
        /// </summary>
        /// <remarks>
        /// The event handlers are called asnychronously on a background thread.
        /// </remarks>
        event EventHandler<MessageProcessedEventArgs> MessageProcessed;
        /// <summary>
        /// Called whenever a poison message is detected by the infrastructure.
        /// </summary>
        /// <remarks>
        /// The event handlers are called asnychronously on a background thread.
        /// </remarks>
        event EventHandler<PoisonMessageDetectedEventArgs> PoisonMessageDetected;
    }
}
