using System;
using LightRail.Amqp.Types;

namespace LightRail.Amqp.Messaging
{
    /// <remarks>
    /// The message-annotations section is used for properties of the message which are aimed at the infrastructure
    /// and SHOULD be propagated across every delivery step.Message annotations convey information about the
    /// message. Intermediaries MUST propagate the annotations unless the annotations are explicitly augmented or
    /// modified(e.g., by the use of the modified outcome).
    /// 
    /// The capabilities negotiated on link attach and on the source and target can be used to establish which annotations
    /// a peer understands; however, in a network of AMQP intermediaries it might not be possible to know if
    /// every intermediary will understand the annotation.Note that for some annotations it might not be necessary for
    /// the intermediary to understand their purpose, i.e., they could be used purely as an attribute which can be filtered
    /// on.
    /// 
    /// A registry of defined annotations and their meanings is maintained[AMQPMESSANN].
    /// 
    /// If the message-annotations section is omitted, it is equivalent to a message-annotations section containing an
    /// empty map of annotations.
    /// </remarks>
    public class MessageAnnotations : DescribedList
    {
        public MessageAnnotations()
            : base(Descriptor.MessageAnnotations)
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