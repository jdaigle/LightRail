using System;

namespace LightRail.ServiceBus
{
    public interface IServiceLocator : IDisposable
    {
        void RegisterSingleton<T>(T instance) where T : class;

        T Resolve<T>();
        object Resolve(Type type);

        IServiceLocator CreateNestedContainer();
    }
}
