using System.Collections.Generic;

namespace LightRail.ServiceBus.Transport
{
    public interface ITransportSender
    {
        /// <summary>
        /// Sends the specified message to the set of addresses
        /// </summary>
        void Send(OutgoingTransportMessage transportMessage, IEnumerable<string> addresses);
    }
}
