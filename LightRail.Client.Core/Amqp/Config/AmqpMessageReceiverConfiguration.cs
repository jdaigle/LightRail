using Amqp;
using LightRail.Client.Config;
using LightRail.Client.Transport;

namespace LightRail.Client.Amqp.Config
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
