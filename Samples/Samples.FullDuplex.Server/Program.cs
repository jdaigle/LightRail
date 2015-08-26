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
        config.TransportConfigurationAs<ServiceBrokerMessageTransportConfiguration>().ServiceBrokerQueue = "SamplesFullDuplexServer";
        config.TransportConfigurationAs<ServiceBrokerMessageTransportConfiguration>().ServiceBrokerService = "SamplesFullDuplexServer";

        //LogManager.Use<DefaultFactory>()
        //    .Level(LogLevel.Info);

        config.Handle<RequestDataMessage>((message, context) => Handle(message, context.Client));

        var client = LightRailClient.Create(config).Start();

        Console.WriteLine("Press any key to exit");
        Console.ReadKey();
    }

    static void Handle(RequestDataMessage message, ILightRailClient client)
    {
        Console.WriteLine("Received request {0}.", message.DataId);
        Console.WriteLine("String received: {0}.", message.String);

        DataResponseMessage response = new DataResponseMessage
        {
            DataId = message.DataId,
            String = message.String
        };

        client.Reply(response);
    }
}