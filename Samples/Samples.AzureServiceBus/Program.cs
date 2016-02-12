using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace Samples.AzureServiceBus
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var factory = MessagingFactory.Create("sb://localhost:5672", new MessagingFactorySettings
            {
                TransportType = TransportType.Amqp,
                AmqpTransportSettings = new Microsoft.ServiceBus.Messaging.Amqp.AmqpTransportSettings()
                {
                    UseSslStreamSecurity = false
                }
            });

            Thread.Sleep(1000);
            var sender = factory.CreateMessageSender("event_queue");
            for (int i = 0; i < 15; i++)
            {
                sender.Send(new BrokeredMessage(new SampleCommandMessage()
                {
                    Data = i + " " + DateTime.UtcNow.ToString(),
                }));
                Thread.Sleep(50);
            }

            sender.Close();
            Thread.Sleep(2000);

            var client = factory.CreateQueueClient("event_queue", ReceiveMode.PeekLock);
            client.PrefetchCount = 6;
            client.OnMessage(m =>
            {
                m.Abandon();
            }, new OnMessageOptions() { MaxConcurrentCalls = 7 });
            Thread.Sleep(2000);
            if ("".Length == 0)
                return;

            for (int i = 0; i < 15; i++)
            {
                client.Send(new BrokeredMessage(new SampleCommandMessage()
                {
                    Data = i + " " + DateTime.UtcNow.ToString(),
                }));
                Thread.Sleep(50);
            }
            Thread.Sleep(2000);

            var msg = client.Receive(TimeSpan.FromSeconds(2));
            Thread.Sleep(2000);

            client.Close();
            Thread.Sleep(2000);

            factory.Close();
        }
    }

    public class SampleCommandMessage
    {
        public string Data { get; set; }
    }
}
