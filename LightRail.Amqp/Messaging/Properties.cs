using System;
using LightRail.Amqp.Types;

namespace LightRail.Amqp.Messaging
{
    public class Properties : DescribedList
    {
        public Properties()
            : base(MessagingDescriptors.Properties)
        {
        }
    }
}