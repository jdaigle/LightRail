using System;

namespace LightRail.Client
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class MessageHandlerAttribute : Attribute
    {
    }
}
