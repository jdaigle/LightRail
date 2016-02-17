namespace LightRail.Amqp.Protocol
{
    public enum LinkStateEnum
    {
        /// <summary>
        /// Fully DETACHED state. No endpointis attached.
        /// </summary>
        DETACHED,
        /// <summary>
        /// Half-attached. THe local endpoint is attached but the remote is not.
        /// </summary>
        ATTACH_SENT,
        /// <summary>
        /// Half-attached. THe remote endpoint is attached but the local is not.
        /// </summary>
        ATTACH_RECEIVED,
        /// <summary>
        /// Fully ATTACHED at both endpoints.
        /// </summary>
        ATTACHED,

        DETACH_SENT,
        DETACH_RECEIVED,

        /// <summary>
        /// The DESTROYED state is a variant of the DETACH_SENT state where the detach is triggered by
        /// an error. If any input (other than a detach) related to the endpoint 
        /// either via the input handle or delivery-ids be received, the session MUST be 
        /// terminated with an errant-link session-error.
        /// </summary>
        DESTROYED,
    }
}
