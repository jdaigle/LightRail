using System;

namespace LightRail.Client
{
    public interface IBusControl : IBus
    {
        /// <summary>
        /// Starts message receiver threads.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops message receiver threads while waiting for existing dispatched messages to finish executing.
        /// </summary>
        void Stop();

        /// <summary>
        /// Stops message receiver threads while waiting for existing dispatched messages to finish executing up until
        /// the specified timeSpan.
        /// </summary>
        void Stop(TimeSpan timeSpan);
    }
}
