using System;

namespace LightRail.Client
{
    public interface IServiceLocator : IDisposable
    {
        void RegisterSingleton<T>(T instance) where T : class;

        T Resolve<T>();
        object Resolve(Type type);

        IServiceLocator CreateNestedContainer();
    }
}
