using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LightRail.ServiceBus.Logging;
using LightRail.ServiceBus.SqlServer.Config;
using LightRail.ServiceBus.Transport;

namespace LightRail.ServiceBus.SqlServer
{
    public sealed class ServiceBrokerTransportReceiver : ITransportReceiver
    {
        public ServiceBrokerTransportReceiver(ServiceBrokerServiceBusReceiverConfiguration receiverConfiguration, ServiceBrokerServiceBusConfiguration configuration)
        {
            messageMapper = configuration.MessageMapper;
            messageEncoder = configuration.MessageEncoder;
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
            ServiceBrokerService = receiverConfiguration.ServiceBrokerService;
            if (string.IsNullOrWhiteSpace(ServiceBrokerService))
            {
                throw new InvalidConfigurationException("ServiceBrokerService cannot be null or whitespace.");
            }
            ServiceBrokerQueue = receiverConfiguration.ServiceBrokerQueue;
            if (string.IsNullOrWhiteSpace(ServiceBrokerQueue))
            {
                throw new InvalidConfigurationException("ServiceBrokerQueue cannot be null or whitespace.");
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
            MaxRetries = receiverConfiguration.MaxRetries;
            if (MaxRetries < 0)
            {
                MaxRetries = 0;
            }
            MaxConcurrency = receiverConfiguration.MaxConcurrency;
            if (MaxConcurrency < 0)
            {
                MaxConcurrency = 0;
            }
            if (MaxConcurrency > 0)
            {
                workerThreadPool = new Semaphore(MaxConcurrency, MaxConcurrency);
            }
            faultManager = new TransportMessageFaultManager(MaxRetries);
        }

        private static readonly int waitTimeout = 15 * 60 * 1000; // wait 15 minutes

        private static ILogger logger = LogManager.GetLogger("LightRail.ServiceBus.SqlServer.ServiceBroker");
        private readonly IMessageMapper messageMapper;
        private readonly IMessageEncoder messageEncoder;
        private string ServiceBrokerMessageType { get; }
        private string ServiceBrokerContract { get; }
        private string ServiceBrokerService { get; }
        private string ServiceBrokerQueue { get; }
        private string ConnectionString { get; }
        private int MaxRetries { get; }
        private int MaxConcurrency { get; }
        private readonly TransportMessageFaultManager faultManager;
        private readonly Semaphore workerThreadPool;

        public event EventHandler<MessageAvailableEventArgs> MessageAvailable;
        public event EventHandler<PoisonMessageDetectedEventArgs> PoisonMessageDetected;

        private bool isRunning;
        private readonly object startLock = new object();

        public void Start()
        {
            if (isRunning)
            {
                return;
            }
            lock (startLock)
            {
                if (isRunning)
                {
                    return;
                }
                InitServiceBroker();
                if (MaxConcurrency > 0)
                {
                    Task.Run((Action)LoopAndReceiveMessage);
                }
                isRunning = true;
            }
        }

        private void InitServiceBroker()
        {
            using (var connection = SqlServerTransactionManager.OpenConnection(ConnectionString))
            using (var transaction = connection.BeginTransaction())
            {
                // Ensure the service and queue exist
                ServiceBrokerWrapper.CreateServiceAndQueue(transaction, ServiceBrokerService, ServiceBrokerQueue, ServiceBrokerMessageType, ServiceBrokerContract);
                transaction.Commit();
                connection.Close();
            }
            PoisonMessageSqlHelper.EnsureTableExists(ConnectionString);
        }

        public void Stop(TimeSpan timeSpan)
        {
            throw new NotImplementedException();
        }

        private void LoopAndReceiveMessage()
        {
            logger.Info("Receiving messages on queue {0}", ServiceBrokerQueue);
            while (isRunning)
            {
                workerThreadPool.WaitOne(); // will block if reached max level of concurrency
                TryReceiveMessage();
            }
        }

        private void TryReceiveMessage()
        {
            // NOTE this method _should not_ throw an exception!
            ServiceBrokerMessage message = null;
            SqlTransaction transaction = null;
            try
            {
                transaction = SqlServerTransactionManager.BeginTransaction(ConnectionString);
                logger.Debug("RECEIVE FROM {0}", ServiceBrokerQueue);
                message = ServiceBrokerWrapper.WaitAndReceive(transaction, ServiceBrokerQueue, waitTimeout);
            }
            catch (Exception e)
            {
                logger.Error(e, "Exception caught trying to receive from queue {0}. Rolling back and backing off before reconnecting.", ServiceBrokerQueue);
                SqlServerTransactionManager.TryRollbackTransaction(transaction);
                SqlServerTransactionManager.TryForceDisposeTransactionAndConnection(transaction);
                Thread.Sleep(1000); // back-off because we will return and loop immediately
                workerThreadPool.Release(1); // always release before returning!
                return;
            }

            // No message? That's okay
            if (message == null)
            {
                try
                {
                    SqlServerTransactionManager.CommitTransactionAndDisposeConnection(transaction);
                }
                catch (Exception)
                {
                    SqlServerTransactionManager.TryForceDisposeTransactionAndConnection(transaction);
                    throw;
                }
                workerThreadPool.Release(1); // always release before returning!
                return;
            }

            // start a
            Task.Run(() =>
            {
                // TryHandleHessage _should not_ throw an exception. It will also commit and cleanup the transactions
                try
                {
                    TryHandleMessage(transaction, message);
                }
                finally
                {
                    var previousCount = workerThreadPool.Release(1); // release this semaphore back to the pool regardless of what happened
                    logger.Debug("Current Concurrent Requests = {0}", MaxConcurrency - previousCount);
                }
            });
        }

        private void TryHandleMessage(SqlTransaction transaction, ServiceBrokerMessage message)
        {
            logger.Debug("Received message {0} from queue {1}", message.ConversationHandle, ServiceBrokerQueue);
            // NOTE this method _should not_ throw an exception!
            // It is responsible for committing or rolling back the SqlTransaction
            // that was passed in.
            // We can keep track of the openTransaction on the current thread in case we need it.
            SqlServerTransactionManager.SaveTransactionForCurrentTask(transaction);
            try
            {
                // this may throw an exception if we need to rollback
                TryHandleMessageInner(transaction, message);
                // End the conversation and commit
                ServiceBrokerWrapper.EndConversation(transaction, message.ConversationHandle);
                SqlServerTransactionManager.CommitTransactionAndDisposeConnection(transaction);
                faultManager.ClearFailuresForMessage(message.ConversationHandle.ToString());
                logger.Debug("Committed message {0} from queue {1}", message.ConversationHandle, ServiceBrokerQueue);
            }
            catch (Exception e)
            {
                SqlServerTransactionManager.TryRollbackTransaction(transaction);
                faultManager.IncrementFailuresForMessage(message.ConversationHandle.ToString(), e);
                logger.Error(e, "Exception caught handling message {0} from queue {1}. Rolling back transaction to retry.", message.ConversationHandle, ServiceBrokerQueue);
#if DEBUG
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    // System.Diagnostics.Debugger.Break();
                }
#endif
            }
            finally
            {
                SqlServerTransactionManager.ClearTransactionForCurrentTask();
                SqlServerTransactionManager.TryForceDisposeTransactionAndConnection(transaction); // shoud not throw exception
            }
        }

        private void TryHandleMessageInner(SqlTransaction transaction, ServiceBrokerMessage message)
        {
            // Only handle transport messages
            if (message.MessageTypeName != ServiceBrokerMessageType
                && !message.IsDialogTimerMessage())
            {
                logger.Debug("Ignoring message of type {0} from queue {1}", message.MessageTypeName, ServiceBrokerQueue);
                return; // ignore
            }
            IncomingTransportMessage transportMessage = null;
            try
            {
                if (message.IsDialogTimerMessage())
                {
                    //transportMessage = TryLoadDialogTimerTimeoutMessage(transaction, message);
                    throw new NotImplementedException();
                }
                else
                {
                    transportMessage = TryDeserializeTransportMessage(message);
                }

                if (!transportMessage.Headers.ContainsKey(StandardHeaders.OriginatingAddress))
                {
                    transportMessage.Headers[StandardHeaders.OriginatingAddress] = ServiceBrokerWrapper.TryGetConversationFarService(transaction, message.ConversationHandle);
                }

                Exception lastException = null;
                if (faultManager.HasMaxRetriesExceeded(transportMessage, out lastException))
                {
                    logger.Debug("MaxRetriesExceeded: Moving message {0} from queue {1} to poison message table.", message.ConversationHandle.ToString(), ServiceBrokerQueue);
                    MoveToPoisonMessage(transaction, message, transportMessage, lastException, "MaxRetriesExceeded", MaxRetries);
                    return; // return without error to commit transaction
                }
                logger.Debug("Notifying observers of new TransportMessage for message {0} from queue {1}.", message.ConversationHandle, ServiceBrokerQueue);
                this.MessageAvailable?.Invoke(this, new MessageAvailableEventArgs(transportMessage));
            }
            catch (CannotDeserializeMessageException e)
            {
                // If we can't deserialize the transport message or inner message, there is no reason to retry.
                // note: error is already logged so we don't need to log here
                logger.Debug("DeserializeError: Moving message {0} from queue {1} to poison message table.", message.ConversationHandle.ToString(), ServiceBrokerQueue);
                MoveToPoisonMessage(transaction, message, transportMessage, e, "DeserializeError", 0);
                return; // return without error to commit transaction
            }
        }

        private IncomingTransportMessage TryDeserializeTransportMessage(ServiceBrokerMessage message)
        {
            using (var stream = new MemoryStream(message.Body))
            {
                try
                {
                    var payload = FastXmlTransportMessageSerializer.Deserialize(stream);
                    var enclosedMessageTypes = payload.Headers[StandardHeaders.EnclosedMessageTypes] as string ?? "";
                    Type messageType = null;
                    foreach (var typeName in enclosedMessageTypes.Split(','))
                    {
                        messageType = messageMapper.GetMappedTypeFor(typeName);
                        if (messageType != null)
                        {
                            break;
                        }
                    }
                    if (enclosedMessageTypes == null)
                    {
                        var errorMessage = "Cannot decode type: " + enclosedMessageTypes;
                        logger.Error(errorMessage);
                        throw new CannotDeserializeMessageException(errorMessage);
                    }
                    var decodedMessage = messageEncoder.Decode(Encoding.UTF8.GetBytes(payload.Body), messageType);
                    return new IncomingTransportMessage(message.ConversationHandle.ToString(), payload.Headers, decodedMessage);
                }
                catch (Exception e)
                {
                    logger.Error(e, "Cannot deserialize transport message for message {0} from queue {1}.", message.ConversationHandle, ServiceBrokerQueue);
                    throw new CannotDeserializeMessageException(e);
                }
            }
        }

        private void MoveToPoisonMessage(SqlTransaction transaction
            , ServiceBrokerMessage serviceBrokerMessage
            , IncomingTransportMessage transportMessage
            , Exception exception
            , string errorCode
            , int retries)
        {
            var origin_service_name = "";
            if (transportMessage != null && transportMessage.Headers.ContainsKey(StandardHeaders.OriginatingAddress))
            {
                origin_service_name = transportMessage.Headers[StandardHeaders.OriginatingAddress];
            }
            else if (transportMessage != null && transportMessage.Headers.ContainsKey(StandardHeaders.ReplyToAddress))
            {
                origin_service_name = transportMessage.Headers[StandardHeaders.ReplyToAddress];
            }
            else
            {
                origin_service_name = ServiceBrokerWrapper.TryGetConversationFarService(transaction, serviceBrokerMessage.ConversationHandle);
            }
            var enqueuedDateTime = DateTime.MinValue;
            if (transportMessage != null && transportMessage.Headers.ContainsKey(StandardHeaders.TimeSent))
            {
                DateTime.TryParse(transportMessage.Headers[StandardHeaders.TimeSent], out enqueuedDateTime);
            }
            try
            {
                PoisonMessageSqlHelper.WriteToPoisonMessageTable(
                    transaction
                    , serviceBrokerMessage.ConversationHandle
                    , origin_service_name
                    , enqueuedDateTime
                    , ServiceBrokerService
                    , ServiceBrokerQueue
                    , serviceBrokerMessage.Body
                    , MaxRetries
                    , errorCode
                    , exception);
            }
            catch (Exception e)
            {
                logger.Fatal(e, "Failed to write PoisonMessage for message {0} from queue {1}", serviceBrokerMessage.ConversationHandle, ServiceBrokerQueue);
                // suppress -- don't let this exception take down the process
            }
            PoisonMessageDetected?.Invoke(this, new PoisonMessageDetectedEventArgs()
            {
                MessageId = serviceBrokerMessage.ConversationHandle.ToString(),
                OriginServiceName = origin_service_name,
                ServiceName = this.ServiceBrokerService,
                QueueName = this.ServiceBrokerQueue,
                MessageBody = serviceBrokerMessage.Body,
                Retries = retries,
                ErrorCode = errorCode,
                Exception = exception,
            });
        }
    }
}
