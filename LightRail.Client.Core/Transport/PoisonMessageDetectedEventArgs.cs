using System;

namespace LightRail.Client.Transport
{
    public class PoisonMessageDetectedEventArgs : EventArgs
    {
        public string MessageId { get; set; }
        
        public string OriginServiceName { get; set; }
        public string ServiceName { get; set; }
        public string QueueName { get; set; }

        public byte[] MessageBody { get; set; }

        public int Retries { get; set; }
        public string ErrorCode { get; set; }
        public Exception Exception { get; set; }
    }
}
