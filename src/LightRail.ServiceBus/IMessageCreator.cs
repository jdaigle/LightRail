using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightRail.ServiceBus
{
    public interface IMessageCreator
    {
        /// <summary>
        /// If the given type is an interface, finds its generated concrete
        /// implementation, instantiates it, and returns the instance.
        /// </summary>
        T CreateInstance<T>();
        /// <summary>
        /// If the given type is an interface, finds its generated concrete
        /// implementation, instantiates it, executes the inlined action, and returns the instance.
        /// </summary>
        T CreateInstance<T>(Action<T> action);
    }
}
