using LightRail.Amqp.Types;

namespace LightRail.Amqp.Messaging
{
    public static class MessagingDescriptors
    {
        // message
        public static readonly Descriptor Header = new Descriptor(0x0000000000000070, "amqp:header:list");
        public static readonly Descriptor DeliveryAnnotations = new Descriptor(0x0000000000000071, "amqp:delivery-annotations:map");
        public static readonly Descriptor MessageAnnotations = new Descriptor(0x0000000000000072, "amqp:message-annotations:map");
        public static readonly Descriptor Properties = new Descriptor(0x0000000000000073, "amqp:properties:list");
        public static readonly Descriptor ApplicationProperties = new Descriptor(0x0000000000000074, "amqp:application-properties:map");
        public static readonly Descriptor Data = new Descriptor(0x0000000000000075, "amqp:data:binary");
        public static readonly Descriptor AmqpSequence = new Descriptor(0x0000000000000076, "amqp:amqp-sequence:list");
        public static readonly Descriptor AmqpValue = new Descriptor(0x0000000000000077, "amqp:amqp-value:*");
        public static readonly Descriptor Footer = new Descriptor(0x0000000000000078, "amqp:footer:map");
    }
}
