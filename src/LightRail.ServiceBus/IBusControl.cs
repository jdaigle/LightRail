﻿using System;

namespace LightRail.ServiceBus
{
    public interface IBusControl : IBus
    {
        /// <summary>
        /// Starts message receiver threads.
        /// </summary>
        IBus Start();

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
