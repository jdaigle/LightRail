namespace LightRail.Amqp.Protocol
{
    public static class SessionStateEnumExtensions
    {
        public static bool CanSendFrames(this SessionStateEnum state)
        {
            return state == SessionStateEnum.BEGIN_SENT ||
                   state == SessionStateEnum.MAPPED ||
                   state == SessionStateEnum.END_RCVD;
        }

        public static bool CanReceiveFrames(this SessionStateEnum state)
        {
            return state == SessionStateEnum.BEGIN_RCVD ||
                   state == SessionStateEnum.MAPPED ||
                   state == SessionStateEnum.END_SENT ||
                   state == SessionStateEnum.DISCARDING;
        }
    }
}
