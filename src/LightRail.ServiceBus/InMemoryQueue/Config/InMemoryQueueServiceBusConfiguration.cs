using LightRail.ServiceBus.Config;
using LightRail.ServiceBus.Transport;

namespace LightRail.ServiceBus.InMemoryQueue.Config
{
    public class InMemoryQueueServiceBusConfiguration : BaseServiceBusConfig
    {
        public override ITransportSender CreateTransportSender()
        {
            return new InMemoryQueueTransportSender(this);
        }
    }
}
