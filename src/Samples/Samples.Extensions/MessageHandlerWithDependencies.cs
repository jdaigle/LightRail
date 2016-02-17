using System;
using LightRail.ServiceBus;
using LightRail.ServiceBus.Logging;

namespace Samples.Extensions
{
    public static class MessageHandlerWithDependencies
    {
        private static ILogger logger = LogManager.GetLogger("SimpleMessageHandler");

        [MessageHandler]
        public static void Handle(Message2 message, IRepository repository, SomethingContext c, IDoSomething doSomething)
        {
            logger.Info("Handle {0}={1}", nameof(Message2), message);
        }
    }

    public class SomethingContext : IDisposable
    {
        public void Dispose()
        {
            Console.WriteLine("RepositoryImpl Disposed");
        }
    }

    public interface IRepository
    {
    }

    public class RepositoryImpl : IRepository, IDisposable
    {
        public void Dispose()
        {
            Console.WriteLine("RepositoryImpl Disposed");
        }
    }

    public interface IDoSomething { }
    public class DoSomethingImpl : IDoSomething, IDisposable
    {
        public void Dispose()
        {
            Console.WriteLine("RepositoryImpl Disposed");
        }
    }
}
