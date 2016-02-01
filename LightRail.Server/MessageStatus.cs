namespace LightRail.Server
{
    public enum MessageStatus : byte
    {
        /// <summary>
        /// The message is available for transfer.
        /// </summary>
        AVAILABLE = 0,
        /// <summary>
        /// The message has been transferred and is awaiting settlement. The message is not available for transfer.
        /// </summary>
        ACQUIRED = 1,
        /// <summary>
        /// The message has been settled with a terminal outcome. The message is not available for transfer.
        /// It will be cleaned up.
        /// </summary>
        ARCHIVED = 2,
    }
}