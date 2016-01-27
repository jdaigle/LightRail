namespace LightRail.Amqp.Protocol
{
    public enum LinkStateEnum
    {
        START,
        ATTACH_SENT,
        ATTACH_RECEIVED,
        ATTACHED,
        DETACH_SENT,
        DETACH_RECEIVED,

        /// <summary>
        /// The DISCARDING state is a variant of the DETACH_SENT state where the end is triggered by
        /// an error. In this case any incoming frames on the session MUST be silently discarded until
        /// the peer’s Detach frame is received.
        /// </summary>
        DISCARDING,
    }
}
