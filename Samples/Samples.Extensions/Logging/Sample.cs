using LightRail.Client;
using LightRail.Client.InMemoryQueue;
using LightRail.Client.log4net;
using LightRail.Client.Logging;
using LightRail.Client.NLog;

namespace Samples.Extensions.Logging
{
    public class Sample
    {
        public void Execute()
        {
            // Logging is statically assigned at startup and should not be
            // changed. So uncomment the logger you want to use.

            //UseLog4Net();
            //UseNLog();

            // Create an InMemory ServiceBus
            var bus = ServiceBus.Factory.CreateFromInMemory(cfg =>
            {
                // Listen for Messages on the named Queue
                cfg.ReceiveFrom("DemoQueue", r =>
                {
                    // Register all handlers in the current assembly
                    r.ScanForHandlersFromCurrentAssembly();
                });
            });

            // Start the ServiceBus (starts any receivers)
            bus.Start();

            for (int i = 0; i < 10; i++)
            {
                bus.Send(new Message1(), "DemoQueue");
            }

            // Stop the Service (stops any receivers) and blocks waiting for current messages to finish processing.
            bus.Stop();
        }

        private static void UseNLog()
        {
            // Override the default LogManager to use Log4Net
            LogManager.UseFactory<NLogLoggerFactory>();
        }

        private static void UseLog4Net()
        {
            // Override the default LogManager to use Log4Net
            LogManager.UseFactory<Log4NetLoggerFactory>();
        }
    }
}
