using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightRail.Amqp.Framing
{
    public sealed class AmqpFrameIndexAttribute : Attribute
    {
        public AmqpFrameIndexAttribute(int index)
        {
            Index = index;
        }

        public int Index { get; }
    }
}
