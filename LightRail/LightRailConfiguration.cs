using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LightRail
{
    public class LightRailConfiguration
    {
        public LightRailConfiguration()
        {
            // Default Values
            this.AssembliesToScan = new HashSet<Assembly>();
            this.UseSerialization<JsonMessageSerializer>();
            this.MessageHandlerCollection = new MessageHandlerCollection();
            this.MessageTypeConventions = new MessageTypeConventions();
#if DEBUG
            this.LogManager = new ConsoleLogManager();
#endif
        }

        public ILogManager LogManager { get; set; }
        public MessageTypeConventions MessageTypeConventions { get; private set; }
        public MessageHandlerCollection MessageHandlerCollection { get; private set; }
        public Func<IMessageSerializer> MessageSerializerConstructor { get; set; }
        public AbstractTransportConfiguration TransportConfiguration { get; private set; }
        public Func<ITransport> TransportConstructor { get; set; }
        public ISet<Assembly> AssembliesToScan { get; set; }

        public void UseSerialization<TMessageSerializer>()
            where TMessageSerializer : IMessageSerializer
        {
            this.MessageSerializerConstructor = () => Activator.CreateInstance<TMessageSerializer>();
        }

        public void UseTransport<TTransport, TTransportConfig>()
            where TTransport : ITransport
            where TTransportConfig : AbstractTransportConfiguration
        {
            this.TransportConfiguration = Activator.CreateInstance<TTransportConfig>();
            this.TransportConstructor = () => (ITransport)Activator.CreateInstance(typeof(TTransport), this, this.TransportConfiguration);
        }

        public TTransportConfig TransportConfigurationAs<TTransportConfig>()
            where TTransportConfig : AbstractTransportConfiguration
        {
            return (TTransportConfig)this.TransportConfiguration;
        }

        public void Handle<TMessage>(Action<TMessage> messageHandler)
        {
            MessageHandlerCollection.Register(messageHandler);
        }

        public void UseLogger<TLogManager>()
            where TLogManager : ILogManager, new()
        {
            LogManager = new TLogManager();
        }

        public void AddAssembliesToScan(IEnumerable<Assembly> assemblies)
        {
            foreach (var assembly in assemblies)
            {
                AssembliesToScan.Add(assembly);
            }
        }

        public void AddAssembliesToScan(params Assembly[] assemblies)
        {
            AddAssembliesToScan((IEnumerable<Assembly>)assemblies);
        }

        public void AddAssemblyToScan(Assembly assembly)
        {
            AddAssembliesToScan(new[] { assembly });
        }
    }
}
