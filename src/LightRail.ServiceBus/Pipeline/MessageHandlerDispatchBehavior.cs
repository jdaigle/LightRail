﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightRail.ServiceBus.Dispatch;

namespace LightRail.ServiceBus.Pipeline
{
    public class MessageHandlerDispatchBehavior : PipelinedBehavior
    {
        protected override void Invoke(MessageContext context, Action next)
        {
            // GetDispatchersForMessageType simply returns a enumerable of all
            // handlers which can accept the message as a parameter in which ever order they exist internally
            foreach (var dispatcher in context.ServiceLocator.Resolve<MessageHandlerCollection>().GetDispatchersForMessageType(context.CurrentMessage.GetType()))
            {
                dispatcher.Execute(ResolveParameters(dispatcher, context.CurrentMessage, context.ServiceLocator));
            }
            next(); // it's best practice to call next, even though this is likely the most inner behavior to execute
        }

        private static object[] ResolveParameters(MessageHandlerMethodDispatcher handler, object message, IServiceLocator serviceLocator)
        {
            var requiresTargetParameter = handler.RequiresTarget;
            var parameters = new object[handler.ParameterTypes.Count + (requiresTargetParameter ? 1 : 0)];
            if (requiresTargetParameter)
            {
                parameters[0] = serviceLocator.Resolve(handler.TargetType) ?? Activator.CreateInstance(handler.TargetType);
                // TODO performance.cache a lambda expression to construct instead of Activator.CreateInstance
            }
            var pStart = (requiresTargetParameter ? 1 : 0);
            for (int i = 0; i < handler.ParameterTypes.Count; i++)
            {
                var parameterType = handler.ParameterTypes[i];
                if (parameterType.IsAssignableFrom(message.GetType()))
                {
                    parameters[pStart + i] = message; // shortcut for message, it's a known type
                    continue;
                }
                parameters[pStart + i] = serviceLocator.Resolve(parameterType);
            }
            return parameters;
        }
    }
}
