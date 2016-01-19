using System;

namespace LightRail.Client
{
    public class MessageAvailableEventArgs : EventArgs
    {
        public IncomingTransportMessage TransportMessage { get; }

        public MessageAvailableEventArgs(IncomingTransportMessage transportMessage)
        {
            this.TransportMessage = transportMessage;
        }
    }
}
