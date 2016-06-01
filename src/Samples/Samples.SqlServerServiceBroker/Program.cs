using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LightRail.ServiceBus;
using LightRail.ServiceBus.SqlServer;

namespace Samples.SqlServerServiceBroker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Create an Service Broker ServiceBus
            var bus = ServiceBus.Factory.CreateFromServiceBroker(cfg =>
            {
                cfg.ServiceBrokerConnectionStringName = "ServiceBus";
                cfg.ServiceBrokerSendingService = "//LightRail/FooService";
                cfg.ReceiveFrom(r =>
                {
                    // Register all handlers in the current assembly
                    r.ScanForMessageHandlersFromCurrentAssembly();

                    r.ServiceBrokerService = "[//LightRail/FooService]";
                    r.ServiceBrokerQueue = "LightRailFooServiceQueue";

                    r.MaxConcurrency = 10;
                });
            });

            // Start the ServiceBus (starts any receivers)
            bus.Start();

                for (int i = 0; i < 100; i++)
                {
                    bus.Send(new Message1(), "[//LightRail/FooService]");
                }
            while (true)
                Thread.Sleep(5000);
        }
    }
}
