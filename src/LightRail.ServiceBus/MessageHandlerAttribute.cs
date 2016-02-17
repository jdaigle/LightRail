using System;

namespace LightRail.ServiceBus
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class MessageHandlerAttribute : Attribute
    {
    }
}
