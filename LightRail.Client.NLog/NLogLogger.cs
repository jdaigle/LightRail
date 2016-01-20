using System;
using LightRail.Client.Logging;
using ILogger = NLog.ILogger;

namespace LightRail.Client.NLog
{
    public class NLogLogger : Logging.ILogger
    {
        private readonly ILogger logger;

        public NLogLogger(ILogger log)
        {
            this.logger = log;
        }

        public bool IsLogEventEnabled(LoggingEventType eventType)
        {
            switch (eventType)
            {
                case LoggingEventType.Debug:
                    return logger.IsDebugEnabled;
                case LoggingEventType.Info:
                    return logger.IsInfoEnabled;
                case LoggingEventType.Warn:
                    return logger.IsWarnEnabled;
                case LoggingEventType.Error:
                    return logger.IsErrorEnabled;
                case LoggingEventType.Fatal:
                    return logger.IsFatalEnabled;
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
                        logger.Debug(entry.Message);
                    }
                    else
                    {
                        logger.Debug(entry.Exception, entry.Message);
                    }
                    break;
                case LoggingEventType.Info:
                    if (entry.Exception == null)
                    {
                        logger.Info(entry.Message);
                    }
                    else
                    {
                        logger.Info(entry.Exception, entry.Message);
                    }
                    break;
                case LoggingEventType.Warn:
                    if (entry.Exception == null)
                    {
                        logger.Warn(entry.Message);
                    }
                    else
                    {
                        logger.Warn(entry.Exception, entry.Message);
                    }
                    break;
                case LoggingEventType.Error:
                    if (entry.Exception == null)
                    {
                        logger.Error(entry.Message);
                    }
                    else
                    {
                        logger.Error(entry.Exception, entry.Message);
                    }
                    break;
                case LoggingEventType.Fatal:
                    if (entry.Exception == null)
                    {
                        logger.Fatal(entry.Message);
                    }
                    else
                    {
                        logger.Fatal(entry.Exception, entry.Message);
                    }
                    break;
            }
        }
    }
}
