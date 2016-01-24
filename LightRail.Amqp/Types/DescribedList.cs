using System;

namespace LightRail.Amqp.Types
{
    /// <summary>
    /// AMQP composite types are represented as a described list.
    /// </summary>
    /// <remarks>
    /// AMQP composite types are represented as a described list. Each element in the list is positionally correlated with
    /// the fields listed in the composite type definition. The permitted element values are determined by the type speci-
    /// fication and multiplicity of the corresponding field definitions. When the trailing elements of the list representation
    /// are null, they MAY be omitted. The descriptor of the list indicates the specific composite type being represented.
    /// </remarks>
    public abstract class DescribedList : DescribedType
    {
        protected DescribedList(Descriptor descriptor)
            : base(descriptor)
        {
        }

        protected abstract int CalculateListSize();
        protected abstract void EncodeListItem(ByteBuffer buffer, int index, bool arrayEncoding);
        protected abstract void DecodeListItem(ByteBuffer buffer, int index);

        protected override void EncodeValue(ByteBuffer buffer)
        {
            Encoder.WriteList(buffer, CalculateListSize(), EncodeListItem, true);
        }

        protected override void DecodeValue(ByteBuffer buffer)
        {
            throw new NotImplementedException();
        }
    }
}
