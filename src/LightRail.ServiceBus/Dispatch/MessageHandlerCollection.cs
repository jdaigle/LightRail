using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LightRail.ServiceBus.Logging;
using LightRail.ServiceBus.Reflection;

namespace LightRail.ServiceBus.Dispatch
{
    public sealed class MessageHandlerCollection : IEnumerable<MessageHandlerMethodDispatcher>
    {
        private static ILogger logger = LogManager.GetLogger("LightRail.Dispatch");

        private static readonly Type MessageHandlerGenericInterfaceType = typeof(IMessageHandler<>);

        private readonly IDictionary<Type, List<MessageHandlerMethodDispatcher>> messageTypeToMessageHandlerDictionary 
            = new Dictionary<Type, List<MessageHandlerMethodDispatcher>>();

        public void ScanAssembliesAndMapMessageHandlers(IEnumerable<Assembly> assembliesToScan)
        {
            foreach (var assembly in assembliesToScan)
            {
                ScanAssemblyAndMapMessageHandlers(assembly);
            }
        }

        public void ScanAssemblyAndMapMessageHandlers(Assembly assembly)
        {
            foreach (var method in FindAllMessageHandlerMethods(assembly))
            {
                var messageType = method.GetParameters()[0].ParameterType;
                foreach (var interfaceType in messageType.GetInterfaces())
                {
                    var dispatcher = new MessageHandlerMethodDispatcher(method, interfaceType);
                    AddMessageHandler(dispatcher);
                    logger.Debug("Mapped '{0}' to '{1}'", dispatcher.MessageType, dispatcher.MessageHandlerType);
                }
                while (messageType != typeof(object))
                {
                    var dispatcher = new MessageHandlerMethodDispatcher(method, messageType);
                    AddMessageHandler(dispatcher);
                    logger.Debug("Mapped '{0}' to '{1}'", dispatcher.MessageType, dispatcher.MessageHandlerType);
                    messageType = messageType.BaseType;
                }
            }
        }

        public void AddMessageHandler(MessageHandlerMethodDispatcher messageHandler)
        {
            var messageType = messageHandler.MessageType;
            if (!messageTypeToMessageHandlerDictionary.ContainsKey(messageType))
            {
                messageTypeToMessageHandlerDictionary.Add(messageType, new List<MessageHandlerMethodDispatcher>());
            }
            var messageHandlers = messageTypeToMessageHandlerDictionary[messageType];
            if (!messageHandlers.Any(x => x.Method == messageHandler.Method))
            {
                messageTypeToMessageHandlerDictionary[messageType].Add(messageHandler);
            }
        }

        public IEnumerable<MessageHandlerMethodDispatcher> GetDispatchersForMessageType(Type messageType)
        {
            if (messageTypeToMessageHandlerDictionary.ContainsKey(messageType))
            {
                foreach (var handler in messageTypeToMessageHandlerDictionary[messageType])
                {
                    yield return handler;
                }
            }
        }

        /// <summary>
        /// Scans the assembly and returns a distinct list of all MethodInfo which have the MessageHandlerAttribute
        /// </summary>
        public static IEnumerable<MethodInfo> FindAllMessageHandlerMethods(Assembly assembly)
        {
            foreach (var candidateType in assembly.GetTypesSafely())
            {
                if (!candidateType.IsClass || candidateType.IsAbstract)
                {
                    continue;
                }

                var messageHandlerInterfaces = FindRequestHandlerInterfaces(candidateType);

                if (!messageHandlerInterfaces.Any())
                {
                    continue;
                }

                foreach (var messageHandlerInterface in messageHandlerInterfaces)
                {
                    var messageType = messageHandlerInterface.GetGenericArguments()[0];
                    yield return candidateType.GetInterfaceMap(messageHandlerInterface).TargetMethods[0];
                }
            }
        }

        private static IEnumerable<Type> FindRequestHandlerInterfaces(Type type)
        {
            foreach (var _interface in type.GetInterfaces())
            {
                if (!_interface.IsGenericType)
                {
                    continue;
                }
                var genericType = _interface.GetGenericTypeDefinition();
                if (genericType == MessageHandlerGenericInterfaceType)
                {
                    yield return _interface;
                }
            }
        }

        public IEnumerator<MessageHandlerMethodDispatcher> GetEnumerator()
        {
            foreach (var col in messageTypeToMessageHandlerDictionary.Values)
            {
                foreach (var item in col)
                {
                    yield return item;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var col in messageTypeToMessageHandlerDictionary.Values)
            {
                foreach (var item in col)
                {
                    yield return item;
                }
            }
        }
    }
}
