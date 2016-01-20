using LightRail.Client.Config;
using LightRail.Client.Transport;

namespace LightRail.Client.InMemoryQueue.Config
{
    public class InMemoryQueueServiceBusConfiguration : BaseServiceBusConfig
    {
        public override ITransportSender CreateTransportSender()
        {
            return new InMemoryQueueTransportSender(this);
        }
    }
}
