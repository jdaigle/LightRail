using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightRail.Amqp.Messaging
{
    public class ReceivedDelivery
    {
        public ReceivedDeliveryStateEnum DeliveryState { get; private set; }
    }
}
