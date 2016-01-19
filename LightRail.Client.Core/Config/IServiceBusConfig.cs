using System;

namespace LightRail.Client.Config
{
    public interface IServiceBusConfig
    {
        void ReceiveFrom(object host, string address, Action<IQueueReceiverConfiguration> cfg);
    }
}