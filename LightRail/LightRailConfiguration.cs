using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightRail
{
    public class LightRailConfiguration
    {
        public LightRailConfiguration()
        {
            // Default Values
            this.UseSerialization<JsonMessageSerializer>();
            this.MessageHandlerCollection = new MessageHandlerCollection();
#if DEBUG
            this.LogManager = new ConsoleLogManager();
#endif
        }

        public void UseSerialization<TMessageSerializer>()
            where TMessageSerializer : IMessageSerializer
        {
            this.MessageSerializerConstructor = () => Activator.CreateInstance<TMessageSerializer>();
        }

        public Func<IMessageSerializer> MessageSerializerConstructor { get; set; }

        public void UseTransport<TTransport, TTransportConfig>()
            where TTransport : ITransport
            where TTransportConfig : AbstractTransportConfiguration
        {
            this.TransportConfiguration = Activator.CreateInstance<TTransportConfig>();
            this.TransportConstructor = () => (ITransport)Activator.CreateInstance(typeof(TTransport), this, this.TransportConfiguration);
        }

        public AbstractTransportConfiguration TransportConfiguration { get; private set; }
        public Func<ITransport> TransportConstructor { get; set; }

        public TTransportConfig TransportConfigurationAs<TTransportConfig>()
            where TTransportConfig : AbstractTransportConfiguration
        {
            return (TTransportConfig)this.TransportConfiguration;
        }

        public void Handle<TMessage>(Action<TMessage> messageHandler)
        {
            MessageHandlerCollection.Register(messageHandler);
        }

        public MessageHandlerCollection MessageHandlerCollection { get; private set; }

        public void UseLogger<TLogManager>()
            where TLogManager : ILogManager, new()
        {
            LogManager = new TLogManager();
        }

        public ILogManager LogManager { get; set; }
    }
}
