namespace LightRail.Amqp.Protocol
{
    public enum ConnectionStateEnum
    {
        /// <summary>
        /// In this state a connection exists, but nothing has been sent or received. This is the state an
        /// implementation would be in immediately after performing a socket connect or socket accept.
        /// </summary>
        START,

        /// <summary>
        /// In this state the connection header has been received from the peer but a connection header
        /// has not been sent.
        /// </summary>
        HDR_RCVD,

        /// <summary>
        /// In this state the connection header has been sent to the peer but no connection header has
        /// been received.
        /// </summary>
        HDR_SENT,

        /// <summary>
        /// In this state the connection header has been sent to the peer and a connection header has
        /// been received from the peer.
        /// </summary>
        HDR_EXCH,

        /// <summary>
        /// In this state both the connection header and the open frame have been sent but nothing has
        /// been received.
        /// </summary>
        OPEN_PIPE,

        /// <summary>
        /// In this state, the connection header, the open frame, any pipelined connection traffic, and
        /// the close frame have been sent but nothing has been received.
        /// </summary>
        OC_PIPE,

        /// <summary>
        /// In this state the connection headers have been exchanged. An open frame has been received
        /// from the peer but an open frame has not been sent.
        /// </summary>
        OPEN_RCVD,

        /// <summary>
        /// In this state the connection headers have been exchanged. An open frame has been sent
        /// to the peer but no open frame has yet been received.
        /// </summary>
        OPEN_SENT,

        /// <summary>
        /// In this state the connection headers have been exchanged. An open frame, any pipelined
        /// connection traffic, and the close frame have been sent but no open frame has yet been
        /// received from the peer.
        /// </summary>
        CLOSE_PIPE,

        /// <summary>
        /// In this state the connection header and the open frame have been both sent and received.
        /// </summary>
        OPENED,

        /// <summary>
        /// In this state a close frame has been received indicating that the peer has initiated an AMQP
        /// close. No further frames are expected to arrive on the connection; however, frames can still
        /// be sent. If desired, an implementation MAY do a TCP half-close at this point to shut down
        /// the read side of the connection.
        /// </summary>
        CLOSED_RCVD,

        /// <summary>
        /// In this state a close frame has been sent to the peer. It is illegal to write anything more
        /// onto the connection, however there could potentially still be incoming frames. If desired,
        /// an implementation MAY do a TCP half-close at this point to shutdown the write side of the
        /// connection.
        /// </summary>
        CLOSE_SENT,

        /// <summary>
        /// The DISCARDING state is a variant of the CLOSE SENT state where the close is triggered
        /// by an error. In this case any incoming frames on the connection MUST be silently discarded
        /// until the peer’s close frame is received.
        /// </summary>
        DISCARDING,

        /// <summary>
        /// In this state it is illegal for either endpoint to write anything more onto the connection. The
        /// connection can be safely closed and discarded.
        /// </summary>
        END,
    }
}
