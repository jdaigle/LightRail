using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LightRail.Logging;
using log4net;

namespace LightRail
{
    public class Log4NetLoggerFactory : ILoggerFactory
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
                        var logger = new Log4NetLogger(log4net.LogManager.GetLogger(name));
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
