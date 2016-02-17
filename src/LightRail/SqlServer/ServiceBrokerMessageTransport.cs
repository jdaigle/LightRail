using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LightRail.Logging;
using LightRail.Util;

namespace LightRail.SqlServer
{
    public class ServiceBrokerMessageTransport : ITransport
    {
        public ServiceBrokerMessageTransport(LightRailConfiguration config, ServiceBrokerMessageTransportConfiguration transportConfiguration)
        {
            ServiceBrokerMessageType = transportConfiguration.ServiceBrokerMessageType;
            if (string.IsNullOrWhiteSpace(ServiceBrokerMessageType))
            {
                throw new InvalidConfigurationException("ServiceBrokerMessageType cannot be Null or WhiteSpace");
            }
            ServiceBrokerContract = transportConfiguration.ServiceBrokerContract;
            if (string.IsNullOrWhiteSpace(ServiceBrokerContract))
            {
                throw new InvalidConfigurationException("ServiceBrokerContract cannot be Null or WhiteSpace");
            }
            ServiceBrokerQueue = transportConfiguration.ServiceBrokerQueue;
            if (string.IsNullOrWhiteSpace(ServiceBrokerQueue))
            {
                throw new InvalidConfigurationException("ServiceBrokerQueue cannot be Null or WhiteSpace");
            }
            ServiceBrokerService = transportConfiguration.ServiceBrokerService;
            if (string.IsNullOrWhiteSpace(ServiceBrokerService))
            {
                throw new InvalidConfigurationException("ServiceBrokerService cannot be Null or WhiteSpace");
            }
            ConnectionString = transportConfiguration.ServiceBrokerConnectionString;
            if (string.IsNullOrWhiteSpace(ConnectionString) &&
                !string.IsNullOrWhiteSpace(transportConfiguration.ServiceBrokerConnectionStringName) &&
                ConfigurationManager.ConnectionStrings[transportConfiguration.ServiceBrokerConnectionStringName] != null)
            {
                ConnectionString = ConfigurationManager.ConnectionStrings[transportConfiguration.ServiceBrokerConnectionStringName].ConnectionString;
            }
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                throw new InvalidConfigurationException("ServiceBrokerConnectionString cannot be Null or WhiteSpace");
            }
            MaxRetries = transportConfiguration.MaxRetries;
            if (MaxRetries < 0)
            {
                MaxRetries = 0;
            }
            MaxConcurrency = transportConfiguration.MaxConcurrency;
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

        public string ServiceBrokerMessageType { get; private set; }
        public string ServiceBrokerContract { get; private set; }
        public string ServiceBrokerQueue { get; private set; }
        public string ServiceBrokerService { get; private set; }
        public int MaxRetries { get; private set; }
        public int MaxConcurrency { get; private set; }
        public string ConnectionString { get; private set; }

        string ITransport.OriginatingAddress { get { return ServiceBrokerService; } }

        private static ILogger logger = LogManager.GetLogger("LightRail.SqlServer");
        private static readonly int waitTimeout = 15 * 60 * 1000; // wait 15 minutes

        private readonly Semaphore workerThreadPool;
        private readonly TransportMessageFaultManager faultManager;

        private bool hasStarted;
        private readonly object startLock = new object();

        [ThreadStatic]
        private static SqlTransaction openTransaction;

        void ITransport.Start()
        {
            if (hasStarted)
            {
                return;
            }
            lock (startLock)
            {
                if (hasStarted) return;
                InitServiceBroker();
                if (MaxConcurrency > 0)
                {
                    Task.Factory.StartNew(LoopAndReceiveMessage, TaskCreationOptions.LongRunning);
                }
                hasStarted = true;
            }
        }

        void ITransport.Stop()
        {
            throw new NotImplementedException();
        }

        public event EventHandler<MessageAvailable> MessageAvailable;
        public event EventHandler<PoisonMessageDetectedEventArgs> PoisonMessageDetected;

        private SqlConnection OpenConnection()
        {
            var connection = new SqlConnection(ConnectionString);
            connection.Open();
            return connection;
        }

        private SqlTransaction BeginTransaction()
        {
            return OpenConnection().BeginTransaction();
        }

        private void TryRollbackTransaction(SqlTransaction transaction)
        {
            try
            {
                transaction.Rollback();
            }
            catch (Exception) { }
        }

        private void TryDisposeTransactionAndConnection(SqlTransaction transaction)
        {
            if (transaction != null)
            {
                try
                {
                    transaction.Dispose();
                }
                catch (Exception) { }
                if (transaction.Connection != null)
                {
                    try
                    {
                        transaction.Connection.Dispose();
                    }
                    catch (Exception) { }
                }
            }
        }

        private void InitServiceBroker()
        {
            using (var connection = OpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                // Ensure the service and queue exist
                ServiceBrokerWrapper.CreateServiceAndQueue(transaction, ServiceBrokerService, ServiceBrokerQueue, ServiceBrokerMessageType, ServiceBrokerContract);
                transaction.Commit();
            }
        }

        public int PeekCount()
        {
            var transaction = openTransaction; // check for "ambient" transaction on current thread
            var commitAndDisposeTransaction = false;
            if (transaction == null)
            {
                transaction = BeginTransaction();
                commitAndDisposeTransaction = true;
            }
            try
            {
                var count = ServiceBrokerWrapper.QueryMessageCount(transaction, ServiceBrokerQueue, ServiceBrokerMessageType);
                if (commitAndDisposeTransaction)
                {
                    transaction.Commit();
                }
                return count;
            }
            finally
            {
                if (commitAndDisposeTransaction)
                {
                    TryDisposeTransactionAndConnection(transaction);
                }
            }
        }

        private void LoopAndReceiveMessage()
        {
            logger.Info("Receiving messages on queue {0}", ServiceBrokerQueue);
            while (hasStarted)
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
                transaction = BeginTransaction();
                logger.Debug("RECEIVE FROM {0}", ServiceBrokerQueue);
                message = ServiceBrokerWrapper.WaitAndReceive(transaction, ServiceBrokerQueue, waitTimeout);
            }
            catch (Exception e)
            {
                logger.Error(e, "Exception caught trying to receive from queue {0}. Rolling back and backing off before reconnecting.", ServiceBrokerQueue);
                TryRollbackTransaction(transaction);
                TryDisposeTransactionAndConnection(transaction);
                workerThreadPool.Release(1);

                Thread.Sleep(1000); // back-off because we will return and loop immediately
                return;
            }

            // No message? That's okay
            if (message == null)
            {
                try
                {
                    transaction.Commit();
                }
                finally
                {
                    TryDisposeTransactionAndConnection(transaction);
                    workerThreadPool.Release(1);
                }
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
                    workerThreadPool.Release(1); // release this semaphore back to the pool regardless of what happened
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
            openTransaction = transaction;
            try
            {
                // this may throw an exception if we need to rollback
                TryHandleMessageInner(transaction, message);
                // End the conversation and commit
                ServiceBrokerWrapper.EndConversation(transaction, message.ConversationHandle);
                transaction.Commit();
                faultManager.ClearFailuresForMessage(message.ConversationHandle.ToString());
                logger.Debug("Committed message {0} from queue {1}", message.ConversationHandle, ServiceBrokerQueue);
            }
            catch (Exception e)
            {
                TryRollbackTransaction(transaction);
                faultManager.IncrementFailuresForMessage(message.ConversationHandle.ToString(), e);
                logger.Error(e, "Exception caught handling message {0} from queue {1}. Rolling back transaction to retry.", message.ConversationHandle, ServiceBrokerQueue);
#if DEBUG
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    System.Diagnostics.Debugger.Break();
                }
#endif
            }
            finally
            {
                openTransaction = null;
                TryDisposeTransactionAndConnection(transaction); // shoud not throw exception
            }
        }

        private void TryHandleMessageInner(SqlTransaction transaction, ServiceBrokerMessage message)
        {
            // Only handle transport messages
            if (message.MessageTypeName != ServiceBrokerMessageType &&
                !message.IsDialogTimerMessage())
            {
                logger.Debug("Ignoring message of type {0} from queue {1}", message.MessageTypeName, ServiceBrokerQueue);
                return; // ignore
            }
            IncomingTransportMessage transportMessage = null;
            try
            {
                if (message.IsDialogTimerMessage())
                {
                    transportMessage = TryLoadDialogTimerTimeoutMessage(transaction, message);
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
                if (faultManager.MaxRetriesExceeded(transportMessage, out lastException))
                {
                    logger.Debug("MaxRetriesExceeded: Moving message {0} from queue {1} to poison message table.", message.ConversationHandle.ToString(), ServiceBrokerQueue);
                    MoveToPoisonMessage(transaction, message, transportMessage, lastException, "MaxRetriesExceeded", MaxRetries);
                    return; // return without error to commit transaction
                }
                logger.Debug("Notifying observers of new TransportMessage for message {0} from queue {1}.", message.ConversationHandle, ServiceBrokerQueue);
                if (this.MessageAvailable != null)
                {
                    this.MessageAvailable(this, new MessageAvailable(transportMessage));
                }
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

        private IncomingTransportMessage TryLoadDialogTimerTimeoutMessage(SqlTransaction transaction, ServiceBrokerMessage message)
        {
            var messageBuffer = PopTimeoutMessage(transaction, message.ConversationHandle);
            if (messageBuffer == null || messageBuffer.Length == 0)
            {
                var errorMessage = string.Format("Cannot deserialize transport message. DialogTimer TimeoutMessage is missing for for message {0} from queue {1}.", message.ConversationHandle, ServiceBrokerQueue);
                logger.Error(errorMessage);
                throw new CannotDeserializeMessageException(errorMessage);
            }
            using (var stream = new MemoryStream(messageBuffer))
            {
                try
                {
                    return FastXmlTransportMessageSerializer.Deserialize(message.ConversationHandle.ToString(), stream);
                }
                catch (Exception e)
                {
                    logger.Error(e, "Cannot deserialize transport message for message {0} from queue {1}.", message.ConversationHandle, ServiceBrokerQueue);
                    throw new CannotDeserializeMessageException(e);
                }
            }
        }

        private IncomingTransportMessage TryDeserializeTransportMessage(ServiceBrokerMessage message)
        {
            using (var stream = new MemoryStream(message.Body))
            {
                try
                {
                    return FastXmlTransportMessageSerializer.Deserialize(message.ConversationHandle.ToString(), stream);
                }
                catch (Exception e)
                {
                    logger.Error(e, "Cannot deserialize transport message for message {0} from queue {1}.", message.ConversationHandle, ServiceBrokerQueue);
                    throw new CannotDeserializeMessageException(e);
                }
            }
        }

        void ITransport.Send(OutgoingTransportMessage transportMessage, IEnumerable<string> destinations)
        {
            var transaction = openTransaction; // check for "ambient" transaction on current thread
            var commitAndDisposeTransaction = false;
            if (transaction == null)
            {
                transaction = BeginTransaction();
                commitAndDisposeTransaction = true;
            }

            try
            {

                transportMessage.Headers[StandardHeaders.TimeSent] = DateTime.UtcNow.ToString("o");
                transportMessage.Headers[StandardHeaders.ReplyToAddress] = this.ServiceBrokerService;
                transportMessage.Headers[StandardHeaders.OriginatingAddress] = this.ServiceBrokerService;

                using (var stream = new MemoryStream())
                {
                    FastXmlTransportMessageSerializer.Serialize(transportMessage, stream);
                    var messageBuffer = stream.ToArray();
                    foreach (var destination in destinations)
                    {
                        var conversationHandle = ServiceBrokerWrapper.SendOne(transaction, ServiceBrokerService, destination, ServiceBrokerContract, ServiceBrokerMessageType, messageBuffer);
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
                    TryDisposeTransactionAndConnection(transaction);
                }
            }
        }

        string ITransport.RequestTimeoutMessage(int secondsToWait, OutgoingTransportMessage transportMessage)
        {
            var transaction = openTransaction; // check for "ambient" transaction on current thread
            var commitAndDisposeTransaction = false;
            if (transaction == null)
            {
                transaction = BeginTransaction();
                commitAndDisposeTransaction = true;
            }

            try
            {
                transportMessage.Headers[StandardHeaders.TimeSent] = DateTime.UtcNow.ToString("o");
                transportMessage.Headers[StandardHeaders.ReplyToAddress] = this.ServiceBrokerService;
                transportMessage.Headers[StandardHeaders.OriginatingAddress] = this.ServiceBrokerService;

                var conversationHandle = ServiceBrokerWrapper.BeginConversation(transaction, ServiceBrokerService, ServiceBrokerService);
                ServiceBrokerWrapper.BeginTimer(transaction, conversationHandle, secondsToWait);

                using (var stream = new MemoryStream())
                {
                    FastXmlTransportMessageSerializer.Serialize(transportMessage, stream);
                    var messageBuffer = stream.ToArray();
                    PushTimeoutMessage(transaction, conversationHandle, messageBuffer, secondsToWait);
                }

                if (commitAndDisposeTransaction)
                {
                    transaction.Commit();
                }

                return conversationHandle.ToString();
            }
            finally
            {
                if (commitAndDisposeTransaction)
                {
                    TryDisposeTransactionAndConnection(transaction);
                }
            }
        }

        void ITransport.ClearTimeout(string timeoutCorrelationID)
        {
            var transaction = openTransaction; // check for "ambient" transaction on current thread
            var commitAndDisposeTransaction = false;
            if (transaction == null)
            {
                transaction = BeginTransaction();
                commitAndDisposeTransaction = true;
            }

            try
            {
                var conversationHandle = Guid.Parse(timeoutCorrelationID);
                if (PopTimeoutMessage(transaction, conversationHandle) != null)
                {
                    ServiceBrokerWrapper.EndConversation(transaction, conversationHandle);
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
                    TryDisposeTransactionAndConnection(transaction);
                }
            }
        }

        private static byte[] PopTimeoutMessage(SqlTransaction transaction, Guid conversationHandle)
        {
            using (var command = transaction.Connection.CreateCommand())
            {
                command.CommandText = "spPopTimeoutMessage";
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@conversation_handle", conversationHandle);
                command.Transaction = transaction;
                return command.ExecuteScalar() as byte[];
            }
        }

        private static void PushTimeoutMessage(SqlTransaction transaction, Guid conversationHandle, byte[] message_body, int timeoutInSeconds)
        {
            using (var command = transaction.Connection.CreateCommand())
            {
                command.CommandText = "spPushTimeoutMessage";
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@conversation_handle", conversationHandle);
                command.Parameters.AddWithValue("@transport_message_body", message_body);
                command.Parameters.AddWithValue("@TimeoutDuration", timeoutInSeconds);
                command.Transaction = transaction;
                command.ExecuteNonQuery();
            }
        }

        private void MoveToPoisonMessage(SqlTransaction transaction, ServiceBrokerMessage serviceBrokerMessage, IncomingTransportMessage transportMessage, Exception exception, string errorCode, int retries)
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
            try
            {
                // write to the message to the PosionMessage table
                using (var command = transaction.Connection.CreateCommand())
                {
                    command.CommandText = "spInsertPoisonMessage";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@conversation_handle", serviceBrokerMessage.ConversationHandle);
                    command.Parameters.AddWithValue("@origin_service_name", origin_service_name);
                    command.Parameters.AddWithValue("@service_name", this.ServiceBrokerService);
                    command.Parameters.AddWithValue("@queue_name", this.ServiceBrokerQueue);
                    command.Parameters.AddWithValue("@message_body", serviceBrokerMessage.Body);
                    command.Parameters.AddWithValue("@retries", retries);
                    command.Parameters.AddWithValue("@errorCode", errorCode);
                    if (exception != null)
                        command.Parameters.AddWithValue("@errorMessage", FormatErrorMessage(exception));
                    else
                        command.Parameters.AddWithValue("@errorMessage", DBNull.Value);
                    command.Transaction = transaction;
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                logger.Fatal(e, "Failed to write PoisonMessage for message {0} from queue {1}", serviceBrokerMessage.ConversationHandle, ServiceBrokerQueue);
                // suppress -- don't let this exception take down the process
            }
            if (PoisonMessageDetected != null)
            {
                PoisonMessageDetected(this, new PoisonMessageDetectedEventArgs()
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

        private static string FormatErrorMessage(Exception e)
        {
            var message = e.GetType().ToString() + ": " + e.Message + Environment.NewLine + e.StackTrace;
            if (e.InnerException != null)
            {
                message = message + Environment.NewLine + Environment.NewLine + FormatErrorMessage(e.InnerException);
            }
            return message;
        }
    }
}
