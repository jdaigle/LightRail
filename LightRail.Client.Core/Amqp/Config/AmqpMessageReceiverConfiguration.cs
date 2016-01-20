using LightRail.Client.Config;
using LightRail.Client.Transport;

namespace LightRail.Client.Amqp.Config
{
    public class AmqpMessageReceiverConfiguration : BaseMessageReceiverConfiguration
    {
        public override ITransportReceiver CreateTransportReceiver()
        {
            return new AmqpTransportReceiver((AmqpHost)this.ServiceBusConfig.Host, this, (AmqpServiceBusConfiguration)this.ServiceBusConfig);
        }
    }
}
