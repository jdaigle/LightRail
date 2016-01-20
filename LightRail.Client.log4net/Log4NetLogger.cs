using LightRail.Client.Logging;
using log4net;

namespace LightRail.Client.log4net
{
    public class Log4NetLogger : ILogger
    {
        private readonly ILog log;

        public Log4NetLogger(ILog log)
        {
            this.log = log;
        }

        public bool IsLogEventEnabled(LoggingEventType eventType)
        {
            switch (eventType)
            {
                case LoggingEventType.Debug:
                    return log.IsDebugEnabled;
                case LoggingEventType.Info:
                    return log.IsInfoEnabled;
                case LoggingEventType.Warn:
                    return log.IsWarnEnabled;
                case LoggingEventType.Error:
                    return log.IsErrorEnabled;
                case LoggingEventType.Fatal:
                    return log.IsFatalEnabled;
                default:
                    return false;
            }
        }

        public void Log(LogEntry entry)
        {
            switch (entry.EventType)
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
