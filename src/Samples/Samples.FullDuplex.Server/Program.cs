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

        var client = config.CreateBus().Start();

        Console.WriteLine("Press any key to exit");
        Console.ReadKey();
    }

    [MessageHandler]
    static void Handle(RequestDataMessage message, IBus client)
    {
        //try to uncomment the line below to see the error handling in action
        // * lightrwail will retry the configured number of times
        // * when the max retries is reached the message will be given to the faultmanager (in memory in this case)
        //throw new Exception("Database connection lost");

        Console.WriteLine("Received request {0}.", message.DataId);
        Console.WriteLine("String received: {0}.", message.String);

        DataResponseMessage response = new DataResponseMessage
        {
            DataId = message.DataId,
            String = message.String
        };

        client.OutgoingHeaders["MyCustomHeader"] = Guid.NewGuid().ToString();

        client.Send(response, client.CurrentMessageContext[StandardHeaders.ReplyToAddress]);
    }
}