using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightRail
{
    /// <summary>
    /// Enables looking up interfaced mapped to generated concrete types
    /// and vice versa.
    /// </summary>
    public interface IMessageMapper : IMessageCreator
    {
        /// <summary>
        /// Initializes the mapper with the given types to be scanned.
        /// </summary>
        /// <param name="types"></param>
        void Initialize(IEnumerable<Type> types);

        /// <summary>
        /// If the given type is an interface, returns the generated concrete type.
        /// If the given type is concerete, returns the interface it was generated from.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        Type GetMappedTypeFor(Type t);

        /// <summary>
        /// Looks up the type mapped for the given name.
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        Type GetMappedTypeFor(string typeName);

        /// <summary>
        /// Determines if the specified type is a type of message based on conventions
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        bool IsMessageType(Type type);
    }
}
