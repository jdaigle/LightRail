using System;
using System.Collections.Generic;

namespace LightRail.ServiceBus
{
    /// <summary>
    /// Enables looking up interfaced mapped to generated concrete types
    /// and vice versa.
    /// </summary>
    public interface IMessageMapper
    {
        /// <summary>
        /// If the given type is an interface, finds its generated concrete
        /// implementation, instantiates it, and returns the result.
        /// </summary>
        T CreateInstance<T>();
        /// <summary>
        /// If the given type is an interface, finds its generated concrete
        /// implementation, instantiates it, and returns the result.
        /// </summary>
        object CreateInstance(Type messageType);

        /// <summary>
        /// Initializes the mapper: scans the given types generating concrete classes for interfaces.
        /// </summary>
        void Initialize(IEnumerable<Type> types);

        /// <summary>
        /// If the given type is an interface, returns the generated concrete type.
        /// If the given type is concerete, returns the interface it was generated from.
        /// </summary>
        Type GetMappedTypeFor(Type t);

        /// <summary>
        /// Looks up the type mapped for the given name.
        /// </summary>
        Type GetMappedTypeFor(string typeName);

        List<Type> GetMessageTypeHierarchy(Type type);

        IEnumerable<string> GetEnclosedMessageTypes(Type type);
    }
}
