using System;
using LightRail.Amqp.Types;

namespace LightRail.Amqp.Messaging
{
    /// <remarks>
    /// The application-properties section is a part of the bare message used for structured application data. Intermediaries
    /// can use the data within this structure for the purposes of filtering or routing.
    /// 
    /// The keys of this map are restricted to be of type string (which excludes the possibility of a null key) and the
    /// values are restricted to be of simple types only, that is, excluding map, list, and array types.
    /// </remarks>
    public sealed class ApplicationProperties : DescribedList
    {
        public ApplicationProperties()
            : base(Descriptor.ApplicationProperties)
        {
        }

        protected override int CalculateListSize()
        {
            throw new NotImplementedException();
        }

        protected override void EncodeListItem(ByteBuffer buffer, int index, bool arrayEncoding)
        {
            throw new NotImplementedException();
        }

        protected override void DecodeListItem(ByteBuffer buffer, int index)
        {
            throw new NotImplementedException();
        }
    }
}