using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LightRail.Dispatch;
using LightRail.FastServiceLocator;
using LightRail.Logging;
using LightRail.Reflection;

namespace LightRail
{
    public class LightRailConfiguration
    {
        private static ILogger logger = LogManager.GetLogger("LightRail.Config");

        public LightRailConfiguration()
        {
            // Default Values
            this.ServiceLocator = new FastServiceLocatorImpl(new FastContainer());
            this.AssembliesToScan = new HashSet<Assembly>();
            this.UseSerialization<JsonMessageSerializer>();
            this.MessageTypeConventions = new MessageTypeConventions();
            this.MessageHandlerCollection = new MessageHandlerCollection(this.MessageTypeConventions);
            this.PipelinedBehaviors = new List<PipelinedBehavior>();
            this.MessageEndpointMappings = new List<MessageEndpointMapping>();
            this.SubscriptionMapping = new HashSet<Type>();
        }

        public MessageTypeConventions MessageTypeConventions { get; private set; }
        public MessageHandlerCollection MessageHandlerCollection { get; private set; }
        public Func<IMessageSerializer> MessageSerializerConstructor { get; set; }
        public AbstractTransportConfiguration TransportConfiguration { get; private set; }
        public Func<ITransport> TransportConstructor { get; private set; }
        public HashSet<Assembly> AssembliesToScan { get; private set; }
        public List<PipelinedBehavior> PipelinedBehaviors { get; private set; }
        public IServiceLocator ServiceLocator { get; set; }
        public List<MessageEndpointMapping> MessageEndpointMappings { get; private set; }
        public HashSet<Type> SubscriptionMapping { get; private set; }
        public ISubscriptionStorage SubscriptionStorage { get; private set; }

        /// <summary>
        /// On startup send a "subscribe" message for the specified type
        /// to the specified destination.
        /// </summary>
        public void Subscribe(Type type)
        {
            SubscriptionMapping.Add(type);
        }

        public void UseSubscriptionStorage<TSubscriptionStorage>()
            where TSubscriptionStorage: ISubscriptionStorage, new()
        {
            this.SubscriptionStorage = new TSubscriptionStorage();
        }

        public void UseSubscriptionStorage(ISubscriptionStorage subscriptionStorage)
        {
            this.SubscriptionStorage = subscriptionStorage;
        }

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

        public void AddPipelinedBehavior(PipelinedBehavior behavior)
        {
            this.PipelinedBehaviors.Add(behavior);
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

        public void AddMessageEndpointMapping(MessageEndpointMapping mapping)
        {
            this.MessageEndpointMappings.Add(mapping);
        }

        public void AddMessageEndpointMapping(string endpoint, string assemblyName, string typeFullName = null)
        {
            AddMessageEndpointMapping(new MessageEndpointMapping(endpoint, assemblyName, typeFullName));
        }

        public IStartableBus CreateBus()
        {
            if (this.ServiceLocator == null)
            {
                throw new InvalidConfigurationException("A ServiceLocator instance must be specified.");
            }

            var messageTypes = MessageTypeConventions.ScanAssembliesForMessageTypes(AssembliesToScan);
            var messageMapper = new MessageMapper(MessageTypeConventions);
            messageMapper.Initialize(messageTypes);
            var messageHandlers = new MessageHandlerCollection(MessageTypeConventions);
            messageHandlers.ScanAssembliesAndInitialize(AssembliesToScan);

            var staticRoutes = new Dictionary<Type, string>();
            foreach (var mapping in MessageEndpointMappings.OrderByDescending(m => m))
            {
                mapping.Configure((messageType, endpoint) =>
                {
                    if (!MessageTypeConventions.IsMessageType(messageType))
                    {
                        return;
                    }
                    staticRoutes[messageType] = endpoint;
                    logger.Debug("Mapping message type [{0}] to endpoint [{1}]", messageType.FullName, endpoint);
                });
            }

            return new LightRailServiceBus(this, messageMapper, messageHandlers, staticRoutes);
        }
    }
}
