using System;
using StructureMap;

namespace LightRail.ServiceBus.StructureMap
{
    public class StructureMapServiceLocator : IServiceLocator
    {
        public StructureMapServiceLocator()
        {
            this.container = new Container();
        }

        public StructureMapServiceLocator(IContainer container)
        {
            this.container = container;
        }

        private readonly IContainer container;

        public void RegisterSingleton<T>(T instance)
            where T : class
        {
            this.container.Configure(c =>
            {
                c.ForSingletonOf<T>().Use(instance);
            });
        }

        public T Resolve<T>()
        {
            return this.container.GetInstance<T>();
        }

        public object Resolve(Type type)
        {
            return this.container.GetInstance(type);
        }

        public IServiceLocator CreateNestedContainer()
        {
            return new StructureMapServiceLocator(container.GetNestedContainer());
        }

        public void Dispose()
        {
            container.Dispose();
        }
    }
}
