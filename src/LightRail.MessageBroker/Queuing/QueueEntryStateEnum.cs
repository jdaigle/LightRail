namespace LightRail.MessageBroker.Queuing
{
    public enum QueueEntryStateEnum : int
    {
        AVAILABLE = 0,
        ACQUIRED = 1,
        ARCHIVED = 2,
    }
}