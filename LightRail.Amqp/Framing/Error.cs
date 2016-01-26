using LightRail.Amqp.Types;

namespace LightRail.Amqp.Framing
{
    public sealed class Error : AmqpFrame
    {
        public Error()
            : base(DescribedListCodec.Error)
        {
        }

        /// <summary>
        /// A symbolic value indicating the error condition.
        /// </summary>
        [AmqpDescribedListIndex(0)]
        public string Condition { get; set; }

        /// <summary>
        /// This text supplies any supplementary details not indicated by the condition field. This text can be
        /// logged as an aid to resolving issues.
        /// </summary>
        [AmqpDescribedListIndex(1)]
        public string Description { get; set; }

        /// <summary>
        /// Map carrying information about the error condition.
        /// </summary>
        [AmqpDescribedListIndex(2)]
        public Fields Info { get; set; }
    }
}
