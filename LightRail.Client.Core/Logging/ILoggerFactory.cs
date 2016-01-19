using System;

namespace LightRail.Client.Logging
{
    public interface ILoggerFactory
    {
        ILogger GetLogger(string name);
        ILogger GetLogger(Type type);
    }
}
