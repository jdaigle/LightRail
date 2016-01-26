using LightRail.Amqp.Types;

namespace LightRail.Amqp.Framing
{
    /// <summary>
    /// Detach the link endpoint from the session.
    /// 
    /// <type name = "detach" class="composite" source="list" provides="frame">
    ///     <descriptor name = "amqp:detach:list" code="0x00000000:0x00000016"/>
    ///     <field name = "handle" type="handle" mandatory="true"/>
    ///     <field name = "closed" type="boolean" default="false"/>
    ///     <field name = "error" type="error"/>
    /// </type>
    /// 
    /// Detach the link endpoint from the session.This unmaps the handle and makes it available for use by other links.

    /// </summary>
    public sealed class Detach : AmqpFrame
    {
        public Detach()
            : base(DescribedListCodec.Detach)
        {
        }

        /// <summary>
        /// the local handle of the link to be detached
        /// </summary>
        [AmqpDescribedListIndex(0)]
        public uint Handle { get; set; }

        /// <summary>
        /// if true then the sender has closed the link
        /// </summary>
        [AmqpDescribedListIndex(1)]
        public bool Closed { get; set; }

        /// <summary>
        /// If set, this field indicates that the link is being detached due to an error condition. The value of the
        /// field SHOULD contain details on the cause of the error.
        /// </summary>
        [AmqpDescribedListIndex(2)]
        public Error Error { get; set; }
    }
}
