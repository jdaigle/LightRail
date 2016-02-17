using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LightRail.ServiceBus.Dispatch
{
    public interface IMessageHandlerDispatcher
    {
        /// <summary>
        /// Executes the configured message handler. If the message handler
        /// is an instance method, then the first parameter should the instance.
        /// </summary>
        /// <param name="parameters"></param>
        void Execute(params object[] parameters);

        bool IsInstanceMethod { get; }
        IReadOnlyList<Type> ParameterTypes { get; }
        Type HandledMessageType { get; }
    }
}
