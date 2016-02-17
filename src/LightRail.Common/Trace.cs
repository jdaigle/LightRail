using System;

namespace LightRail
{
    public delegate void TraceEventHandler(TraceSource source, TraceLevel traceLevel, Exception exception, string message, params object[] formatArgs);

    public static class Trace
    {
        /// <summary>
        /// Callback for trace events.
        /// </summary>
        public static event TraceEventHandler OnTraceEvent;

        /// <summary>
        /// Sets the desired trace level for code to execute. Only trace events >= EnabledTraceLevel will be
        /// sent to the OnTraceEvent handlers.
        /// </summary>
        public static TraceLevel EnabledTraceLevel = TraceLevel.Debug;

        public static bool IsDebugEnabled { get { return EnabledTraceLevel >= TraceLevel.Debug; } }

        public static void Debug(this TraceSource source, Exception exception, string message, params object[] formatArgs)
        {
            source.Log(TraceLevel.Debug, exception, message, formatArgs);
        }
        public static void Debug(this TraceSource source, string message, params object[] formatArgs)
        {
            source.Log(TraceLevel.Debug, null, message, formatArgs);
        }

        public static void Info(this TraceSource source, Exception exception, string message, params object[] formatArgs)
        {
            source.Log(TraceLevel.Info, exception, message, formatArgs);
        }
        public static void Info(this TraceSource source, string message, params object[] formatArgs)
        {
            source.Log(TraceLevel.Info, null, message, formatArgs);
        }

        public static void Warn(this TraceSource source, Exception exception, string message, params object[] formatArgs)
        {
            source.Log(TraceLevel.Warn, exception, message, formatArgs);
        }
        public static void Warn(this TraceSource source, string message, params object[] formatArgs)
        {
            source.Log(TraceLevel.Warn, null, message, formatArgs);
        }

        public static void Error(this TraceSource source, Exception exception)
        {
            source.Log(TraceLevel.Error, exception, "");
        }
        public static void Error(this TraceSource source, Exception exception, string message, params object[] formatArgs)
        {
            source.Log(TraceLevel.Error, exception, message, formatArgs);
        }
        public static void Error(this TraceSource source, string message, params object[] formatArgs)
        {
            source.Log(TraceLevel.Error, null, message, formatArgs);
        }

        public static void Fatal(this TraceSource source, Exception exception, string message, params object[] formatArgs)
        {
            source.Log(TraceLevel.Fatal, exception, message, formatArgs);
        }
        public static void Fatal(this TraceSource source, string message, params object[] formatArgs)
        {
            source.Log(TraceLevel.Fatal, null, message, formatArgs);
        }

        public static void Log(this TraceSource source, TraceLevel traceLevel, Exception exception, string message, params object[] formatArgs)
        {
            if (EnabledTraceLevel >= traceLevel)
            {
                var onTrace = OnTraceEvent;
                if (onTrace != null)
                {
                    onTrace(source, traceLevel, exception, message, formatArgs);
                }
            }
        }

        public static readonly TraceEventHandler ConsoleTraceEventHandler = (traceSource, traceLevel, exception, message, formatArgs) =>
        {
            switch (traceLevel)
            {
                case TraceLevel.Fatal:
                case TraceLevel.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case TraceLevel.Warn:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    break;
                case TraceLevel.Info:
                default:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case TraceLevel.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
            }
            var formattedMessage = string.Format(message, formatArgs);
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.ffff|") + "{0}[{1}] {2}", traceSource.Name, traceLevel.ToString(), formattedMessage);
            if (exception != null)
            {
                Console.WriteLine("Exception: " + exception.GetType());
                Console.WriteLine(exception.Message);
                Console.WriteLine(exception.StackTrace);
            }
        };

    }
}
