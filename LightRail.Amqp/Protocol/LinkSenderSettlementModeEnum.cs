using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightRail.Amqp.Protocol
{
    public enum LinkSenderSettlementModeEnum : byte
    {
        /// <summary>
        /// The sender will send all deliveries initially unsettled to the receiver.
        /// </summary>
        Unsettled = 0,
        /// <summary>
        /// The sender will send all deliveries settled to the receiver.
        /// </summary>
        Settled = 1,
        /// <summary>
        /// The sender MAY send a mixture of settled and unsettled deliveries to the receiver.
        /// </summary>
        Mixed = 2,
    }
}
