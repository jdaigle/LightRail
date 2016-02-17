using LightRail.Amqp.Types;

namespace LightRail.Amqp.Framing
{
    /// <summary>
    /// Signal a connection close.
    /// 
    /// <type name = "close" class="composite" source="list" provides="frame">
    ///     <descriptor name = "amqp:close:list" code="0x00000000:0x00000018"/>
    ///     <field name = "error" type="error"/>
    /// </type>
    /// 
    /// Sending a close signals that the sender will not be sending any more frames (or bytes of any other kind) on the
    /// connection. Orderly shutdown requires that this frame MUST be written by the sender. It is illegal to send any
    /// more frames (or bytes of any other kind) after sending a close frame.
    /// </summary>
    public sealed class Close : AmqpFrame
    {
        public Close() : base(DescribedTypeCodec.Close) { }

        /// <summary>
        /// If set, this field indicates that the connection is being closed due to an error condition. The value
        /// of the field SHOULD contain details on the cause of the error.
        /// </summary>
        [AmqpDescribedListIndex(0)]
        public Error Error { get; set; }
    }
}
