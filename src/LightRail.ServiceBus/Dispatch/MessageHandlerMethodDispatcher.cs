using System;
using System.Linq.Expressions;
using System.Reflection;

namespace LightRail.ServiceBus.Dispatch
{
    public sealed class MessageHandlerMethodDispatcher
    {
        public delegate void ActionExecutor(object messageHandler, object message);

        public MessageHandlerMethodDispatcher(MethodInfo methodInfo)
            :this(methodInfo, methodInfo.GetParameters()[0].ParameterType)
        {
        }

        public MessageHandlerMethodDispatcher(MethodInfo methodInfo, Type messageType)
        {
            var methodParameterType = methodInfo.GetParameters()[0].ParameterType;
            if (messageType != methodParameterType
                && !messageType.IsInstanceOfType(methodParameterType))
            {
                throw new ArgumentException(nameof(messageType), $"{messageType} must be instance of {methodParameterType}");
            }
            Method = methodInfo;
            MessageHandlerType = methodInfo.DeclaringType;
            MessageType = messageType;
            actionExecutorExpression = GetExecutor(methodInfo, messageType);
            _compiledActionExecutor = new Lazy<ActionExecutor>(() => actionExecutorExpression.Compile());
        }

        public MethodInfo Method { get; }
        public Type MessageType { get; }
        public Type MessageHandlerType { get; }

        private readonly Expression<ActionExecutor> actionExecutorExpression;
        private readonly Lazy<ActionExecutor> _compiledActionExecutor;

        /// <summary>
        /// Executes a specific lambda represented a message handler
        /// with the specific closure object.
        /// </summary>
        public ActionExecutor Execute
        {
            get { return _compiledActionExecutor.Value; }
        }

        /// <summary>
        /// Generates a lambda expression which when compiled will execute
        /// a message handler lambda function, passing in the message from the
        /// requestContext.
        /// </summary>
        private static Expression<ActionExecutor> GetExecutor(MethodInfo methodInfo, Type messageType)
        {
            ParameterExpression messageHandlerParameter = Expression.Parameter(typeof(object), "messageHandler");
            UnaryExpression messageHandlerParameterCastToDeclaringType = Expression.Convert(messageHandlerParameter, methodInfo.DeclaringType);

            ParameterExpression messageParameter = Expression.Parameter(typeof(object), "message");
            UnaryExpression messageParameterCastToMessageType = Expression.Convert(messageParameter, messageType);

            // ((TMessageHandler)messageHandler).Execute((TMessage)message)
            MethodCallExpression methodCall = Expression.Call(messageHandlerParameterCastToDeclaringType, methodInfo, messageParameterCastToMessageType);

            return Expression.Lambda<ActionExecutor>(methodCall, messageHandlerParameter, messageParameter);
        }
    }
}
