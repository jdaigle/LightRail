using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LightRail.Dispatch
{
    public class MessageHandlerMethodDispatcher
    {
        private ActionExecutor _executor;

        public object Execute(params object[] parameters)
        {
            return _executor(parameters);
        }

        public MessageHandlerMethodDispatcher(MethodInfo methodInfo, Type handledMessageType)
        {
            _executor = GetExecutor(methodInfo);
            MethodInfo = methodInfo;
            ParameterTypes = methodInfo.GetParameters().Select(x => x.ParameterType).ToList().AsReadOnly();
            HandledMessageType = handledMessageType;
        }

        private delegate object ActionExecutor(object[] parameters);
        private delegate void VoidActionExecutor(object[] parameters);

        public MethodInfo MethodInfo { get; private set; }
        public IReadOnlyList<Type> ParameterTypes { get; private set; }
        public Type HandledMessageType { get; private set; }

        private static ActionExecutor GetExecutor(MethodInfo methodInfo)
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

            // Call method
            MethodCallExpression methodCall = methodCall = Expression.Call(null, methodInfo, parameters);

            // methodCall is "static method((T0) parameters[0], (T1) parameters[1], ...)"
            // Create function
            if (methodCall.Type == typeof(void))
            {
                Expression<VoidActionExecutor> lambda = Expression.Lambda<VoidActionExecutor>(methodCall, parametersParameter);
                VoidActionExecutor voidExecutor = lambda.Compile();
                return WrapVoidAction(voidExecutor);
            }
            else
            {
                // must coerce methodCall to match ActionExecutor signature
                UnaryExpression castMethodCall = Expression.Convert(methodCall, typeof(object));
                Expression<ActionExecutor> lambda = Expression.Lambda<ActionExecutor>(castMethodCall, parametersParameter);
                return lambda.Compile();
            }
        }

        private static ActionExecutor WrapVoidAction(VoidActionExecutor executor)
        {
            return delegate(object[] parameters)
            {
                executor(parameters);
                return null;
            };
        }
    }
}
