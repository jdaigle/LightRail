using System;
using LightRail.Amqp.Types;

namespace LightRail.Amqp.Messaging
{
    /// <summary>
    /// Transport footers for a message.
    /// </summary>
    /// <remarks>
    /// The footer section is used for details about the message or delivery which can only be calculated or evaluated
    /// once the whole bare message has been constructed or seen(for example message hashes, HMACs, signatures
    /// and encryption details).
    /// 
    /// A registry of defined footers and their meanings is maintained[AMQPFOOTER].
    /// </remarks>
    public class Footer : DescribedList
    {
        public Footer()
            : base(MessagingDescriptors.Footer)
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