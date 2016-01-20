using Amqp;
using LightRail.Client.Config;
using LightRail.Client.Transport;

namespace LightRail.Client.Amqp.Config
{
    public class AmqpServiceBusConfiguration : BaseServiceBusConfig
    {
        public AmqpServiceBusConfiguration()
        {
            MessageEncoder = new JsonMessageEncoder();
        }

        /// <summary>
        /// The host address of the AMQP container to connect to.
        /// </summary>
        public Address AmqpAddress { get; set; }

        /// <summary>
        /// Encodes/Decodes messages sent & received in AMQP transfers. Defaults to LightRail.Client.JsonMessageEncoder
        /// </summary>
        public IMessageEncoder MessageEncoder { get; set; }

        public override ITransportSender CreateTransportSender()
        {
            return new AmqpTransportSender(this);
        }
    }
}
