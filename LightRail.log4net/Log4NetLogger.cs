using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightRail.Logging;
using log4net;

namespace LightRail
{
    public class Log4NetLogger : ILogger
    {
        private readonly ILog log;

        public Log4NetLogger(ILog log)
        {
            this.log = log;
        }

        public void Log(LogEntry entry)
        {
            switch (entry.Severity)
            {
                case LoggingEventType.Debug:
                    if (entry.Exception == null)
                    {
                        log.Debug(entry.Message);
                    }
                    else
                    {
                        log.Debug(entry.Message, entry.Exception);
                    }
                    break;
                case LoggingEventType.Info:
                    if (entry.Exception == null)
                    {
                        log.Info(entry.Message);
                    }
                    else
                    {
                        log.Info(entry.Message, entry.Exception);
                    }
                    break;
                case LoggingEventType.Warn:
                    if (entry.Exception == null)
                    {
                        log.Warn(entry.Message);
                    }
                    else
                    {
                        log.Warn(entry.Message, entry.Exception);
                    }
                    break;
                case LoggingEventType.Error:
                    if (entry.Exception == null)
                    {
                        log.Error(entry.Message);
                    }
                    else
                    {
                        log.Error(entry.Message, entry.Exception);
                    }
                    break;
                case LoggingEventType.Fatal:
                    if (entry.Exception == null)
                    {
                        log.Fatal(entry.Message);
                    }
                    else
                    {
                        log.Fatal(entry.Message, entry.Exception);
                    }
                    break;
            }
        }
    }
}
