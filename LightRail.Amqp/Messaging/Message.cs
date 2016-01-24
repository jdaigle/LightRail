using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightRail.Amqp.Types;

namespace LightRail.Amqp.Messaging
{
    public class Message
    {
        /// <summary>
        /// The header section carries standard delivery details about the
        /// transfer of a message through the AMQP network.
        /// </summary>
        public Header Header;

        /// <summary>
        /// The delivery-annotations section is used for delivery-specific
        /// non-standard properties at the head of the message.
        /// </summary>
        public DeliveryAnnotations DeliveryAnnotations;

        /// <summary>
        /// The message-annotations section is used for properties of the
        /// message which are aimed at the infrastructure and SHOULD be
        /// propagated across every delivery step.
        /// </summary>
        public MessageAnnotations MessageAnnotations;

        /// <summary>
        /// Immutable properties of the message. The properties section is
        /// used for a defined set of standard properties of the message.
        /// </summary>
        public Properties Properties;

        /// <summary>
        /// The application-properties section is a part of the bare message
        /// used for structured application data.
        /// </summary>
        public ApplicationProperties ApplicationProperties;

        /// <summary>
        /// A data section contains opaque binary data.
        /// </summary>
        public DescribedType BodySection;

        /// <summary>
        /// Transport footers for a message.
        /// </summary>
        public Footer Footer;
        
        public void Encode(ByteBuffer buffer)
        {
            EncodeIfNotNull(this.Header, buffer);
            EncodeIfNotNull(this.DeliveryAnnotations, buffer);
            EncodeIfNotNull(this.MessageAnnotations, buffer);
            EncodeIfNotNull(this.Properties, buffer);
            EncodeIfNotNull(this.ApplicationProperties, buffer);
            EncodeIfNotNull(this.BodySection, buffer);
            EncodeIfNotNull(this.Footer, buffer);
        }

        static void EncodeIfNotNull(DescribedType section, ByteBuffer buffer)
        {
            if (section != null)
            {
                section.Encode(buffer);
            }
        }
    }
}
