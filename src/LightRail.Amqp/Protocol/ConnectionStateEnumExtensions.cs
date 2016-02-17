namespace LightRail.Amqp.Protocol
{
    public static class ConnectionStateEnumExtensions
    {
        public static bool HasSentHeader(this ConnectionStateEnum state)
        {
            return state == ConnectionStateEnum.HDR_SENT ||
                   state == ConnectionStateEnum.HDR_EXCH ||
                   state == ConnectionStateEnum.OPEN_PIPE ||
                   state == ConnectionStateEnum.OC_PIPE;
        }

        public static bool IsExpectingProtocolHeader(this ConnectionStateEnum state)
        {
            return state == ConnectionStateEnum.START ||
                   state == ConnectionStateEnum.HDR_SENT ||
                   state == ConnectionStateEnum.OPEN_PIPE ||
                   state == ConnectionStateEnum.OC_PIPE;
        }

        public static bool ShouldIgnoreReceivedData(this ConnectionStateEnum state)
        {
            return state == ConnectionStateEnum.END ||
                   state == ConnectionStateEnum.CLOSED_RCVD;
        }

        public static bool CanSendFrames(this ConnectionStateEnum state)
        {
            return state == ConnectionStateEnum.OPENED ||
                   state == ConnectionStateEnum.HDR_EXCH ||
                   state == ConnectionStateEnum.OPEN_PIPE ||
                   state == ConnectionStateEnum.OPEN_RCVD ||
                   state == ConnectionStateEnum.OPEN_SENT ||
                   state == ConnectionStateEnum.CLOSED_RCVD;
        }

        public static bool CanReceiveFrames(this ConnectionStateEnum state)
        {
            return state == ConnectionStateEnum.OPENED ||
                   state == ConnectionStateEnum.HDR_EXCH ||
                   state == ConnectionStateEnum.OPEN_PIPE ||
                   state == ConnectionStateEnum.OC_PIPE ||
                   state == ConnectionStateEnum.OPEN_RCVD ||
                   state == ConnectionStateEnum.OPEN_SENT ||
                   state == ConnectionStateEnum.CLOSE_PIPE ||
                   state == ConnectionStateEnum.CLOSE_SENT;
        }
    }
}
