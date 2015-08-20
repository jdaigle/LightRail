using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LightRail.Util;

namespace LightRail.SqlServer
{
    public class ServiceBrokerMessageTransport : ITransport, IReceiveMessages, ISendMessages
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
            if (MaxConcurrency <= 0)
            {
                MaxConcurrency = 1;
            }
            workerThreadPool = new Semaphore(MaxConcurrency, MaxConcurrency);
            logger = config.LogManager.GetLogger("LightRail.SqlServer");
        }

        private static readonly int waitTimeout = 15 * 60 * 1000; // wait 15 minutes
        public string ServiceBrokerMessageType { get; set; }
        public string ServiceBrokerContract { get; set; }
        public string ServiceBrokerQueue { get; private set; }
        public string ServiceBrokerService { get; private set; }
        public int MaxRetries { get; private set; }
        public int MaxConcurrency { get; private set; }
        public string ConnectionString { get; private set; }
        private readonly Semaphore workerThreadPool;
        private readonly ILogger logger;

        private bool hasStarted;
        private object startLock = new object();

        private Observable<MessageAvailable> MessageAvailableSubscribers = new Observable<MessageAvailable>();

        void IReceiveMessages.Start()
        {
            if (hasStarted)
            {
                return;
            }
            lock (startLock)
            {
                if (hasStarted) return;
                InitServiceBroker();
                Task.Factory.StartNew(Loop, TaskCreationOptions.LongRunning);
                hasStarted = true;
            }
        }

        void IReceiveMessages.Stop()
        {
            throw new NotImplementedException();
        }

        public IDisposable Subscribe(IObserver<MessageAvailable> observer)
        {
            return MessageAvailableSubscribers.Subscribe(observer);
        }

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

        private void Loop()
        {
            while (hasStarted)
            {
                workerThreadPool.WaitOne(); // will block if worker threads are busy
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
                message = ServiceBrokerWrapper.WaitAndReceive(transaction, this.ServiceBrokerQueue, waitTimeout);
            }
            catch (Exception e)
            {
                logger.Error("Exception caught trying to receive from queue.", e);
                TryRollbackTransaction(transaction);
                TryDisposeTransactionAndConnection(transaction);
                workerThreadPool.Release(1);

                Thread.Sleep(1000); // back-off because we will return and loop immediately
                return;
            }

            // No message? That's okay
            if (message == null)
            {
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
                    workerThreadPool.Release(1); // release this thread from the pool regardless of the what happened
                }
            });
        }

        
        private void TryHandleMessage(SqlTransaction transaction, ServiceBrokerMessage message)
        {
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
            }
            catch (Exception e)
            {
                logger.Error("Exception caught handling message. Rolling back transaction to retry.", e);
                TryRollbackTransaction(transaction);
            }
            finally
            {
                openTransaction = null;
                TryDisposeTransactionAndConnection(transaction);
            }
        }

        private void TryHandleMessageInner(SqlTransaction transaction, ServiceBrokerMessage message)
        {
            // Only handle transport messages
            if (message.MessageTypeName != ServiceBrokerMessageType)
            {
                return; // ignore
            }
            try
            {
                IncomingTransportMessage transportMessage = null;
                using (var stream = new MemoryStream(message.Body))
                {
                    transportMessage = FastXmlServiceBrokerMessageSerializer.Deserialize(message.ConversationHandle.ToString(), stream);
                }
                this.MessageAvailableSubscribers.OnNext(new MessageAvailable(transportMessage));
            }
            catch (Exception)
            {
                // TODO poison message detection
                throw; // exception will get logged by caller
            }
        }

        [ThreadStatic]
        private static SqlTransaction openTransaction;

        void ISendMessages.Send(OutgoingTransportMessage transportMessage, IEnumerable<string> destinations)
        {
            var transaction = openTransaction; // check for "ambient" transaction on current thread
            var commitAndDisposeTransaction = false;
            if (transaction == null)
            {
                transaction = BeginTransaction();
                commitAndDisposeTransaction = true;
            }

            using (var stream = new MemoryStream())
            {
                FastXmlServiceBrokerMessageSerializer.Serialize(transportMessage, stream);
                var messageBuffer = stream.ToArray();
                foreach (var destination in destinations)
                {
                    var conversationHandle = ServiceBrokerWrapper.SendOne(transaction, ServiceBrokerService, destination, ServiceBrokerContract, ServiceBrokerMessageType, messageBuffer);
#if DEBUG
                    logger.Debug(string.Format("Sending message {0} with Handle {1} to Service Named {2}.\n" +
                                               "ToString() of the message yields: {3}\n" +
                                               "Message headers:\n{4}",
                                               transportMessage.Message.GetType().AssemblyQualifiedName,
                                               conversationHandle,
                                               destination,
                                               transportMessage.Message.ToString(),
                                               string.Join(", ", transportMessage.Headers.Select(h => h.Key + ":" + h.Value).ToArray())));
#endif
                }
            }

            try
            {
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
    }
}
