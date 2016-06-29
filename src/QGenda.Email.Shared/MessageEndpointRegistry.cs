using LightRail.ServiceBus.Config;

namespace QGenda.Email
{
    public class MessageEndpointRegistry : AbstractMessageEndpointRegistry
    {
        public MessageEndpointRegistry()
        {
            AddMessageEndpointMapping<SendEmail>("//QGenda/Email");
        }
    }
}
