using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace LightRail
{
    public class Log4NetLogManager : ILogManager
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
                        var logger = new Log4NetLogger(LogManager.GetLogger(name));
                        loggerCache[name] = logger;
                    }
                }
            }
            return loggerCache[name];
        }
    }
}
