using LightRail.Amqp.Types;

namespace LightRail.Amqp.Messaging
{
    /// <summary>
    /// Immutable properties of the message.
    /// 
    /// <type name = "properties" class="composite" source="list" provides="section">
    ///     <descriptor name = "amqp:properties:list" code="0x00000000:0x00000073"/>
    ///     <field name = "message-id" type="*" requires="message-id"/>
    ///     <field name = "user-id" type="binary"/>
    ///     <field name = "to" type="*" requires="address"/>
    ///     <field name = "subject" type="string"/>
    ///     <field name = "reply-to" type="*" requires="address"/>
    ///     <field name = "correlation-id" type="*" requires="message-id"/>
    ///     <field name = "content-type" type="symbol"/>
    ///     <field name = "content-encoding" type="symbol"/>
    ///     <field name = "absolute-expiry-time" type="timestamp"/>
    ///     <field name = "creation-time" type="timestamp"/>
    ///     <field name = "group-id" type="string"/>
    ///     <field name = "group-sequence" type="sequence-no"/>
    ///     <field name = "reply-to-group-id" type="string"/>
    /// </type>
    /// 
    /// The properties section is used for a defined set of standard properties of the message.The properties section is
    /// part of the bare message; therefore, if retransmitted by an intermediary, it MUST remain unaltered.
    /// </summary>
    public class Properties : DescribedList
    {
        public Properties() : base(MessagingDescriptors.Properties) { }

        // TODO: property fields
    }
}