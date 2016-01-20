using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LightRail.Client.Config;
using LightRail.Client.Logging;
using LightRail.Client.Reflection;
using LightRail.Client.Transport;
using LightRail.Client.Util;

namespace LightRail.Client
{
    public class PipelineServiceBus : IBus, IBusControl
    {
        public PipelineServiceBus(IServiceBusConfig config)
        {
            this.Name = Guid.NewGuid().ToString();

            this.MessageMapper = config.MessageMapper;

            this.staticRoutes = new Dictionary<Type, HashSet<string>>();
            foreach (var mapping in config.MessageEndpointMappings.OrderByDescending(m => m))
            {
                mapping.Configure((messageType, endpoint) =>
                {
                    if (!staticRoutes.ContainsKey(messageType))
                    {
                        staticRoutes[messageType] = new HashSet<string>();
                    }
                    staticRoutes[messageType].Add(endpoint);
                    logger.Debug("Mapping message type [{0}] to endpoint [{1}]", messageType.FullName, endpoint);
                });
            }

            this.Transport = config.CreateTransportSender();

            List<PipelineMessageReceiver> receivers;
            MessageReceivers = receivers = new List<PipelineMessageReceiver>();
            foreach (var receiverConfig in config.MessageReceivers)
            {
                var receiver = new PipelineMessageReceiver(this, receiverConfig, config);
                receivers.Add(receiver);
                this.startupActions.Add(() => receiver.Start());
            }
        }

        private static ILogger logger = LogManager.GetLogger("LightRail.Client");

        public string Name { get; }
        public ITransportSender Transport { get; }
        public IMessageMapper MessageMapper { get; }
        public IEnumerable<PipelineMessageReceiver> MessageReceivers { get; }

        public event EventHandler<MessageProcessedEventArgs> MessageProcessed;
        public event EventHandler<PoisonMessageDetectedEventArgs> PoisonMessageDetected;

        private readonly List<Action> startupActions = new List<Action>();
        private readonly Dictionary<Type, HashSet<String>> staticRoutes;

        public IBus Start()
        {
            logger.Info("Starting PipelineServiceBus[{0}]", Name);
            startupActions.ForEach(a => a());
            return this;
        }

        public void Stop()
        {
            Stop(TimeSpan.MaxValue);
        }

        public void Stop(TimeSpan timeSpan)
        {
            logger.Info("Stopping PipelineServiceBus[{0}]", Name);
            foreach (var receiver in MessageReceivers)
            {
                receiver.Stop(timeSpan);
            }
        }

        public void Send<T>(T message)
        {
            SendInternal(message, GetAddressesForMessageType(typeof(T)));
        }

        public void Send<T>(T message, string address)
        {
            if (address.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException(nameof(address));
            }
            SendInternal(message, new[] { address });
        }

        public void SendInternal<T>(T message, IEnumerable<string> addresses)
        {
            if (addresses.Count() == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(addresses), "Must have an address to send the message to");
            }
            if (message == null)
            {
                return;
            }
            var transportMessage = new OutgoingTransportMessage(new Dictionary<string, string>(), message);
            Transport.Send(transportMessage, addresses);
        }

        private HashSet<string> GetAddressesForMessageType(Type messageType)
        {
            HashSet<string> addresses = null;
            if (staticRoutes.TryGetValue(messageType, out addresses))
            {
                return addresses;
            }

            if (!messageType.IsInterface)
            {
                var interfaces = messageType.GetInterfaces();
                foreach (var _interface in interfaces)
                {
                    if (staticRoutes.TryGetValue(_interface, out addresses))
                    {
                        return addresses;
                    }
                }

                var t = MessageMapper.GetMappedTypeFor(messageType);
                if (t != null && t != messageType)
                {
                    return GetAddressesForMessageType(t);
                }
            }

            return addresses;
        }

        public T CreateInstance<T>()
        {
            return MessageMapper.CreateInstance<T>();
        }

        public T CreateInstance<T>(Action<T> action)
        {
            var instance = MessageMapper.CreateInstance<T>();
            if (action != null)
            {
                action(instance);
            }
            return instance;
        }

        internal void OnMessageProcessed(PipelineMessageReceiver sender, MessageProcessedEventArgs args)
        {
            if (MessageProcessed != null)
            {
                var callback = MessageProcessed;
                Task.Factory.StartNew(() => callback(sender, args));
            }
        }

        internal void OnPoisonMessageDetected(PipelineMessageReceiver sender, PoisonMessageDetectedEventArgs args)
        {
            if (PoisonMessageDetected != null)
            {
                var callback = PoisonMessageDetected;
                Task.Factory.StartNew(() => callback(sender, args));
            }
        }
    }
}