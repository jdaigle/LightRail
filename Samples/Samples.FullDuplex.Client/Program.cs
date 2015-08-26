using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightRail;
using LightRail.SqlServer;

class Program
{
    static void Main(string[] args)
    {
        var config = new LightRailConfiguration();
        config.AddAssemblyToScan(typeof(Program).Assembly);
        config.AddAssemblyToScan(typeof(RequestDataMessage).Assembly);
        config.UseTransport<ServiceBrokerMessageTransport, ServiceBrokerMessageTransportConfiguration>();
        config.TransportConfigurationAs<ServiceBrokerMessageTransportConfiguration>().ServiceBrokerConnectionString = "server=localhost;database=servicebus;integrated security=true;";
        config.TransportConfigurationAs<ServiceBrokerMessageTransportConfiguration>().ServiceBrokerQueue = "SamplesFullDuplexClient";
        config.TransportConfigurationAs<ServiceBrokerMessageTransportConfiguration>().ServiceBrokerService = "SamplesFullDuplexClient";

        //LogManager.Use<DefaultFactory>()
        //    .Level(LogLevel.Info);

        config.Handle<DataResponseMessage>((message, context) => Handle(message));

        var client = LightRailClient.Create(config).Start();

        Console.WriteLine("Press enter to send a message");
        Console.WriteLine("Press any key to exit");
        while (true)
        {
            ConsoleKeyInfo key = Console.ReadKey();
            Console.WriteLine();
            
            if (key.Key != ConsoleKey.Enter)
            {
                return;
            }
            Guid guid = Guid.NewGuid();
            Console.WriteLine("Requesting to get data by id: {0}", guid.ToString("N"));

            RequestDataMessage message = new RequestDataMessage
            {
                DataId = guid,
                String = "<node>it's my \"node\" & i like it<node>"
            };
            client.Send(message, "SamplesFullDuplexServer");
        }
    }

    static void Handle(DataResponseMessage message)
    {
        Console.WriteLine("Response received with description: {0}", message.String);
    }
}