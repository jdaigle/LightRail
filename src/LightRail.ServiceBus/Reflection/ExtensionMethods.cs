/* This code is derived from NServiceBus 2.0 
 * https://github.com/Particular/NServiceBus/blob/2.0/src/utils/NServiceBus.Utils.Reflection/ExtensionMethods.cs
 * 
 * Which is licensed under Apache Licence, Version 2.0
 * http://www.apache.org/licenses/LICENSE-2.0
 */

using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace LightRail.ServiceBus.Reflection
{
    /// <summary>
    /// Contains extension methods
    /// </summary>
    public static class ExtensionMethods
    {
        public static IEnumerable<Type> GetTypesSafely(this Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(x => x != null);
            }
        }

        public static IEnumerable<Type> GetTypes(this Assembly assembly, Func<Type, bool> matching)
        {
            return GetTypesSafely(assembly).Where(matching);
        }

        /// <summary>
        /// Useful for finding if a type is (for example) IMessageHandler{T} where T : IMessage.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="openGenericType"></param>
        /// <param name="genericArg"></param>
        /// <returns></returns>
        public static bool IsGenericallyEquivalent(this Type type, Type openGenericType, Type genericArg)
        {
            bool result = false;
            LoopAndAct(type, openGenericType, genericArg, t => result = true);

            return result;
        }

        /// <summary>
        /// Returns the enclosed generic type given that the type is GenericallyEquivalent.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="openGenericType"></param>
        /// <param name="genericArg"></param>
        /// <returns></returns>
        public static Type GetGenericallyContainedType(this Type type, Type openGenericType, Type genericArg)
        {
            Type result = null;
            LoopAndAct(type, openGenericType, genericArg, t => result = t);

            return result;
        }

        private static void LoopAndAct(Type type, Type openGenericType, Type genericArg, Action<Type> act)
        {
            foreach (var i in type.GetInterfaces())
            {
                var args = i.GetGenericArguments();

                if (args.Length == 1)
                    if (genericArg.IsAssignableFrom(args[0]))
                        if (openGenericType.MakeGenericType(args[0]) == i)
                        {
                            act(args[0]);
                            break;
                        }
            }
        }

        /// <summary>
        /// Returns true if the type can be serialized as is.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsSimpleType(this Type type)
        {
            return (type == typeof(string) ||
                    type.IsPrimitive ||
                    type == typeof(decimal) ||
                    type == typeof(Guid) ||
                    type == typeof(DateTime) ||
                    type == typeof(TimeSpan) ||
                    type == typeof(DateTimeOffset) ||
                    type.IsEnum);
        }

        /// <summary>
        /// Takes the name of the given type and makes it friendly for serialization
        /// by removing problematic characters.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static string SerializationFriendlyName(this Type t)
        {
            lock (TypeToNameLookup)
                if (TypeToNameLookup.ContainsKey(t))
                    return TypeToNameLookup[t];

            var args = t.GetGenericArguments();
            if (args != null)
            {
                int index = t.Name.IndexOf('`');
                if (index >= 0)
                {
                    string result = t.Name.Substring(0, index) + "Of";
                    for (int i = 0; i < args.Length; i++)
                    {
                        result += args[i].SerializationFriendlyName();
                        if (i != args.Length - 1)
                            result += "And";
                    }

                    if (args.Length == 2)
                        if (typeof(KeyValuePair<,>).MakeGenericType(args) == t)
                            result = "LightRail.ServiceBus.Serialization." + result;

                    lock (TypeToNameLookup)
                        TypeToNameLookup[t] = result;

                    return result;
                }
            }

            lock (TypeToNameLookup)
                TypeToNameLookup[t] = t.Name;

            return t.Name;
        }

        private static readonly IDictionary<Type, string> TypeToNameLookup = new Dictionary<Type, string>();
    }
}
