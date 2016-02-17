using System;

namespace LightRail.ServiceBus.Logging
{
    public interface ILoggerFactory
    {
        ILogger GetLogger(string name);
        ILogger GetLogger(Type type);
    }
}
