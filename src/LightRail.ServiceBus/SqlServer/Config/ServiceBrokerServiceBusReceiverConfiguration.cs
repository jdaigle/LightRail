using LightRail.ServiceBus.Config;
using LightRail.ServiceBus.Transport;

namespace LightRail.ServiceBus.SqlServer.Config
{
    public class ServiceBrokerServiceBusReceiverConfiguration : BaseMessageReceiverConfiguration
    {
        public string ServiceBrokerService { get; set; }
        public string ServiceBrokerQueue { get; set; }

        public override ITransportReceiver CreateTransportReceiver()
        {
            return new ServiceBrokerTransportReceiver(this, (ServiceBrokerServiceBusConfiguration)this.ServiceBusConfig);
        }
    }
}
