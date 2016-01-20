using LightRail.Client.Config;
using LightRail.Client.Transport;

namespace LightRail.Client.Amqp.Config
{
    public class AmqpServiceBusConfiguration : BaseServiceBusConfig
    {
        public override ITransportSender CreateTransportSender()
        {
            return new AmqpTransportSender((AmqpHost)this.Host, this);
        }
    }
}
