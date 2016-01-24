using System;
using LightRail.Amqp.Types;

namespace LightRail.Amqp.Messaging
{
    public class Properties : DescribedList
    {
        public Properties()
            : base(Descriptor.Properties)
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