using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightRail.Logging
{
    // Sourced from: http://stackoverflow.com/a/5646876/507
    public static class LoggerExtensions
    {
        public static void Debug(this ILogger logger, Exception exception, string message, params object[] formatArgs)
        {
            logger.Log(new LogEntry(LoggingEventType.Debug, string.Format(message, formatArgs), exception));
        }
        public static void Debug(this ILogger logger, string message, params object[] formatArgs)
        {
            logger.Log(new LogEntry(LoggingEventType.Debug, string.Format(message, formatArgs), null));
        }

        public static void Info(this ILogger logger, Exception exception, string message, params object[] formatArgs)
        {
            logger.Log(new LogEntry(LoggingEventType.Info, string.Format(message, formatArgs), exception));
        }
        public static void Info(this ILogger logger, string message, params object[] formatArgs)
        {
            logger.Log(new LogEntry(LoggingEventType.Info, string.Format(message, formatArgs), null));
        }

        public static void Warn(this ILogger logger, Exception exception, string message, params object[] formatArgs)
        {
            logger.Log(new LogEntry(LoggingEventType.Warn, string.Format(message, formatArgs), exception));
        }
        public static void Warn(this ILogger logger, string message, params object[] formatArgs)
        {
            logger.Log(new LogEntry(LoggingEventType.Warn, string.Format(message, formatArgs), null));
        }

        public static void Error(this ILogger logger, Exception exception, string message, params object[] formatArgs)
        {
            logger.Log(new LogEntry(LoggingEventType.Error, string.Format(message, formatArgs), exception));
        }
        public static void Error(this ILogger logger, string message, params object[] formatArgs)
        {
            logger.Log(new LogEntry(LoggingEventType.Error, string.Format(message, formatArgs), null));
        }

        public static void Fatal(this ILogger logger, Exception exception, string message, params object[] formatArgs)
        {
            logger.Log(new LogEntry(LoggingEventType.Fatal, string.Format(message, formatArgs), exception));
        }
        public static void Fatal(this ILogger logger, string message, params object[] formatArgs)
        {
            logger.Log(new LogEntry(LoggingEventType.Fatal, string.Format(message, formatArgs), null));
        }
    }
}
