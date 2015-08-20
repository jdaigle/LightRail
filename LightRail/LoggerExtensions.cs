using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightRail
{
    // Sourced from: http://stackoverflow.com/a/5646876/507
    public static class LoggerExtensions
    {
        public static void Debug(this ILogger logger, string message)
        {
            logger.Log(new LogEntry(LoggingEventType.Debug, message, null));
        }
        public static void Debug(this ILogger logger, string message, Exception exception)
        {
            logger.Log(new LogEntry(LoggingEventType.Debug, message, exception));
        }
        public static void DebugFormat(this ILogger logger, string format, params object[] args)
        {
            logger.Log(new LogEntry(LoggingEventType.Debug, string.Format(format, args), null));
        }

        public static void Info(this ILogger logger, string message)
        {
            logger.Log(new LogEntry(LoggingEventType.Information, message, null));
        }
        public static void Info(this ILogger logger, string message, Exception exception)
        {
            logger.Log(new LogEntry(LoggingEventType.Information, message, exception));
        }
        public static void InfoFormat(this ILogger logger, string format, params object[] args)
        {
            logger.Log(new LogEntry(LoggingEventType.Information, string.Format(format, args), null));
        }

        public static void Warn(this ILogger logger, string message)
        {
            logger.Log(new LogEntry(LoggingEventType.Warning, message, null));
        }
        public static void Warn(this ILogger logger, string message, Exception exception)
        {
            logger.Log(new LogEntry(LoggingEventType.Warning, message, exception));
        }
        public static void WarnFormat(this ILogger logger, string format, params object[] args)
        {
            logger.Log(new LogEntry(LoggingEventType.Warning, string.Format(format, args), null));
        }

        public static void Error(this ILogger logger, string message)
        {
            logger.Log(new LogEntry(LoggingEventType.Error, message, null));
        }
        public static void Error(this ILogger logger, string message, Exception exception)
        {
            logger.Log(new LogEntry(LoggingEventType.Error, message, exception));
        }
        public static void ErrorFormat(this ILogger logger, string format, params object[] args)
        {
            logger.Log(new LogEntry(LoggingEventType.Error, string.Format(format, args), null));
        }

        public static void Fatal(this ILogger logger, string message)
        {
            logger.Log(new LogEntry(LoggingEventType.Fatal, message, null));
        }
        public static void Fatal(this ILogger logger, string message, Exception exception)
        {
            logger.Log(new LogEntry(LoggingEventType.Fatal, message, exception));
        }
        public static void FatalFormat(this ILogger logger, string format, params object[] args)
        {
            logger.Log(new LogEntry(LoggingEventType.Fatal, string.Format(format, args), null));
        }

        //bool IsDebugEnabled { get; }
        //bool IsErrorEnabled { get; }
        //bool IsFatalEnabled { get; }
        //bool IsInfoEnabled { get; }
        //bool IsWarnEnabled { get; }
    }
}
