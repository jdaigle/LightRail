using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightRail.Reflection;

namespace LightRail
{
    public class LightRailClient : IObserver<MessageAvailable>
    {
        public static LightRailClient Create(LightRailConfiguration config)
        {
            var client = new LightRailClient();

            client.logger = config.LogManager.GetLogger("LightRail");
            client.Transport = config.TransportConstructor();
            client.Transport.Subscribe(client);
            client.MessageSerializer = config.MessageSerializerConstructor();
            client.MessageHandlers = config.MessageHandlerCollection;
            var messageTypes = config.MessageTypeConventions.ScanAssembliesForMessageTypes(config.AssembliesToScan);
            client.MessageMapper = new MessageMapper(config.MessageTypeConventions);
            client.MessageMapper.Initialize(messageTypes);

            return client;
        }

        private LightRailClient()
        {

        }

        public LightRailClient Start()
        {
            logger.Debug("Starting LightRailClient");
            this.Transport.Start();
            logger.Debug("LightRailClient Started");
            return this;
        }

        private ILogger logger;

        public ITransport Transport { get; private set; }
        public IMessageSerializer MessageSerializer { get; private set; }
        public MessageHandlerCollection MessageHandlers { get; private set; }
        public IMessageMapper MessageMapper { get; private set; }

        public void Send(object message)
        {
            throw new NotImplementedException();
        }

        public void Send(object message, string destination)
        {
            SendInternal(message, new[] { destination });
        }

        public void Send<T>(Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        public void Send<T>(Action<T> messageConstructor, string destination)
        {
            SendInternal(MessageMapper.CreateInstance(messageConstructor), new[] { destination });
        }

        private void SendInternal(object message, IEnumerable<string> destinations)
        {
            var headers = new Dictionary<string, string>(); // copy headers before sending
            headers[Headers.ContentType] = MessageSerializer.ContentType;
            headers[Headers.EnclosedMessageTypes] = string.Join(",", GetEnclosedMessageTypes(message.GetType()).Distinct());
            var transportMessage = new OutgoingTransportMessage(headers, message, MessageSerializer.Serialize(message));
            this.Transport.Send(transportMessage, destinations);
        }

        private static HashSet<string> systemAssemblyNames = new HashSet<string>
        {
            "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
            "System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
        };

        private IEnumerable<string> GetEnclosedMessageTypes(Type type)
        {
            if (systemAssemblyNames.Contains(type.Assembly.FullName) || type == typeof(object))
            {
                yield break;
            }
            if (type.IsClass)
            {
                yield return type.FullName;
                foreach (var _interface in type.GetInterfaces())
                {
                    foreach (var name in GetEnclosedMessageTypes(_interface))
                    {
                        yield return name;
                    }
                }
                foreach (var name in GetEnclosedMessageTypes(type.BaseType))
                {
                    yield return name;
                }
            }
            if (type.IsInterface)
            {
                yield return type.FullName;
                foreach (var _interface in type.GetInterfaces())
                {
                    foreach (var name in GetEnclosedMessageTypes(_interface))
                    {
                        yield return name;
                    }
                }
            }
        }

        void IObserver<MessageAvailable>.OnCompleted()
        {
            throw new NotImplementedException();
        }

        void IObserver<MessageAvailable>.OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        void IObserver<MessageAvailable>.OnNext(MessageAvailable value)
        {
            Type messageType = null;
            foreach (var typeName in value.TransportMessage.Headers[Headers.EnclosedMessageTypes].Split(','))
            {
                messageType = this.MessageMapper.GetMappedTypeFor(typeName);
                if (messageType != null)
                {
                    break;
                }
            }
            var message = MessageSerializer.Deserialize(value.TransportMessage.SerializedMessageData, messageType);
            foreach (var handler in MessageHandlers.GetOrderedDispatchInfoFor(message.GetType()))
            {
                handler.Invoke(message);
            }
        }
    }
}
