using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using LightRail.SqlServer;

namespace LightRail.SampleServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new LightRailConfiguration();

            config.UseSerialization<JsonMessageSerializer>();
            config.UseTransport<ServiceBrokerMessageTransport, ServiceBrokerMessageTransportConfiguration>();

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

            //config.Conventions.DefiningMessagesAs(t => t.Namespace != null && t.Namespace == "Messages");

            //config.ExecuteTheseHandlersFirst(typeof(HandlerB), typeof(HandlerA), typeof(HandlerC));

            config.TransportConfigurationAs<ServiceBrokerMessageTransportConfiguration>().ServiceBrokerConnectionString = "server=localhost;database=servicebus;integrated security=true;";
            config.TransportConfigurationAs<ServiceBrokerMessageTransportConfiguration>().ServiceBrokerQueue = "TestListenerQueue";
            config.TransportConfigurationAs<ServiceBrokerMessageTransportConfiguration>().ServiceBrokerService = "TestListenerService";
            config.TransportConfiguration.MaxRetries = 2;
            //config.SecondLevelRetriesConfig
            //{
            //    Enabled = true,
            //    NumberOfRetries = 2,
            //    TimeIncrease = TimeSpan.FromSeconds(10)
            //};

            config.Handle<SampleMessage>(message =>
            {
                Console.WriteLine("Message Received: " + message.Data);
            });

            var client = LightRailClient.Create(config).Start();

            client.Send(new SampleMessage() { Data = "Hello World" }, "TestListenerService");

            while (true)
            {
                Thread.Sleep(1000);
            }
        }
    }

    public class SampleMessage : IMessage
    {
        public string Data { get; set; }
    }

    public interface IMessage
    {

    }


}