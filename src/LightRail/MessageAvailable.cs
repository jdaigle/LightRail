using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LightRail
{
    public class MessageAvailable : EventArgs
    {
        public IncomingTransportMessage TransportMessage { get; private set; }

        public MessageAvailable(IncomingTransportMessage transportMessage)
        {
            this.TransportMessage = transportMessage;
        }
    }
}
