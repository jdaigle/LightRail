using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LightRail.Client.Logging;
using LightRail.Client.Reflection;

namespace LightRail.Client.Dispatch
{
    public class MessageHandlerCollection
    {
        private static ILogger logger = LogManager.GetLogger("LightRail.Dispatch");

        private readonly IDictionary<Type, List<MessageHandlerMethodDispatcher>> messageTypeToMessageHandlerDictionary = new Dictionary<Type, List<MessageHandlerMethodDispatcher>>();

        public void ScanAssembliesAndMapMessageHandlers(IEnumerable<Assembly> assembliesToScan)
        {
            foreach (var method in FindAllMessageHandlerMethods(assembliesToScan))
            {
                var messageType = FindMessageTypeFromMethodParameters(method);
                AddMessageHandler(method, messageType);
                logger.Debug("Associated '{0}' message with method '{1}'", messageType, method);
            }
        }

        public void AddMessageHandler(MethodInfo method, Type messageType)
        {
            AddMessageHandler(new MessageHandlerMethodDispatcher(method, messageType));
        }

        public void AddMessageHandler(MessageHandlerMethodDispatcher messageHandler)
        {
            var messageType = messageHandler.HandledMessageType;
            if (!messageTypeToMessageHandlerDictionary.ContainsKey(messageType))
            {
                messageTypeToMessageHandlerDictionary.Add(messageType, new List<MessageHandlerMethodDispatcher>());
            }
            messageTypeToMessageHandlerDictionary[messageType].Add(messageHandler);
        }

        public IEnumerable<MessageHandlerMethodDispatcher> GetDispatchersForMessageType(Type messageType)
        {
            foreach (var handledMessageType in messageTypeToMessageHandlerDictionary.Keys)
            {
                // TODO: faster lookup? Maybe cache this logic on first lookup?
                if (handledMessageType.IsAssignableFrom(messageType))
                {
                    foreach (var handler in messageTypeToMessageHandlerDictionary[handledMessageType])
                    {
                        yield return handler;
                    }
                }
            }
        }

        /// <summary>
        /// Scans all assemblies and returns a distinct list of all MethodInfo which have the MessageHandlerAttribute
        /// </summary>
        public static IEnumerable<MethodInfo> FindAllMessageHandlerMethods(IEnumerable<Assembly> assemblies)
        {
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypesSafely())
                {
                    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                    {
                        if (IsMessageHandlerMethod(method))
                        {
                            yield return method;
                        }
                    }
                }
            }
        }

        private static bool IsMessageHandlerMethod(MethodInfo method)
        {
            return method.GetCustomAttribute<MessageHandlerAttribute>() != null;
        }

        private Type FindMessageTypeFromMethodParameters(MethodInfo method)
        {
            var parameter = method.GetParameters().FirstOrDefault();
            if (parameter == null)
            {
                throw new InvalidOperationException(string.Format("Method {0} is marked with [MessageHandlerAttribute] does not have any parameters. The first parameter is the handled message type.", method));
            }
            return parameter.ParameterType;
        }
    }
}
