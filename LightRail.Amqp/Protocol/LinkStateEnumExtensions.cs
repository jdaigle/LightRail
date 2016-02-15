namespace LightRail.Amqp.Protocol
{
    public static class LinkStateEnumExtensions
    {
        public static bool CanReceiveDispositionFrames(this LinkStateEnum state)
        {
            return state == LinkStateEnum.ATTACHED ||
                   state == LinkStateEnum.DETACHED ||
                   state == LinkStateEnum.DETACH_SENT ||
                   state == LinkStateEnum.DETACH_RECEIVED;
        }
    }
}
