using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightRail.FastServiceLocator
{
    public class FastServiceLocatorImpl : IServiceLocator
    {
        private readonly FastContainer container;

        public FastServiceLocatorImpl(FastContainer container)
        {
            this.container = container;
        }

        public void RegisterSingleton<T>(T instance) where T : class
        {
            this.container.Register(instance);
        }

        public T Resolve<T>()
        {
            return this.container.Resolve<T>();
        }

        public object Resolve(Type type)
        {
            return this.container.Resolve(type);
        }

        public IServiceLocator CreateNestedContainer()
        {
            return new FastServiceLocatorImpl(this.container.Clone());
        }

        public void Dispose()
        {
            container.Dispose();
        }
    }
}
