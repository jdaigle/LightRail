using System;
using LightRail.ServiceBus.Util;

namespace LightRail.ServiceBus.Logging
{
    public static class LogManager
    {
        static LogManager()
        {
            factory = new ConsoleLogFactory();
        }

        private static ILoggerFactory factory;

        public static void UseFactory<T>()
            where T : ILoggerFactory, new()
        {
            factory = new T();
        }

        public static void UseFactory<T>(T instance)
            where T : ILoggerFactory, new()
        {
            factory = instance;
        }

        /// <summary>
        /// Construct a <see cref="ILog"/> using <typeparamref name="T"/> as the name.
        /// </summary>
        public static ILogger GetLogger<T>()
        {
            return GetLogger(typeof(T));
        }

        /// <summary>
        /// Construct a <see cref="ILog"/> using <paramref name="type"/> as the name.
        /// </summary>
        public static ILogger GetLogger(Type type)
        {
            Guard.ArgumentNotNull(type, nameof(type));
            return factory.GetLogger(type);
        }

        /// <summary>
        /// Construct a <see cref="ILog"/> for <paramref name="name"/>.
        /// </summary>
        public static ILogger GetLogger(string name)
        {
            Guard.ArgumentNotNullOrEmpty(name, nameof(name));
            return factory.GetLogger(name);
        }
    }
}
