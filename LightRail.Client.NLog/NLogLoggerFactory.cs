using System;
using System.Collections.Generic;
using LightRail.Client.Logging;
using LogManager = NLog.LogManager;

namespace LightRail.Client.NLog
{
    public class NLogLoggerFactory : ILoggerFactory
    {
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
                        var logger = new NLogLogger(LogManager.GetLogger(name));
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
