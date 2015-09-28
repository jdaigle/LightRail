using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightRail
{
    /// <remarks>
    /// ALL events are observed on a separate thread from the one handling the current message.
    /// </remarks>
    public interface IServiceBusEvents
    {
        event EventHandler<MessageProcessedEventArgs> MessageProcessed;
        event EventHandler<PoisonMessageDetectedEventArgs> PoisonMessageDetected;
    }
}
