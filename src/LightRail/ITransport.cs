using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LightRail
{
    public interface ITransport
    {
        void Start();
        void Stop();

        void Send(OutgoingTransportMessage transportMessage, IEnumerable<string> destinations);
        string RequestTimeoutMessage(int secondsToWait, OutgoingTransportMessage transportMessage);
        void ClearTimeout(string timeoutCorrelationID);

        event EventHandler<MessageAvailable> MessageAvailable;
        event EventHandler<PoisonMessageDetectedEventArgs> PoisonMessageDetected;

        string OriginatingAddress { get; }

        /// <summary>
        /// Peeks at the underyling receive queue and estimates the number of queued messages
        /// </summary>
        int PeekCount();
    }
}
