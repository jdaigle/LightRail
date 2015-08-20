using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightRail
{
    public class LightRailClient : IObserver<MessageAvailable>
    {
        public static LightRailClient Create(LightRailConfiguration config)
        {
            var client = new LightRailClient();

            client.Transport = config.TransportConstructor();
            client.Transport.Subscribe(client);
            client.MessageSerializer = config.MessageSerializerConstructor();
            client.MessageHandlers = config.MessageHandlerCollection;

            return client;
        }

        private LightRailClient()
        {

        }

        public LightRailClient Start()
        {
            this.Transport.Start();
            return this;
        }

        public ITransport Transport { get; private set; }
        public IMessageSerializer MessageSerializer { get; private set; }
        public MessageHandlerCollection MessageHandlers { get; private set; }

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
            throw new NotImplementedException();
        }

        private void SendInternal(object message, IEnumerable<string> destinations)
        {
            var headers = new Dictionary<string, string>(); // copy headers before sending
            headers[Headers.ContentType] = MessageSerializer.ContentType;
            headers[Headers.EnclosedMessageTypes] = string.Join(",", GetEnclosedMessageTypes(message.GetType()).Distinct());
            var transportMessage = new OutgoingTransportMessage(headers, MessageSerializer.Serialize(message));
            this.Transport.Send(transportMessage, destinations);
        }

        private static HashSet<string> systemAssemblyNames = new HashSet<string>
        {
            "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
            "System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
        };

        private static IEnumerable<string> GetEnclosedMessageTypes(Type type)
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
            var message = MessageSerializer.Deserialize(value.TransportMessage.SerializedMessageData, value.TransportMessage.Headers[Headers.EnclosedMessageTypes].Split(',')[0] + ", LightRail.SampleServer");
            foreach (var handler in MessageHandlers.GetOrderedDispatchInfoFor(message.GetType()))
            {
                handler.Invoke(message);
            }
        }
    }
}
