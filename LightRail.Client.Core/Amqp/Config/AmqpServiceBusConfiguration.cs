using Amqp;
using LightRail.Client.Config;
using LightRail.Client.Transport;

namespace LightRail.Client.Amqp.Config
{
    public class AmqpServiceBusConfiguration : BaseServiceBusConfig
    {
        public Address AmqpAddress { get; set; }

        public override ITransportSender CreateTransportSender()
        {
            return new AmqpTransportSender(this);
        }
    }
}
