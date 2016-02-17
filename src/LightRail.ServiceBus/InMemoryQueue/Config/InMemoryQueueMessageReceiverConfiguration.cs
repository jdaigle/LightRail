using LightRail.ServiceBus.Config;
using LightRail.ServiceBus.Transport;

namespace LightRail.ServiceBus.InMemoryQueue.Config
{
    public class InMemoryQueueMessageReceiverConfiguration : BaseMessageReceiverConfiguration
    {
        /// <summary>
        /// The address from which the message receiver will receiver messages.
        /// </summary>
        public string Address { get; set; }

        public override ITransportReceiver CreateTransportReceiver()
        {
            return new InMemoryQueueTransportReceiver(this, this.ServiceBusConfig);
        }
    }
}
