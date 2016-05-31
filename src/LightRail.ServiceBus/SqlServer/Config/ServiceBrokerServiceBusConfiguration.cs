using LightRail.ServiceBus.Config;
using LightRail.ServiceBus.Transport;

namespace LightRail.ServiceBus.SqlServer.Config
{
    public class ServiceBrokerServiceBusConfiguration : BaseServiceBusConfig
    {
        /// <summary>
        /// Encodes/Decodes messages sent & received in SQL Server Service Broker messages. Defaults to LightRail.ServiceBus.JsonMessageEncoder
        /// </summary>
        public IMessageEncoder MessageEncoder { get; set; } = new JsonMessageEncoder();

        public string ServiceBrokerMessageType { get; set; }
        public string ServiceBrokerContract { get; set; }
        public string ServiceBrokerSendingService { get; set; }

        /// <summary>
        /// The connection string used to connect to a SQL Server database with Service Broker enabled.
        /// </summary>
        /// <remarks>
        /// Either ServiceBrokerConnectionString or ServiceBrokerConnectionStringName must be set. If
        /// ServiceBrokerConnectionString is set, then it is always used.
        /// </remarks>
        public string ServiceBrokerConnectionString { get; set; }
        /// <summary>
        /// The name of a connection string in the app.config/web.config used to connect to a SQL Server database with Service Broker enabled.
        /// </summary>
        /// <remarks>
        /// Either ServiceBrokerConnectionString or ServiceBrokerConnectionStringName must be set. If
        /// ServiceBrokerConnectionString is set, then it is always used.
        /// </remarks>
        public string ServiceBrokerConnectionStringName { get; set; }

        public override ITransportSender CreateTransportSender()
        {
            return new ServiceBrokerTransportSender(this);
        }
    }
}
