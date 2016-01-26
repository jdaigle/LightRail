namespace LightRail.Amqp.Protocol
{
    public static class ConnectionStateEnumExtensions
    {
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

    }
}
