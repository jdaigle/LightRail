using System;

namespace LightRail.ServiceBus
{
    public sealed class InvalidConfigurationException : Exception
    {
        public InvalidConfigurationException(string message) : base(message) { }
    }
}
