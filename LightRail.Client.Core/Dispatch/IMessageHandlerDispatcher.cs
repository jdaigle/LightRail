using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LightRail.Client.Dispatch
{
    public interface IMessageHandlerDispatcher
    {
        /// <summary>
        /// Executes the configured message handler. If the message handler
        /// is an instance method, then the first parameter should the instance.
        /// </summary>
        /// <param name="parameters"></param>
        Task Execute(params object[] parameters);

        bool IsInstanceMethod { get; }
        IReadOnlyList<Type> ParameterTypes { get; }
        Type HandledMessageType { get; }
    }
}
