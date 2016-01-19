using System;

namespace LightRail.Client
{
    public interface IBus
        : IMessageCreator
    {
        /// <summary>
        /// Sends a message to the configured destination
        /// </summary>
        void Send<T>(T message);
        /// <summary>
        /// Sends a message to a specific address
        /// </summary>
        void Send<T>(T message, string address);

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
