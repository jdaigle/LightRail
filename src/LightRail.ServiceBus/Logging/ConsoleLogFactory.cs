using System;

namespace LightRail.ServiceBus.Logging
{
    public class ConsoleLogFactory : ILoggerFactory
    {
        public ILogger GetLogger(string name)
        {
            return new ConsoleLogger(name);
        }


        public ILogger GetLogger(Type type)
        {
            return new ConsoleLogger(type.FullName);
        }
    }
}
