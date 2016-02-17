namespace LightRail.Amqp.Protocol
{
    public enum SessionStateEnum
    {
        /// <summary>
        /// In the UNMAPPED state, the session endpoint is not mapped to any incoming or outgoing
        /// channels on the connection endpoint. In this state an endpoint cannot send or receive
        /// frames.
        /// </summary>
        UNMAPPED,

        /// <summary>
        /// In the BEGIN SENT state, the session endpoint is assigned an outgoing channel number,
        /// but there is no entry in the incoming channel map. In this state the endpoint MAY send
        /// frames but cannot receive them.
        /// </summary>
        BEGIN_SENT,

        /// <summary>
        /// In the BEGIN RCVD state, the session endpoint has an entry in the incoming channel map,
        /// but has not yet been assigned an outgoing channel number. The endpoint MAY receive
        /// frames, but cannot send them.
        /// </summary>
        BEGIN_RCVD,

        /// <summary>
        /// In the MAPPED state, the session endpoint has both an outgoing channel number and an
        /// entry in the incoming channel map. The endpoint MAY both send and receive frames.
        /// </summary>
        MAPPED,

        /// <summary>
        /// In the END SENT state, the session endpoint has an entry in the incoming channel map,
        /// but is no longer assigned an outgoing channel number. The endpoint MAY receive frames,
        /// but cannot send them.
        /// </summary>
        END_SENT,

        /// <summary>
        /// In the END RCVD state, the session endpoint is assigned an outgoing channel number, but
        /// there is no entry in the incoming channel map. The endpoint MAY send frames, but cannot
        /// receive them.
        /// </summary>
        END_RCVD,

        /// <summary>
        /// The DISCARDING state is a variant of the END SENT state where the end is triggered by
        /// an error. In this case any incoming frames on the session MUST be silently discarded until
        /// the peer’s end frame is received.
        /// </summary>
        DISCARDING,
    }
}
