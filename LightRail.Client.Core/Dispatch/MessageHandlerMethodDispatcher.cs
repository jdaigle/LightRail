using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace LightRail.Client.Dispatch
{
    public class MessageHandlerMethodDispatcher : IMessageHandlerDispatcher
    {
        private StaticActionExecutor _executor;

        public void Execute(params object[] parameters)
        {
            _executor(parameters);
        }

        public static MessageHandlerMethodDispatcher FromDelegate<T1>(Action<T1, MessageContext> method)
        {
            return new MessageHandlerMethodDispatcher((Delegate)method, typeof(T1));
        }

        public static MessageHandlerMethodDispatcher FromDelegate<T1>(Action<T1> method)
        {
            return new MessageHandlerMethodDispatcher((Delegate)method, typeof(T1));
        }

        public MessageHandlerMethodDispatcher(Delegate method, Type handledMessageType)
        {
            var methodInfo = method.Method;
            MethodInfo = methodInfo;
            Target = method.Target;
            ParameterTypes = methodInfo.GetParameters().Select(x => x.ParameterType).ToList().AsReadOnly();
            HandledMessageType = handledMessageType;
            IsInstanceMethod = !methodInfo.IsStatic;
            TargetType = methodInfo.DeclaringType;
            _executor = GetExecutor(MethodInfo, Target);
        }

        public MessageHandlerMethodDispatcher(MethodInfo methodInfo, Type handledMessageType)
        {
            MethodInfo = methodInfo;
            ParameterTypes = methodInfo.GetParameters().Select(x => x.ParameterType).ToList().AsReadOnly();
            HandledMessageType = handledMessageType;
            IsInstanceMethod = !methodInfo.IsStatic;
            TargetType = methodInfo.DeclaringType;
            _executor = GetExecutor(methodInfo, null);
        }

        private delegate void StaticActionExecutor(object[] parameters);
        private delegate void InstanceActionExecutor(object instance, object[] parameters);

        public bool IsInstanceMethod { get; }
        public MethodInfo MethodInfo { get; }
        public IReadOnlyList<Type> ParameterTypes { get; }
        public Type HandledMessageType { get; }
        public object Target { get; }
        public Type TargetType { get; }
        public bool RequiresTarget
        {
            get
            {
                return IsInstanceMethod && Target == null;
            }
        }

        private static StaticActionExecutor GetExecutor(MethodInfo methodInfo, object instanceTarget)
        {
            // Parameters to executor
            ParameterExpression parametersParameter = Expression.Parameter(typeof(object[]), "parameters");

            // Build parameter list
            List<Expression> parameters = new List<Expression>();
            ParameterInfo[] paramInfos = methodInfo.GetParameters();
            for (int i = 0; i < paramInfos.Length; i++)
            {
                ParameterInfo paramInfo = paramInfos[i];
                BinaryExpression valueObj = Expression.ArrayIndex(parametersParameter, Expression.Constant(i));
                UnaryExpression valueCast = Expression.Convert(valueObj, paramInfo.ParameterType);

                // valueCast is "(Ti) parameters[i]"
                parameters.Add(valueCast);
            }

            if (methodInfo.IsStatic)
            {
                MethodCallExpression methodCall = Expression.Call(null, methodInfo, parameters);
                AssertMethodCallIsVoid(methodCall);

                // methodCall is "static method((T0) parameters[0], (T1) parameters[1], ...)"
                Expression<StaticActionExecutor> lambda = Expression.Lambda<StaticActionExecutor>(methodCall, parametersParameter);
                return lambda.Compile();
            }
            else
            {
                ParameterExpression instanceParameter = Expression.Parameter(typeof(object), "instance");
                UnaryExpression instanceCast = Expression.Convert(instanceParameter, methodInfo.ReflectedType);

                MethodCallExpression methodCall = Expression.Call(instanceCast, methodInfo, parameters);
                AssertMethodCallIsVoid(methodCall);

                // methodCall is "[instance] method((T0) parameters[0], (T1) parameters[1], ...)"
                Expression<InstanceActionExecutor> lambda = Expression.Lambda<InstanceActionExecutor>(methodCall, instanceParameter, parametersParameter);
                var instanceAction = lambda.Compile();
                if (instanceTarget != null)
                {
                    return WrapAsStaticAction(instanceAction, instanceTarget);
                }
                return WrapAsStaticAction(instanceAction);
            }
        }

        private static void AssertMethodCallIsVoid(MethodCallExpression methodCall)
        {
            if (methodCall.Type != typeof(void))
            {
                throw new Exception("Message handler methods must have a void return type.");
            }
        }

        private static StaticActionExecutor WrapAsStaticAction(InstanceActionExecutor action, object target)
        {
            return delegate (object[] parameters)
            {
                action(target, parameters);
            };
        }

        private static StaticActionExecutor WrapAsStaticAction(InstanceActionExecutor action)
        {
            return delegate (object[] parameters)
            {
                if (parameters.Length < 1)
                {
                    throw new InvalidOperationException("Cannot execute wrapped instance method without a singe parameter (the first parameter should be the instance).");
                }
                var _p = new object[parameters.Length - 1];
                for (int i = 1; i < parameters.Length; i++)
                {
                    _p[i - 1] = parameters[i];
                }
                action(parameters[0], _p);
            };
        }
    }
}
