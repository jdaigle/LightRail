namespace LightRail.Amqp.Protocol
{
    public enum LinkReceiverSettlementModeEnum
    {
        /// <summary>
        /// The receiver will spontaneously settle all incoming transfers.
        /// </summary>
        First = 0,
        /// <summary>
        /// The receiver will only settle after sending the disposition to the
        /// sender and receiving a disposition indicating settlement of the delivery from the sender.
        /// </summary>
        Second = 1,
    }
}