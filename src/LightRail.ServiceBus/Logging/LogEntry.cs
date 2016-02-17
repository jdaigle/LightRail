using System;

namespace LightRail.ServiceBus.Logging
{
    // Sourced from: http://stackoverflow.com/a/5646876/507
    public class LogEntry
    {
        public readonly LoggingEventType EventType;
        public readonly string Message;
        public readonly Exception Exception;

        public LogEntry(LoggingEventType eventType, string message, Exception exception)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException(nameof(message));
            }
            if (eventType < LoggingEventType.Debug || eventType > LoggingEventType.Fatal)
            {
                throw new ArgumentOutOfRangeException(nameof(eventType));
            }

            this.EventType = eventType;
            this.Message = message;
            this.Exception = exception;
        }
    }
}
