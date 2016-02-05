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
    /// 
    /// <type name="message-annotations" class="restricted" source="annotations" provides="section">
    ///     <descriptor name = "amqp:message-annotations:map" code="0x00000000:0x00000072"/>
    /// </type>
    /// 
    /// </remarks>
    public sealed class MessageAnnotations : DescribedMap
    {
        public MessageAnnotations() : base(MessagingDescriptors.MessageAnnotations) { }
    }
}