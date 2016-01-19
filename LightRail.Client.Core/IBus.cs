using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightRail.Client
{
    public interface IBus
    {
        /// <summary>
        /// Publishes a message to the current list of subscribers
        /// </summary>
        void Publish<T>(T message);

        /// <summary>
        /// Gets the message context containing the Id, return address, and headers
        /// of the message currently being handled on this thread.
        /// 
        /// This may be null if a message is not currently being handled on this thread.
        /// </summary>
        MessageContext CurrentMessageContext { get; }
    }
}
