using System.Threading;
using LightRail.ServiceBus;
using LightRail.ServiceBus.Logging;

namespace Samples.SqlServerServiceBroker
{
    public static class SimpleMessageHandler
    {
        private static ILogger logger = LogManager.GetLogger("SimpleMessageHandler");

        private static volatile int messageCount;

        [MessageHandler]
        public static void Handle(Message1 message)
        {
            logger.Info("Handle {0}={1} Count={2}", nameof(Message1), message, messageCount++);
            Thread.Sleep(250);
            if (message.MessageCounter % 15 == 0)
                throw new System.Exception("buzz...");
        }
    }
}
