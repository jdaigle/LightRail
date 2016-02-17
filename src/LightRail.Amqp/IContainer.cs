using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightRail.Amqp.Framing;
using LightRail.Amqp.Messaging;
using LightRail.Amqp.Protocol;

namespace LightRail.Amqp
{
    public interface IContainer
    {
        string ContainerId { get; }

        /// <summary>
        /// Called whenever a link is successfully attached to a session.
        /// </summary>
        void OnLinkAttached(AmqpLink link);

        /// <summary>
        /// Returns whether or not the link can be attached to this container.
        /// </summary>
        bool CanAttachLink(AmqpLink newLink, Attach attach);

        /// <summary>
        /// Called whenever a link receives a Transfer with a Message.
        /// </summary>
        /// <param name="link">The link on which the transfer was received.</param>
        /// <param name="delivery">The received delivery.</param>
        void OnDelivery(AmqpLink link, Delivery delivery);
    }
}
