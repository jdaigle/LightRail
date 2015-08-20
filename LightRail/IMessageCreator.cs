using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightRail
{
    /// <summary>
    /// The abstraction for creating interface-based messages.
    /// </summary>
    public interface IMessageCreator
    {
        T CreateInstance<T>();
        T CreateInstance<T>(Action<T> action);
        object CreateInstance(Type messageType);
    }
}
