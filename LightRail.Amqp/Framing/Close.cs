using LightRail.Amqp.Types;

namespace LightRail.Amqp.Framing
{
    public sealed class Close : AmqpFrame
    {
        public Close()
            : base(DescribedListCodec.Close)
        {
        }

        /// <summary>
        /// If set, this field indicates that the connection is being closed due to an error condition. The value
        /// of the field SHOULD contain details on the cause of the error.
        /// </summary>
        [AmqpDescribedListIndex(0)]
        public Error Error { get; set; }
    }
}
