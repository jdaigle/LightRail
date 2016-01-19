using System;
using System.Threading.Tasks;
using LightRail.Client;
using LightRail.Client.InMemory;

namespace Samples.AzureServiceBus
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var busControl = ServiceBus.Factory.CreateFromInMemory(cfg =>
            {
                //var host = cfg.Host(new Uri("amqps://SenderListener:2o6Aj2htucx/Oti9IzyVfBwS2RYCd2+UGfZWYepOx+I=@jdaigle-test-amqp.servicebus.windows.net"), hcfg =>
                //{
                //
                //});

                object host = null;

                cfg.ReceiveFrom(host, "event_queue", e =>
                {
                    e.ScanForHandlersFromAssembly(typeof(SimpleMessageHandler).Assembly);

                    e.Handle<ISampleEventMessage>(async (message, context) =>
                    {
                        Console.WriteLine($"Lambda: {nameof(ISampleEventMessage)}.Data=[{message.Data}]");
                        await Task.Delay(0);
                    });

                    e.Handle<SampleCommandMessage>(SimpleMessageHandler.HandleSpecial);
                });
            });

            // starts any receive threads
            busControl.Start();

            Console.ReadLine();

            busControl.Stop(TimeSpan.FromSeconds(30));
        }
    }

    public interface ISampleEventMessage
    {
        string Data { get; set; }
    }

    public class SampleCommandMessage
    {
        public string Data { get; set; }
    }

    public class SimpleMessageHandler
    {
        [MessageHandler]
        public async Task Handle(ISampleEventMessage message)
        {
            Console.WriteLine($"{nameof(ISampleEventMessage)}.Data=[{message.Data}]");
            await Task.Delay(0);
        }

        [MessageHandler]
        public async Task Handle(SampleCommandMessage message)
        {
            Console.WriteLine($"{nameof(SampleCommandMessage)}.Data=[{message.Data}]");
            await Task.Delay(0);
        }

        public static async Task HandleSpecial(SampleCommandMessage message)
        {
            Console.WriteLine($"Non-Attribute Handler: {nameof(SampleCommandMessage)}.Data=[{message.Data}]");
            await Task.Delay(0);
        }
    }

    public static class StaticMessageHandler
    {
        [MessageHandler]
        public static async Task Handle(ISampleEventMessage message)
        {
            Console.WriteLine($"{nameof(ISampleEventMessage)}.Data=[{message.Data}]");
            await Task.Delay(0);
        }

        [MessageHandler]
        public static async Task Handle(SampleCommandMessage message)
        {
            Console.WriteLine($"{nameof(SampleCommandMessage)}.Data=[{message.Data}]");
            await Task.Delay(0);
        }
    }

    public static class StaticMessageHandlerWithDependencies
    {
        [MessageHandler]
        public static async Task Handle(ISampleEventMessage message, MessageContext messageContext)
        {
            Console.WriteLine($"{nameof(ISampleEventMessage)}.Data=[{message.Data}]");
            await Task.Delay(0);
        }

        [MessageHandler]
        public static async Task Handle(SampleCommandMessage message, MessageContext messageContext)
        {
            Console.WriteLine($"{nameof(SampleCommandMessage)}.Data=[{message.Data}]");
            await Task.Delay(0);
        }
    }
}
