using LightRail.Logging;

namespace LightRail
{
    public class NLogLogger : Logging.ILogger
    {
        private readonly NLog.ILogger logger;

        public NLogLogger(NLog.ILogger log)
        {
            this.logger = log;
        }

        public void Log(LogEntry entry)
        {
            switch (entry.Severity)
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
