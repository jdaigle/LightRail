using LightRail.Amqp.Types;

namespace LightRail.Amqp.Framing
{
    /// <summary>
    /// End the session.
    /// 
    /// <type name = "end" class="composite" source="list" provides="frame">
    ///     <descriptor name = "amqp:end:list" code="0x00000000:0x00000017"/>
    ///     <field name = "error" type="error"/>
    /// </type>
    /// 
    /// Indicates that the session has ended.
    /// </summary>
    public sealed class End : AmqpFrame
    {
        public End()
            : base(DescribedListCodec.End)
        {
        }

        /// <summary>
        /// If set, this field indicates that the session is being ended due to an error condition. The value of
        /// the field SHOULD contain details on the cause of the error.
        /// </summary>
        [AmqpDescribedListIndex(0)]
        public Error Error { get; set; }
    }
}
