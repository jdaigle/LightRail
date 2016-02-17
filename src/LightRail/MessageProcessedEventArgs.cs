using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LightRail
{
    public class MessageProcessedEventArgs : EventArgs
    {
        public MessageContext MessageContext { get; private set; }
        public DateTime StartProcessingTimestamp { get; private set; }
        public DateTime EndProcessingTimestamp { get; private set; }
        public double ProcessingDuration { get; private set; }

        public MessageProcessedEventArgs(MessageContext currentMessageContext, DateTime startTimestamp, DateTime endTimestamp, double p)
        {
            this.MessageContext = currentMessageContext;
            this.StartProcessingTimestamp = startTimestamp;
            this.EndProcessingTimestamp = endTimestamp;
            this.ProcessingDuration = p;
        }
    }
}
