using System;
using System.Collections.Generic;
using LightRail.ServiceBus.Logging;
using log4net.Config;
using LogManager = log4net.LogManager;

namespace LightRail.ServiceBus.log4net
{
    public class Log4NetLoggerFactory : ILoggerFactory
    {
        static Log4NetLoggerFactory()
        {
            XmlConfigurator.Configure();
        }

        private readonly static object loggerCacheLock = new object();
        private readonly static Dictionary<string, ILogger> loggerCache = new Dictionary<string, ILogger>(StringComparer.InvariantCultureIgnoreCase);

        public ILogger GetLogger(string name)
        {
            if (!loggerCache.ContainsKey(name))
            {
                lock (loggerCacheLock)
                {
                    if (!loggerCache.ContainsKey(name))
                    {
                        var logger = new Log4NetLogger(LogManager.GetLogger(name));
                        loggerCache[name] = logger;
                    }
                }
            }
            return loggerCache[name];
        }

        public ILogger GetLogger(Type type)
        {
            return GetLogger(type.FullName);
        }
    }
}
