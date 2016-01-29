using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LightRail.Amqp.Client;

namespace Samples.Amqp.AzureServiceBus
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Create an AmqpClient from a URI pointing to an address (i.e. a queue)
            // 1) The URI format, simply, is "amqps://[username:password@]host/address"
            // 2) "amqps" specifies TLS over TCP
            // 3) "amqp" specifies TCP
            // 4) Including a username/password specifies SASL authentication.
            // 5) "address" specifies the endpoint at which to send/receive.
            var client = AmqpClient.CreateFromURI("amqps://SenderListener:Euwi1XOtdRCn0A1DvmgnwJSjlIMvyeHUjY61I4GkuOI=@jdaigle-test-amqp.servicebus.windows.net/event_queue");

            // The client will automatically maintain and pool connections/sessions, reconnecting as necessary.

            // Send a message to the specified endpoint. The message is automatically
            // encoded using the default encoding of the client.
            var task = client.SendAsync(new HelloWorldMessage()
            {
                Data = "Hello World! " + Guid.NewGuid().ToString(),
            });
            // All client API operations are async.
            task.Wait();

            Console.WriteLine("Press Enter To Exit");
            Console.ReadLine();
        }
    }

    public class HelloWorldMessage
    {
        public string Data { get; set; }
    }
}
