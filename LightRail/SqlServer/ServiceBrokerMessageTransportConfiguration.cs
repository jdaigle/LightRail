using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightRail.SqlServer
{
    public class ServiceBrokerMessageTransportConfiguration : AbstractTransportConfiguration
    {
        public ServiceBrokerMessageTransportConfiguration()
        {
            ServiceBrokerMessageType = "LightRailTransportMessageType";
            ServiceBrokerContract = "LightRailTransportContract";
        }

        public string ServiceBrokerMessageType { get; set; }
        public string ServiceBrokerContract { get; set; }
        public string ServiceBrokerQueue { get; set; }
        public string ServiceBrokerService { get; set; }
        public string ServiceBrokerConnectionString { get; set; }
    }
}
