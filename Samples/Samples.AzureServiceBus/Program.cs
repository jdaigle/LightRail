using System;
using System.Threading;
using System.Threading.Tasks;
using LightRail.Client;
using LightRail.Client.Amqp;

namespace Samples.AzureServiceBus
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var busControl = ServiceBus.Factory.CreateFromAmqp(cfg =>
            {
                cfg.AmqpAddressFromUri("amqps://SenderListener:Euwi1XOtdRCn0A1DvmgnwJSjlIMvyeHUjY61I4GkuOI=@jdaigle-test-amqp.servicebus.windows.net");

                cfg.ReceiveFrom("event_queue", e =>
                {
                    e.MaxConcurrency = 5;
                    //  e.ScanForHandlersFromAssembly(typeof(Program).Assembly);

                    //e.Handle<SampleCommandMessage>(async (message, context) =>
                    //{
                    //    Console.WriteLine($"Lambda: {nameof(SampleCommandMessage)}.Data=[{message.Data}]");
                    //    await Task.Delay(0);
                    //});

                    //  e.Handle<SampleCommandMessage>(SimpleMessageHandler.HandleSpecial);
                    //  e.Handle<SampleCommandMessage>(SimpleMessageHandler.HandleSpecial2);

                    e.Handle<SampleCommandMessage>((message, context) =>
                    {
                        Console.WriteLine("MESSAGE RECEIVED!" + message.Data);
                    });
                });

                cfg.ReceiveFrom("event_queue/$DeadLetterQueue", e =>
                {
                    e.MaxConcurrency = 1;
                });
            });
            // starts any receive threads
            busControl.Start();

            for (int i = 0; i < 10; i++)
            {
                //Thread.Sleep(2000);
                busControl.Send(new SampleCommandMessage()
                {
                    Data = DateTime.UtcNow.ToString(),
                }, "event_queue");
            }

            Console.WriteLine("Press Any Key To Exit.");
            Console.ReadKey();

            busControl.Stop(TimeSpan.FromSeconds(30));
        }
    }

    public class SampleCommandMessage
    {
        public string Data { get; set; }
    }
}
