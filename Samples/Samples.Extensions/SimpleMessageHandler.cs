using LightRail.Client;
using LightRail.Client.Logging;

namespace Samples.Extensions
{
    public static class SimpleMessageHandler
    {
        private static ILogger logger = LogManager.GetLogger("SimpleMessageHandler");

        [MessageHandler]
        public static void Handle(Message1 message)
        {
            logger.Info("Handle {0}={1}", nameof(Message1), message);
        }
    }
}
