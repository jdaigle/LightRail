using System;

namespace LightRail.Client
{
    public class MessageProcessedEventArgs : EventArgs
    {
        public MessageContext MessageContext { get; }
        public DateTime StartProcessingTimestamp { get; }
        public DateTime EndProcessingTimestamp { get; }
        public double ProcessingDuration { get; }

        public MessageProcessedEventArgs(MessageContext currentMessageContext, DateTime startTimestamp, DateTime endTimestamp, double duration)
        {
            this.MessageContext = currentMessageContext;
            this.StartProcessingTimestamp = startTimestamp;
            this.EndProcessingTimestamp = endTimestamp;
            this.ProcessingDuration = duration;
        }
    }
}
