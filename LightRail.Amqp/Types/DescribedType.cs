using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightRail.Amqp.Types
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// A descriptor forms an association between a custom type, and an AMQP type.
    /// 
    /// This association indicates that the AMQP type is actually a representation
    /// of the custom type.The resulting combination of the AMQP type and its
    /// descriptor is referred to as a described type.
    /// </remarks>
    public abstract class DescribedType
    {
        public Descriptor Descriptor { get; }

        protected DescribedType(Descriptor descriptor)
        {
            Descriptor = descriptor;
        }

        public void Encode(ByteBuffer buffer)
        {
            AmqpBitConverter.WriteUByte(buffer, FormatCode.Described);
            if (!string.IsNullOrWhiteSpace(Descriptor.Name) || Descriptor.Code != 0)
            {
                Encoder.WriteULong(buffer, Descriptor.Code, true);
            }
            else
            {
                Encoder.WriteSymbol(buffer, Descriptor.Name, true);
            }
            EncodeValue(buffer);
        }

        protected abstract void EncodeValue(ByteBuffer buffer);
        protected abstract void DecodeValue(ByteBuffer buffer);
    }
}
