namespace LightRail.Client
{
    public interface IBus
        : IMessageCreator
        , IBusEvents
    {
        /// <summary>
        /// Sends a message to the configured destination
        /// </summary>
        void Send<T>(T message);
        /// <summary>
        /// Sends a message to a specific address
        /// </summary>
        void Send<T>(T message, string address);
    }
}
