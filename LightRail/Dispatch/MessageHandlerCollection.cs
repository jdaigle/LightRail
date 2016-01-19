using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using LightRail.Dispatch;
using LightRail.Logging;
using LightRail.Reflection;

namespace LightRail.Dispatch
{
    public class MessageHandlerCollection
    {
        public MessageHandlerCollection(MessageTypeConventions messageTypeConventions)
        {
            this.messageTypeConventions = messageTypeConventions;
        }

        private static ILogger logger = LogManager.GetLogger("LightRail.Dispatch");

        private readonly MessageTypeConventions messageTypeConventions;
        private bool isInit = false;
        private readonly IDictionary<Type, List<MessageHandlerMethodDispatcher>> messageTypeToMessageHandlerDictionary = new Dictionary<Type, List<MessageHandlerMethodDispatcher>>();
        private static readonly List<MessageHandlerMethodDispatcher> emptyMessageHandlerCollection = new List<MessageHandlerMethodDispatcher>();

        private void AssertNotInit()
        {
            if (isInit)
            {
                throw new InvalidOperationException("Collection already initialized");
            }
        }

        private void AssertInit()
        {
            if (!isInit)
            {
                throw new InvalidOperationException("Collection has not been initialized");
            }
        }

        public void ScanAssembliesAndInitialize(IEnumerable<Assembly> assembliesToScan)
        {
            if (isInit)
            {
                return;
            }

            foreach (var method in FindAllMessageHandlerMethods(assembliesToScan))
            {
                var messageType = FindMessageTypeFromMethodParameters(method);
                var dispatcher = new MessageHandlerMethodDispatcher(method, messageType);
                AddMessageHandler(dispatcher);
                logger.Debug("Associated '{0}' message with static method '{1}'", messageType, method);
            }

            this.isInit = true;
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
            AssertInit();
            foreach (var handledMessageType in messageTypeToMessageHandlerDictionary.Keys)
            {
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
                    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
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
            var parameter = method.GetParameters().SingleOrDefault(p => messageTypeConventions.IsMessageType(p.ParameterType));
            if (parameter == null)
            {
                throw new InvalidOperationException(string.Format("Method {0} is marked with [MessageHandlerAttribute] does not have a single parameter which matches a message type convention", method));
            }
            return parameter.ParameterType;
        }
    }
}
