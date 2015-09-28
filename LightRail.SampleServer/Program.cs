using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using LightRail.Logging;
using LightRail.SqlServer;
using LightRail.StructureMap;

namespace LightRail.SampleServer
{
    class Program
    {
        static void Main(string[] args)
        {
            // always start with a configuration object
            var config = new LightRailConfiguration();

            config.ServiceLocator = new StructureMapServiceLocator();

            // Optional: by default we use the ConsoleLogManager
            // but you can change that by specifying the type
            //config.UseLogger<ConsoleLogManager>();
            LogManager.UseFactory<Log4NetLoggerFactory>();

            // Override to use log4net
            log4net.Config.XmlConfigurator.Configure();

            // Optional: JsonMessageSerializer by default
            // but you can change that by specifying the type
            config.UseSerialization<JsonMessageSerializer>();

            //append mapping to config
            //config.MessageEndpointMappings.Add(
            //    new MessageEndpointMapping
            //    {
            //        AssemblyName = "assembly",
            //        Namespace = "", // To register all message types defined in an assembly with a specific namespace (it does not include sub namespaces)
            //        Endpoint = "queue@machinename"
            //    });
            //config.MessageEndpointMappings.Add(
            //    new MessageEndpointMapping
            //    {
            //        TypeFullName = "assembly",
            //        Endpoint = "queue@machinename"
            //    });

            // by default, all assemblies are scanned. but you can
            // turn that off and scan specific assemblies
            config.AddAssemblyToScan(typeof(Program).Assembly);

            // by default, there aren't any messages types so you must specify conventions
            config.MessageTypeConventions.AddConvention(t => typeof(IMessage).IsAssignableFrom(t));

            //config.ExecuteTheseHandlersFirst(typeof(HandlerB), typeof(HandlerA), typeof(HandlerC));

            // You must specify the type of transport and transport config
            config.UseTransport<ServiceBrokerMessageTransport, ServiceBrokerMessageTransportConfiguration>();

            // Max Retries changes how often we retry on errors
            config.TransportConfiguration.MaxRetries = 2;

            // required ServiceBroker settings
            config.TransportConfigurationAs<ServiceBrokerMessageTransportConfiguration>().ServiceBrokerConnectionString = "server=localhost;database=servicebus;integrated security=true;";
            config.TransportConfigurationAs<ServiceBrokerMessageTransportConfiguration>().ServiceBrokerQueue = "TestListenerQueue";
            config.TransportConfigurationAs<ServiceBrokerMessageTransportConfiguration>().ServiceBrokerService = "TestListenerService";


            // create the client, and start to begin listening
            var client = config.CreateBus().Start();

            // sample sending
            client.Send(new SampleMessage() { Data = "Hello World" }, "TestListenerService");
            client.Send<IOnly>(x =>
            {
                x.Data = "hello world...!";
            }, "TestListenerService");

            while (true)
            {
                Thread.Sleep(1000);
            }
        }

        [MessageHandler]
        static void Handle(SampleMessage message)
        {
            Console.WriteLine("Message Received: " + message.Data);
        }
        
        [MessageHandler]
        static void Handle(IOnly message)
        {
            Console.WriteLine("Message Received: " + message.Data);
        }

        [MessageHandler]
        static void Handle(IMessage message)
        {
            Console.WriteLine("Message Received:");
        }
    }

    public interface IOnly : IMessage
    {
        string Data { get; set; }
    }

    public class SampleMessage : IMessage
    {
        public string Data { get; set; }
    }

    public interface IMessage
    {

    }


}