using System;

namespace LightRail.Amqp.Types
{
    public sealed class AmqpDescribedListIndexAttribute : Attribute
    {
        public AmqpDescribedListIndexAttribute(int index)
        {
            Index = index;
        }

        public int Index { get; }
    }
}
