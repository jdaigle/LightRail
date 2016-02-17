using System;

namespace LightRail.ServiceBus.Logging
{
    public class ConsoleLogger : ILogger
    {
        private readonly string name;

        public ConsoleLogger(string name)
        {
            this.name = name;
        }

        public bool IsLogEventEnabled(LoggingEventType eventType)
        {
            return true;
        }

        public void Log(LogEntry entry)
        {
            switch (entry.EventType)
            {
                case LoggingEventType.Error:
                case LoggingEventType.Fatal:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LoggingEventType.Warn:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    break;
                case LoggingEventType.Debug:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case LoggingEventType.Info:
                default:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
            }
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.ffff|") + "{0}[{1}] {2}", name, entry.EventType.ToString(), entry.Message);
            if (entry.Exception != null)
            {
                Console.WriteLine("Exception: " + entry.Exception.GetType());
                Console.WriteLine(entry.Exception.Message);
                Console.WriteLine(entry.Exception.StackTrace);
            }
        }
    }
}
