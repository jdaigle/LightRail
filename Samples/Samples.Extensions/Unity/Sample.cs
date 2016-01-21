using System;
using System.Threading;
using LightRail.Client;
using LightRail.Client.InMemoryQueue;
using LightRail.Client.Unity;
using Microsoft.Practices.Unity;

namespace Samples.Extensions.Unity
{
    public class Sample
    {
        public void Execute()
        {
            // setup your container as you normally would
            // note that all message handlers execute in a Child Container instance!
            var container = new UnityContainer();

            // HierarchicalLifetimeManager means the object will be disposed after all message handlers execute
            container.RegisterType<IRepository, RepositoryImpl>(new HierarchicalLifetimeManager());

            // PerResolveLifetimeManager means the container will not track the created instance at all
            container.RegisterType<IDoSomething, DoSomethingImpl>(new PerResolveLifetimeManager());

            // ExternallyControlledLifetimeManager means the object is a singleton and won't be disposed
            container.RegisterInstance<SomethingContext>(new SomethingContext(), new ExternallyControlledLifetimeManager());

            // Create an InMemory ServiceBus
            var bus = ServiceBus.Factory.CreateFromInMemory(cfg =>
            {
                // UnityServiceLocator is simply a wrapper around an IUnityContainer
                cfg.ServiceLocator = new LightRail.Client.Unity.UnityServiceLocator(container);

                // Listen for Messages on the named Queue
                cfg.ReceiveFrom("Unity.DemoQueue", r =>
                {
                    // Register all handlers in the current assembly
                    r.ScanForHandlersFromCurrentAssembly();
                });
            });

            // Start the ServiceBus (starts any receivers)
            bus.Start();

            for (int i = 0; i < 10; i++)
            {
                bus.Send(new Message2(), "Unity.DemoQueue");
            }
            Thread.Sleep(1000);

            // Stop the Service (stops any receivers) and blocks waiting for current messages to finish processing.
            bus.Stop();
        }
    }
}
