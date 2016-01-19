using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightRail.Client.Config;

namespace LightRail.Client.InMemory
{
    public class InMemoryConfiguration : IServiceBusConfig
    {
        public void ReceiveFrom(object host, string address, Action<IQueueReceiverConfiguration> cfg)
        {
            throw new NotImplementedException();
        }
    }
}
