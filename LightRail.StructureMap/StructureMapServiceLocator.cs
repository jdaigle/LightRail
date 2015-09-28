using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StructureMap;

namespace LightRail.StructureMap
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

        public IServiceLocator CreateChildContainer()
        {
            return new StructureMapServiceLocator(container.CreateChildContainer());
        }

        public void Dispose()
        {
            container.Dispose();
        }
    }
}
