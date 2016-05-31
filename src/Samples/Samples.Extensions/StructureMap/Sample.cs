using System;
using System.Threading;
using LightRail.ServiceBus;
using LightRail.ServiceBus.InMemoryQueue;
using LightRail.ServiceBus.StructureMap;
using StructureMap;

namespace Samples.Extensions.StructureMap
{
    public class Sample
    {
        public void Execute()
        {
            // setup your container as you normally would
            // note that all message handlers execute in a Nested Container instance!
            var container = new Container(cfg =>
            {
                cfg.For<IRepository>().Use<RepositoryImpl>();
                cfg.ForSingletonOf<SomethingContext>().Use(new SomethingContext());
                cfg.For<IDoSomething>().Use<DoSomethingImpl>().AlwaysUnique();
            });

            // Create an InMemory ServiceBus
            var bus = ServiceBus.Factory.CreateFromInMemory(cfg =>
            {
                // StructureMapServiceLocator is simply a wrapper around an IContainer
                cfg.ServiceLocator = new StructureMapServiceLocator(container);

                // Listen for Messages on the named Queue
                cfg.ReceiveFrom("StructureMap.DemoQueue", r =>
                {
                    // Register all handlers in the current assembly
                    r.ScanForMessageHandlersFromCurrentAssembly();
                });
            });

            // Start the ServiceBus (starts any receivers)
            bus.Start();

            for (int i = 0; i < 10; i++)
            {
                bus.Send(new Message2(), "StructureMap.DemoQueue");
            }
            Thread.Sleep(1000);

            // Stop the Service (stops any receivers) and blocks waiting for current messages to finish processing.
            bus.Stop();
        }
    }
}
