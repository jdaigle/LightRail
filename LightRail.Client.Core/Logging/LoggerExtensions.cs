using System;

namespace LightRail.Client.Logging
{
    // Sourced from: http://stackoverflow.com/a/5646876/507
    public static class LoggerExtensions
    {
        public static void Log(this ILogger logger, LoggingEventType eventType, Exception exception, string message, params object[] formatArgs)
        {
            if (logger.IsLogEventEnabled(eventType))
            {
                logger.Log(new LogEntry(eventType, string.Format(message, formatArgs), exception));
            }
        }

        public static void Debug(this ILogger logger, Exception exception, string message, params object[] formatArgs)
        {
            logger.Log(LoggingEventType.Debug, exception, message, formatArgs);
        }
        public static void Debug(this ILogger logger, string message, params object[] formatArgs)
        {
            logger.Log(LoggingEventType.Debug, null, message, formatArgs);
        }

        public static void Info(this ILogger logger, Exception exception, string message, params object[] formatArgs)
        {
            logger.Log(LoggingEventType.Info, exception, message, formatArgs);
        }
        public static void Info(this ILogger logger, string message, params object[] formatArgs)
        {
            logger.Log(LoggingEventType.Info, null, message, formatArgs);
        }

        public static void Warn(this ILogger logger, Exception exception, string message, params object[] formatArgs)
        {
            logger.Log(LoggingEventType.Warn, exception, message, formatArgs);
        }
        public static void Warn(this ILogger logger, string message, params object[] formatArgs)
        {
            logger.Log(LoggingEventType.Warn, null, message, formatArgs);
        }

        public static void Error(this ILogger logger, Exception exception, string message, params object[] formatArgs)
        {
            logger.Log(LoggingEventType.Error, exception, message, formatArgs);
        }
        public static void Error(this ILogger logger, string message, params object[] formatArgs)
        {
            logger.Log(LoggingEventType.Error, null, message, formatArgs);
        }

        public static void Fatal(this ILogger logger, Exception exception, string message, params object[] formatArgs)
        {
            logger.Log(LoggingEventType.Fatal, exception, message, formatArgs);
        }
        public static void Fatal(this ILogger logger, string message, params object[] formatArgs)
        {
            logger.Log(LoggingEventType.Fatal, null, message, formatArgs);
        }
    }
}
