using Amqp;
using LightRail.ServiceBus.Config;
using LightRail.ServiceBus.Transport;

namespace LightRail.ServiceBus.Amqp.Config
{
    public class AmqpMessageReceiverConfiguration : BaseMessageReceiverConfiguration
    {
        /// <summary>
        /// The address from which the message receiver will receiver messages.
        /// </summary>
        public string ReceiverLinkAddress { get; set; }

        public override ITransportReceiver CreateTransportReceiver()
        {
            return new AmqpTransportReceiver(this, (AmqpServiceBusConfiguration)this.ServiceBusConfig);
        }
    }
}
