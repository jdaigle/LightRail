namespace LightRail.ServiceBus
{
    public interface IMessageHandler<in TMessage>
    {
        void Handle(TMessage message);
    }
}
