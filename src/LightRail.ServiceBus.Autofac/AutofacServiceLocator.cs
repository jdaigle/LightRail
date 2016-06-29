using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Autofac.Features.ResolveAnything;

namespace LightRail.ServiceBus.Autofac
{
    public class AutofacServiceLocator : IServiceLocator
    {
        public AutofacServiceLocator()
            :this(new ContainerBuilder().Build())
        {
        }

        public AutofacServiceLocator(ILifetimeScope container)
        {
            this.container = container;
            this.container.ComponentRegistry.AddRegistrationSource(new AnyConcreteTypeNotAlreadyRegisteredSource());
        }

        private readonly ILifetimeScope container;

        public void RegisterSingleton<T>(T instance)
            where T : class
        {
            var builder = new ContainerBuilder();
            var services = GetAllServices(typeof(T)).ToArray();
            var registrationBuilder = builder.RegisterInstance(instance).As(services).SingleInstance();
            builder.Update(container.ComponentRegistry);
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
            return new AutofacServiceLocator(container.BeginLifetimeScope());
        }

        public void Dispose()
        {
            container.Dispose();
        }

        static IEnumerable<Type> GetAllServices(Type type)
        {
            if (type == null)
            {
                return new List<Type>();
            }

            var result = new List<Type>(type.GetInterfaces()) { type };

            foreach (var interfaceType in type.GetInterfaces())
            {
                result.AddRange(GetAllServices(interfaceType));
            }

            return result.Distinct();
        }
    }
}
