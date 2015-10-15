using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightRail.Dispatch;
using LightRail.Logging;

namespace LightRail
{
    public class LightRailServiceBus : IStartableBus, IServiceBusEvents
    {
        public LightRailServiceBus(LightRailConfiguration config, IMessageMapper mapper, MessageHandlerCollection messageHandlers, IDictionary<Type, string> staticRoutes)
        {
            this.ServiceLocator = config.ServiceLocator;
            this.MessageSerializer = config.MessageSerializerConstructor();
            this.MessageMapper = mapper;
            this.MessageHandlers = messageHandlers;

            this.messageHandlerPipelinedBehaviors = config.PipelinedBehaviors.ToList();
            // ensure MessageHandlerDispatchBehavior is added to the end
            this.messageHandlerPipelinedBehaviors.RemoveAll(x => x is MessageHandlerDispatchBehavior);
            this.messageHandlerPipelinedBehaviors.Add(new MessageHandlerDispatchBehavior());
            this.compiledMessagePipeline = PipelinedBehavior.CompileMessageHandlerPipeline(this.messageHandlerPipelinedBehaviors);
            this.staticRoutes = staticRoutes;
            this.SubscriptionStorage = config.SubscriptionStorage;

            this.Transport = config.TransportConstructor();
            this.Transport.MessageAvailable += (sender, messageAvailable) => OnMessageAvailable(messageAvailable);
            this.Transport.PoisonMessageDetected += (sender, poisonMessageDetected) => OnPoisonMessageDetected(poisonMessageDetected);

            this.startupActions.Add(() => this.Transport.Start());
            if (config.SubscriptionMapping.Any())
            {
                this.startupActions.Add(() =>
                {
                    if (this.SubscriptionStorage == null)
                    {
                        throw new InvalidConfigurationException("SubscriptionStorage is required when setting up subscriptions.");
                    }
                    foreach (var messageType in config.SubscriptionMapping)
                    {
                        if (config.MessageTypeConventions.IsMessageType(messageType))
                        {
                            this.SubscriptionStorage.Subscribe(this.Transport.OriginatingAddress, new[] { messageType.FullName });
                        }
                    }
                });
            }
        }

        public IBus Start()
        {
            logger.Info("Starting ServiceBusClient");
            startupActions.ForEach(a => a());
            return this;
        }

        private static ILogger logger = LogManager.GetLogger("LightRail");

        public ITransport Transport { get; private set; }
        public IMessageSerializer MessageSerializer { get; private set; }
        public MessageHandlerCollection MessageHandlers { get; private set; }
        public IMessageMapper MessageMapper { get; private set; }
        public IServiceLocator ServiceLocator { get; private set; }
        public ISubscriptionStorage SubscriptionStorage { get; private set; }

        private readonly List<Action> startupActions = new List<Action>();
        private readonly List<PipelinedBehavior> messageHandlerPipelinedBehaviors;
        private readonly Action<MessageContext> compiledMessagePipeline;
        private readonly IDictionary<Type, string> staticRoutes;

        [ThreadStatic]
        private static MessageContext currentMessageContext;
        [ThreadStatic]
        private static Dictionary<string, string> outgoingMessageHeadersThreadStatic;

        public void Send(object message)
        {
            Send(message, GetDestinationForMessageType(message.GetType()));
        }

        public void Send(object message, string destination)
        {
            if (string.IsNullOrWhiteSpace(destination))
            {
                throw new ArgumentException("destination required", "destination");
            }
            SendInternal(message, new[] { destination });
        }

        public void Send<T>(Action<T> messageConstructor)
        {
            Send<T>(messageConstructor, GetDestinationForMessageType(typeof(T)));
        }

        public void Send<T>(Action<T> messageConstructor, string destination)
        {
            if (string.IsNullOrWhiteSpace(destination))
            {
                throw new ArgumentException("destination required", "destination");
            }
            SendInternal(MessageMapper.CreateInstance(messageConstructor), new[] { destination });
        }

        public void Publish(object message)
        {
            if (SubscriptionStorage == null)
            {
                throw new InvalidOperationException("Cannot publish - no subscription storage has been configured.");
            }

            var fullTypes = MessageMapper.GetMessageTypeHierarchy(message.GetType());
            var subscribers = SubscriptionStorage
                .GetSubscribersForMessage(fullTypes)
                .ToList();

            if (!subscribers.Any())
            {
                return;
            }

            if (subscribers.Any())
            {
                SendInternal(message, subscribers);
            }
        }

        public void Publish<T>(Action<T> messageConstructor)
        {
            Publish(MessageMapper.CreateInstance(messageConstructor));
        }

        private string GetDestinationForMessageType(Type messageType)
        {
            string destination = null;
            if (staticRoutes.TryGetValue(messageType, out destination))
            {
                return destination;
            }

            if (!messageType.IsInterface)
            {
                var interfaces = messageType.GetInterfaces();
                foreach (var _interface in interfaces)
                {
                    if (staticRoutes.TryGetValue(_interface, out destination))
                    {
                        return destination;
                    }
                }

                var t = MessageMapper.GetMappedTypeFor(messageType);
                if (t != null && t != messageType)
                {
                    return GetDestinationForMessageType(t);
                }
            }

            return destination;
        }

        private void SendInternal(object message, IEnumerable<string> destinations)
        {
            OutgoingHeaders[StandardHeaders.ContentType] = MessageSerializer.ContentType;
            OutgoingHeaders[StandardHeaders.EnclosedMessageTypes] = string.Join(",", MessageMapper.GetEnclosedMessageTypes(message.GetType()).Distinct());
            if (currentMessageContext != null)
            {
                OutgoingHeaders[StandardHeaders.RelatedTo] = currentMessageContext[StandardHeaders.MessageId];
            }
            var transportMessage = new OutgoingTransportMessage(OutgoingHeaders, message, MessageSerializer.Serialize(message));
            this.Transport.Send(transportMessage, destinations);
        }

        public IDictionary<string, string> OutgoingHeaders
        {
            get
            {
                if (outgoingMessageHeadersThreadStatic == null)
                {
                    outgoingMessageHeadersThreadStatic = new Dictionary<string, string>();
                }
                return outgoingMessageHeadersThreadStatic;
            }
        }

        public MessageContext CurrentMessageContext
        {
            get
            {
                return currentMessageContext;
            }
        }

        private void OnMessageAvailable(MessageAvailable value)
        {
            using (var childServiceLocator = this.ServiceLocator.CreateNestedContainer())
            {
                currentMessageContext = new MessageContext(this, value.TransportMessage.MessageId, value.TransportMessage.Headers, childServiceLocator);

                // register a bunch of things we might want to use during the message handling
                childServiceLocator.RegisterSingleton<IBus>(this);
                childServiceLocator.RegisterSingleton(this.MessageHandlers);
                childServiceLocator.RegisterSingleton<IMessageSerializer>(this.MessageSerializer);
                childServiceLocator.RegisterSingleton<IMessageMapper>(this.MessageMapper);
                childServiceLocator.RegisterSingleton<ITransport>(this.Transport);
                childServiceLocator.RegisterSingleton<MessageContext>(currentMessageContext);

                OutgoingHeaders.Clear();
                try
                {
                    object message = null;
                    try
                    {
                        message = DeserializeMessage(value);
                    }
                    catch (Exception e)
                    {
                        logger.Error(e, "Cannot deserialize message.");
                        // The message cannot be deserialized. There is no reason
                        // to retry.
                        throw new CannotDeserializeMessageException(e);
                    }
                    currentMessageContext.CurrentMessage = message;
                    currentMessageContext.SerializedMessageData = value.TransportMessage.SerializedMessageData;
                    
                    var stopwatch = Stopwatch.StartNew();
                    var startTimestamp = DateTime.UtcNow;
                    
                    compiledMessagePipeline(currentMessageContext);
                    
                    var endTimestamp = DateTime.UtcNow;
                    stopwatch.Stop();

                    OnMessageProcessed(new MessageProcessedEventArgs(currentMessageContext, startTimestamp, endTimestamp, stopwatch.Elapsed.TotalMilliseconds));
                }
                finally
                {
                    currentMessageContext = null;
                }
            }
        }

        private object DeserializeMessage(MessageAvailable value)
        {
            Type messageType = null;
            foreach (var typeName in value.TransportMessage.Headers[StandardHeaders.EnclosedMessageTypes].Split(','))
            {
                messageType = this.MessageMapper.GetMappedTypeFor(typeName);
                if (messageType != null)
                {
                    break;
                }
            }
            var message = MessageSerializer.Deserialize(value.TransportMessage.SerializedMessageData, messageType);
            return message;
        }

        public string RequestTimeoutMessage(int secondsToWait, object timeoutMessage)
        {
            OutgoingHeaders[StandardHeaders.TimeoutMessageTimeout] = secondsToWait.ToString();
            OutgoingHeaders[StandardHeaders.ContentType] = MessageSerializer.ContentType;
            OutgoingHeaders[StandardHeaders.EnclosedMessageTypes] = string.Join(",", MessageMapper.GetEnclosedMessageTypes(timeoutMessage.GetType()).Distinct());
            if (currentMessageContext != null)
            {
                OutgoingHeaders[StandardHeaders.RelatedTo] = currentMessageContext[StandardHeaders.MessageId];
            }
            var transportMessage = new OutgoingTransportMessage(OutgoingHeaders, timeoutMessage, MessageSerializer.Serialize(timeoutMessage));
            return this.Transport.RequestTimeoutMessage(secondsToWait, transportMessage);
        }

        public void ClearTimeout(string timeoutCorrelationID)
        {
            this.Transport.ClearTimeout(timeoutCorrelationID);
        }

        public void OnMessageProcessed(MessageProcessedEventArgs args)
        {
            if (MessageProcessed != null)
            {
                var callback = MessageProcessed;
                Task.Factory.StartNew(() => callback(this, args));
            }
        }

        public event EventHandler<MessageProcessedEventArgs> MessageProcessed;

        public void OnPoisonMessageDetected(PoisonMessageDetectedEventArgs args)
        {
            if (PoisonMessageDetected != null)
            {
                var callback = PoisonMessageDetected;
                Task.Factory.StartNew(() => callback(this, args));
            }
        }

        public event EventHandler<PoisonMessageDetectedEventArgs> PoisonMessageDetected;
    }
}
;