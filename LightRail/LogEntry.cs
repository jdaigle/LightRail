using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LightRail
{
    // Sourced from: http://stackoverflow.com/a/5646876/507
    public class LogEntry
    {
        public readonly LoggingEventType Severity;
        public readonly string Message;
        public readonly Exception Exception;

        public LogEntry(LoggingEventType severity, string message, Exception exception)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("message");
            }
            if (severity < LoggingEventType.Debug || severity > LoggingEventType.Fatal)
            {
                throw new ArgumentOutOfRangeException("severity");
            }

            this.Severity = severity;
            this.Message = message;
            this.Exception = exception;
        }
    }
}
