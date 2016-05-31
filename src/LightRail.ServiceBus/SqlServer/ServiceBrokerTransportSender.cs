using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightRail.ServiceBus.Logging;
using LightRail.ServiceBus.SqlServer.Config;
using LightRail.ServiceBus.Transport;

namespace LightRail.ServiceBus.SqlServer
{
    public sealed class ServiceBrokerTransportSender : ITransportSender
    {
        public ServiceBrokerTransportSender(ServiceBrokerServiceBusConfiguration configuration)
        {
            ServiceBrokerMessageType = configuration.ServiceBrokerMessageType;
            if (string.IsNullOrWhiteSpace(ServiceBrokerMessageType))
            {
                throw new InvalidConfigurationException("ServiceBrokerMessageType cannot be null or whitespace.");
            }
            ServiceBrokerContract = configuration.ServiceBrokerContract;
            if (string.IsNullOrWhiteSpace(ServiceBrokerContract))
            {
                throw new InvalidConfigurationException("ServiceBrokerContract cannot be null or whitespace.");
            }
            ServiceBrokerSendingService = configuration.ServiceBrokerSendingService;
            if (string.IsNullOrWhiteSpace(ServiceBrokerSendingService))
            {
                throw new InvalidConfigurationException("ServiceBrokerSendingService cannot be null or whitespace.");
            }
            ConnectionString = configuration.ServiceBrokerConnectionString;
            if (string.IsNullOrWhiteSpace(ConnectionString) &&
                !string.IsNullOrWhiteSpace(configuration.ServiceBrokerConnectionStringName) &&
                ConfigurationManager.ConnectionStrings[configuration.ServiceBrokerConnectionStringName] != null)
            {
                ConnectionString = ConfigurationManager.ConnectionStrings[configuration.ServiceBrokerConnectionStringName].ConnectionString;
            }
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                throw new InvalidConfigurationException("ServiceBrokerConnectionString cannot be null or whitespace.");
            }
            messageEncoder = configuration.MessageEncoder;
            if (messageEncoder == null)
            {
                throw new InvalidConfigurationException("MessageEncoder cannot be null.");
            }
        }

        private static ILogger logger = LogManager.GetLogger("LightRail.ServiceBus.SqlServer.ServiceBroker");
        private string ServiceBrokerMessageType { get; }
        private string ServiceBrokerContract { get; }
        private string ServiceBrokerSendingService { get; }
        private string ConnectionString { get; }
        private readonly IMessageEncoder messageEncoder;

        public void Send(OutgoingTransportMessage transportMessage, IEnumerable<string> addresses)
        {
            var transaction = SqlServerTransactionManager.TryGetTransactionForCurrentTask();
            var commitAndDisposeTransaction = false;
            if (transaction == null)
            {
                transaction = SqlServerTransactionManager.BeginTransaction(ConnectionString);
                commitAndDisposeTransaction = true;
            }
            try
            {

                transportMessage.Headers[StandardHeaders.TimeSent] = DateTime.UtcNow.ToString("o");
                if (!transportMessage.Headers.ContainsKey(StandardHeaders.ReplyToAddress))
                {
                    transportMessage.Headers[StandardHeaders.ReplyToAddress] = this.ServiceBrokerSendingService;
                }
                transportMessage.Headers[StandardHeaders.OriginatingAddress] = this.ServiceBrokerSendingService;

                using (var stream = new MemoryStream())
                {
                    var encodedMessage = messageEncoder.EncodeAsString(transportMessage.Message);
                    FastXmlTransportMessageSerializer.Serialize(transportMessage.Headers, encodedMessage, stream);
                    var messageBuffer = stream.ToArray();
                    foreach (var destination in addresses)
                    {
                        var conversationHandle = ServiceBrokerWrapper.SendOne(
                              transaction: transaction
                            , initiatorServiceName: ServiceBrokerSendingService
                            , targetServiceName: destination
                            , messageContractName: ServiceBrokerContract
                            , messageType: ServiceBrokerMessageType
                            , body: messageBuffer);
#if DEBUG
                        logger.Debug(string.Format("Sending message {0} with Handle {1} to Service Named {2}.",
                                                   transportMessage.Message.GetType().AssemblyQualifiedName,
                                                   conversationHandle,
                                                   destination));
                        logger.Debug(string.Format("ToString() of the message yields: {0}\n" +
                                                   "Message headers:\n{1}",
                                                   transportMessage.Message.ToString(),
                                                   string.Join(", ", transportMessage.Headers.Select(h => h.Key + ":" + h.Value).ToArray())));
#endif
                    }
                }

                if (commitAndDisposeTransaction)
                {
                    transaction.Commit();
                }
            }
            finally
            {
                if (commitAndDisposeTransaction)
                {
                    SqlServerTransactionManager.TryDisposeTransactionAndConnection(transaction);
                }
            }
        }
    }
}
