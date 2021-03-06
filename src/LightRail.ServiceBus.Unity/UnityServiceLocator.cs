﻿using System;
using Microsoft.Practices.Unity;

namespace LightRail.ServiceBus.Unity
{
    public class UnityServiceLocator : IServiceLocator
    {
        public UnityServiceLocator()
        {
            this.container = new UnityContainer();
        }

        public UnityServiceLocator(IUnityContainer container)
        {
            this.container = container;
        }

        private readonly IUnityContainer container;

        public void RegisterSingleton<T>(T instance)
            where T : class
        {
            this.container.RegisterInstance<T>(instance, new ExternallyControlledLifetimeManager());
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
            return new UnityServiceLocator(container.CreateChildContainer());
        }

        public void Dispose()
        {
            container.Dispose();
        }
    }
}
