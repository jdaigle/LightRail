using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using LightRail.Client.Config;
using LightRail.Client.Dispatch;
using LightRail.Client.Logging;
using LightRail.Client.Pipeline;
using LightRail.Client.Reflection;
using LightRail.Client.Util;

namespace LightRail.Client
{
    public class PipelineServiceBus : IBus, IBusControl
    {
        public PipelineServiceBus(IServiceBusConfig config)
        {
            this.Name = Guid.NewGuid().ToString();

            this.ServiceLocator = config.ServiceLocator;
            this.MessageMapper = new ReflectionMessageMapper();
            this.MessageHandlers = config.MessageHandlers;

            var messageHandlerPipelinedBehaviors = config.PipelinedBehaviors.ToList(); // copy the list
            // ensure MessageHandlerDispatchBehavior is added to the end
            messageHandlerPipelinedBehaviors.RemoveAll(x => x is MessageHandlerDispatchBehavior);
            messageHandlerPipelinedBehaviors.Add(new MessageHandlerDispatchBehavior());
            this.compiledMessageHandlerPipeline = PipelinedBehavior.CompileMessageHandlerPipeline(messageHandlerPipelinedBehaviors);

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

            //this.Transport = config.TransportConstructor();
            this.Transport.MessageAvailable += (sender, messageAvailable) => OnMessageAvailable(messageAvailable);
            this.Transport.PoisonMessageDetected += (sender, poisonMessageDetected) => OnPoisonMessageDetected(poisonMessageDetected);
            this.startupActions.Add(() => this.Transport.Start());
        }

        private static ILogger logger = LogManager.GetLogger("LightRail.Client");

        public string Name { get; }
        public ITransport Transport { get; }
        public MessageHandlerCollection MessageHandlers { get; }
        public IServiceLocator ServiceLocator { get; }
        public IMessageMapper MessageMapper { get; }

        public event EventHandler<MessageProcessedEventArgs> MessageProcessed;
        public event EventHandler<PoisonMessageDetectedEventArgs> PoisonMessageDetected;

        private readonly List<Action> startupActions = new List<Action>();
        private readonly Func<MessageContext, Task> compiledMessageHandlerPipeline;
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
            this.Transport.Stop(timeSpan);
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

        private void OnMessageAvailable(MessageAvailableEventArgs value)
        {
            throw new NotImplementedException();
            //using (var childServiceLocator = this.ServiceLocator.CreateNestedContainer())
            //{
            //    var currentMessageContext = new MessageContext(this, value.TransportMessage.MessageId, value.TransportMessage.Headers, childServiceLocator);

            //    // register a bunch of things we might want to use during the message handling
            //    childServiceLocator.RegisterSingleton<IBus>(this);
            //    childServiceLocator.RegisterSingleton(this.MessageHandlers);
            //    childServiceLocator.RegisterSingleton<IMessageMapper>(this.MessageMapper);
            //    childServiceLocator.RegisterSingleton<ITransport>(this.Transport);
            //    childServiceLocator.RegisterSingleton<MessageContext>(currentMessageContext);

            //    try
            //    {
            //        object message = null;
            //        try
            //        {
            //            message = DeserializeMessage(value);
            //        }
            //        catch (Exception e)
            //        {
            //            logger.Error(e, "Cannot deserialize message.");
            //            // The message cannot be deserialized. There is no reason
            //            // to retry.
            //            throw new CannotDeserializeMessageException(e);
            //        }
            //        currentMessageContext.CurrentMessage = message;
            //        currentMessageContext.SerializedMessageData = value.TransportMessage.SerializedMessageData;

            //        var stopwatch = Stopwatch.StartNew();
            //        var startTimestamp = DateTime.UtcNow;

            //        compiledMessageHandlerPipeline(currentMessageContext);

            //        var endTimestamp = DateTime.UtcNow;
            //        stopwatch.Stop();

            //        OnMessageProcessed(new MessageProcessedEventArgs(currentMessageContext, startTimestamp, endTimestamp, stopwatch.Elapsed.TotalMilliseconds));
            //    }
            //    finally
            //    {
            //        currentMessageContext = null;
            //    }
            //}
        }

        public void OnMessageProcessed(MessageProcessedEventArgs args)
        {
            if (MessageProcessed != null)
            {
                var callback = MessageProcessed;
                Task.Factory.StartNew(() => callback(this, args));
            }
        }

        public void OnPoisonMessageDetected(PoisonMessageDetectedEventArgs args)
        {
            if (PoisonMessageDetected != null)
            {
                var callback = PoisonMessageDetected;
                Task.Factory.StartNew(() => callback(this, args));
            }
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
    }
}
