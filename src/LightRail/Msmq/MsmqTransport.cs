using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Messaging;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LightRail.Logging;

namespace LightRail.Msmq
{
    public class MsmqTransport : ITransport
    {
        public MsmqTransport(LightRailConfiguration config, MsmqTransportConfiguration transportConfiguration)
        {
            InputQueue = transportConfiguration.InputQueue;
            if (string.IsNullOrWhiteSpace(InputQueue))
            {
                throw new InvalidConfigurationException("InputQueue cannot be Null or WhiteSpace");
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

        public string InputQueue { get; private set; }
        public string ErrorQueue { get; private set; }

        public int MaxRetries { get; private set; }
        public int MaxConcurrency { get; private set; }

        string ITransport.OriginatingAddress { get { return InputQueue; } }

        private static ILogger logger = LogManager.GetLogger("LightRail.Msmq");

        private readonly Semaphore workerThreadPool;
        private readonly TransportMessageFaultManager faultManager;

        private bool hasStarted;
        private readonly object startLock = new object();

        private MessageQueue localQueue;
        private MessageQueue errorQueue;

        [ThreadStatic]
        private static MessageQueueTransaction openTransaction;

        void ITransport.Start()
        {
            if (hasStarted)
            {
                return;
            }
            lock (startLock)
            {
                if (hasStarted) return;

                InitQueue();

                if (ErrorQueue != null)
                {
                    errorQueue = new MessageQueue(MsmqUtilities.GetFullPath(ErrorQueue));
                }

                localQueue = new MessageQueue(MsmqUtilities.GetFullPath(InputQueue));
                var mpf = new MessagePropertyFilter();
                mpf.SetAll();
                localQueue.MessageReadPropertyFilter = mpf;

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

        private void InitQueue()
        {
            if (string.IsNullOrEmpty(InputQueue))
                return;

            var machine = MsmqUtilities.GetMachineNameFromLogicalName(InputQueue);

            if (machine.ToLower() != Environment.MachineName.ToLower())
            {
                throw new InvalidOperationException("Input queue must be on the same machine as this process.");
            }

            MsmqUtilities.CreateQueueIfNecessary(InputQueue);
            MsmqUtilities.CreateQueueIfNecessary(ErrorQueue);
        }

        private void LoopAndReceiveMessage()
        {
            logger.Info("Receiving messages on queue {0}", InputQueue);
            while (hasStarted)
            {
                workerThreadPool.WaitOne(); // will block if reached max level of concurrency
                TryReceiveMessage();
            }
        }

        private void TryReceiveMessage()
        {
            if (!MessageInQueue())
            {
                workerThreadPool.Release(1);
                return;
            }
            // NOTE this method _should not_ throw an exception!
            Message message = null;
            MessageQueueTransaction transaction = null;
            try
            {
                transaction = new MessageQueueTransaction();
                transaction.Begin();
                logger.Debug("RECEIVE FROM {0}", InputQueue);
                message = localQueue.Receive(TimeSpan.FromMilliseconds(10), transaction);
            }
            catch (Exception e)
            {
                logger.Error(e, "Exception caught trying to receive from queue {0}. Rolling back and backing off before reconnecting.", InputQueue);
                TryAbortTransaction(transaction);
                TryDisposeTransaction(transaction);
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
                    TryDisposeTransaction(transaction);
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

        private void TryDisposeTransaction(MessageQueueTransaction transaction)
        {
            try
            {
                if (transaction != null)
                {
                    transaction.Dispose();
                }
            }
            catch (Exception) { }
        }

        private void TryAbortTransaction(MessageQueueTransaction transaction)
        {
            try
            {
                if (transaction != null && transaction.Status == MessageQueueTransactionStatus.Pending)
                {
                    transaction.Abort();
                }
            }
            catch (Exception) { }
        }

        private T ExecuteMessageQueueOperation<T>(Func<T> function, T errorValue)
        {
            try
            {
                return function();
            }
            catch (MessageQueueException mqe)
            {
                if (mqe.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                {
                    return errorValue;
                }
                if (mqe.MessageQueueErrorCode == MessageQueueErrorCode.AccessDenied)
                {
                    logger.Fatal(mqe, "Do not have permission to access queue [{0}]. Make sure that the current user [{1}] has permission to Send, Receive, and Peek  from this queue.", InputQueue, WindowsIdentity.GetCurrent() != null ? WindowsIdentity.GetCurrent().Name : "unknown user");
                    return errorValue;
                }
                logger.Error(mqe, "Error executing operation on message queue: " + Enum.GetName(typeof(MessageQueueErrorCode), mqe.MessageQueueErrorCode));
                return errorValue;
            }
            catch (ObjectDisposedException ode)
            {
                logger.Fatal(ode, "Queue has been disposed. Cannot continue operation. Please restart this process.");
                return errorValue;
            }
            catch (Exception e)
            {
                logger.Error(e, "Error executing operation on message queue.");
                return errorValue;
            }
        }

        private bool MessageInQueue()
        {
            return ExecuteMessageQueueOperation(() =>
            {
                localQueue.Peek(TimeSpan.FromSeconds(10));
                return true;
            }, false);
        }

        private Message ReceiveMessageFromQueueAfterPeekWasSuccessful(MessageQueueTransaction transaction)
        {
            return ExecuteMessageQueueOperation(() =>
            {
                return localQueue.Receive(TimeSpan.FromSeconds(10), transaction);
            }, null);
        }

        private void TryHandleMessage(MessageQueueTransaction transaction, Message message)
        {
            logger.Debug("Received message {0} from queue {1}", message.Id, localQueue.QueueName);
            // NOTE this method _should not_ throw an exception!
            // It is responsible for committing or rolling back the SqlTransaction
            // that was passed in.
            // We can keep track of the openTransaction on the current thread in case we need it.
            openTransaction = transaction;
            try
            {
                // this may throw an exception if we need to rollback
                TryHandleMessageInner(transaction, message);
                transaction.Commit();
                //faultManager.ClearFailuresForMessage(message.ConversationHandle.ToString());
                logger.Debug("Committed message {0} from queue {1}", message.Id, localQueue.QueueName);
            }
            catch (Exception e)
            {
                TryAbortTransaction(transaction);
                //faultManager.IncrementFailuresForMessage(message.ConversationHandle.ToString(), e);
                logger.Error(e, "Exception caught handling message {0} from queue {1}. Rolling back transaction to retry.", message.Id, localQueue.QueueName);
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
                TryDisposeTransaction(transaction); // shoud not throw exception
            }
        }

        private void TryHandleMessageInner(MessageQueueTransaction transaction, Message message)
        {
            IncomingTransportMessage transportMessage = null;
            try
            {
                transportMessage = TryConvertTransportMessage(message);

                if (!transportMessage.Headers.ContainsKey(StandardHeaders.OriginatingAddress))
                {
                    transportMessage.Headers[StandardHeaders.OriginatingAddress] = message.ResponseQueue.QueueName;
                }

                Exception lastException = null;
                if (faultManager.MaxRetriesExceeded(transportMessage, out lastException))
                {
                    logger.Debug("MaxRetriesExceeded: Moving message {0} from queue {1} to poison message table.", message.Id, localQueue.QueueName);
                    MoveToPoisonMessage(transaction, message, transportMessage, lastException, "MaxRetriesExceeded", MaxRetries);
                    return; // return without error to commit transaction
                }
                logger.Debug("Notifying observers of new TransportMessage for message {0} from queue {1}.", message.Id, localQueue.QueueName);
                if (this.MessageAvailable != null)
                {
                    this.MessageAvailable(this, new MessageAvailable(transportMessage));
                }
            }
            catch (CannotDeserializeMessageException e)
            {
                // If we can't deserialize the transport message or inner message, there is no reason to retry.
                // note: error is already logged so we don't need to log here
                logger.Debug("DeserializeError: Moving message {0} from queue {1} to poison message table.", message.Id, localQueue.QueueName);
                MoveToPoisonMessage(transaction, message, transportMessage, e, "DeserializeError", 0);
                return; // return without error to commit transaction
            }
        }

        private void MoveToPoisonMessage(MessageQueueTransaction transaction
            , Message message
            , IncomingTransportMessage transportMessage
            , Exception e
            , string v1, int v2)
        {
            PoisonMessageDetected(null, null);
            throw new NotImplementedException();
        }

        private IncomingTransportMessage TryConvertTransportMessage(Message message)
        {
            try
            {
                return FastXmlTransportMessageSerializer.Deserialize(message.Id, message.BodyStream);
            }
            catch (Exception e)
            {
                logger.Error(e, "Cannot deserialize transport message for message {0} from queue {1}.", message.Id, localQueue.QueueName);
                throw new CannotDeserializeMessageException(e);
            }
        }

        public void Send(OutgoingTransportMessage transportMessage, IEnumerable<string> destinations)
        {
            var transaction = openTransaction; // check for "ambient" transaction on current thread
            var commitAndDisposeTransaction = false;
            if (transaction == null)
            {
                transaction = new MessageQueueTransaction();
                transaction.Begin();
                commitAndDisposeTransaction = true;
            }
            try
            {
                using (var stream = new MemoryStream())
                {
                    var message = new Message();
                    FastXmlTransportMessageSerializer.Serialize(transportMessage, stream);
                    stream.Position = 0;
                    message.BodyStream = stream;
                    message.Recoverable = true;
                    message.ResponseQueue = new MessageQueue(MsmqUtilities.GetFullPath(InputQueue));

                    localQueue.Send(message, transaction);
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
                    TryDisposeTransaction(transaction);
                }
            }
        }

        public string RequestTimeoutMessage(int secondsToWait, OutgoingTransportMessage transportMessage)
        {
            throw new NotImplementedException();
        }

        public void ClearTimeout(string timeoutCorrelationID)
        {
            throw new NotImplementedException();
        }

        public int PeekCount()
        {
            throw new NotImplementedException();
            //var qMgmt = new MSMQ.MSMQManagementClass();
            //object machine = Environment.MachineName;
            //var missing = Type.Missing;
            //object formatName = queue.FormatName;

            //qMgmt.Init(ref machine, ref missing, ref formatName);
            //return qMgmt.MessageCount;
        }

        public event EventHandler<MessageAvailable> MessageAvailable;
        public event EventHandler<PoisonMessageDetectedEventArgs> PoisonMessageDetected;

    }
}
