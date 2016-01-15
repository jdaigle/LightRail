using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using LightRail.Logging;
using System.Threading;
using LightRail.Util;
using System.IO;

namespace LightRail.RabbitMQ
{
    public class RabbitMQTransport : ITransport
    {
        public string WorkQueue { get; private set; }
        public int MaxRetries { get; private set; }
        public int MaxConcurrency { get; private set; }

        private static ILogger logger = LogManager.GetLogger("LightRail.RabbitMQ");
        private static readonly int waitTimeout = 15 * 60 * 1000; // wait 15 minutes

        private readonly CancellationTokenSource tokenSource = new CancellationTokenSource();
        private readonly Semaphore workerThreadPool;
        private readonly TransportMessageFaultManager faultManager;

        public event EventHandler<MessageAvailable> MessageAvailable;
        public event EventHandler<PoisonMessageDetectedEventArgs> PoisonMessageDetected;

        private bool hasStarted;
        private readonly object startLock = new object();

        void ITransport.Start()
        {
            if (hasStarted)
            {
                return;
            }
            lock (startLock)
            {
                if (hasStarted) { return; }

                var token = tokenSource.Token;
                for (var i = 0; i < MaxConcurrency; i++)
                {
                    Task.Factory.StartNew(DequeueMessageLoop, token, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                }
            }
        }

        void ITransport.Stop()
        {
            tokenSource.Cancel();
        }

        private void DequeueMessageLoop(object obj)
        {
            var cancellationToken = (CancellationToken)obj;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    IConnection connection = ConnectionManager.GetConsumeConnection();

                    using (var channel = connection.CreateModel())
                    {
                        channel.BasicQos(0, 0, false);
                        channel.QueueDeclare(queue: WorkQueue, durable: true, exclusive: false, autoDelete: false, arguments: null);
                        var consumer = new QueueingBasicConsumer(channel);
                        channel.BasicConsume(WorkQueue, noAck: true, consumer: consumer);
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            DequeueMessageLoopInner(consumer);
                        }
                    }
                }
                catch (IOException)
                {
                    //Unable to write data to the transport connection: An existing connection was forcibly closed by the remote host.
                    //This exception is expected because we are shutting down!
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Exception Thrown at Top of Message Loop.");
                    Thread.Sleep(1000); // back-off because we loop immediately
                }
            }
        }

        private void DequeueMessageLoopInner(QueueingBasicConsumer consumer)
        {
            var channel = consumer.Model;
            var message = TryDequeueMessage(consumer);
            if (message == null)
            {
                return;
            }

            var messageId = message.BasicProperties.MessageId;

            if (string.IsNullOrWhiteSpace(messageId))
            {
                logger.Warn("Received Message Without an Application Message Id. Cannot Process");
                channel.BasicAck(message.DeliveryTag, false);
                return;
            }

            bool awkMessage = false;
            bool requeueMessage = false;

            try
            {
                IncomingTransportMessage transportMessage = null;
                try
                {
                    transportMessage = ToTransportMessage(message);
                }
                catch (Exception ex)
                {
                    logger.Error("Poison message detected, deliveryTag: " + message.DeliveryTag, ex);
                    // TODO: poison message detection?
                    awkMessage = true;
                }
                if (transportMessage != null)
                {
                    TryProcessMessage(transportMessage);
                    awkMessage = true;
                }
            }
            catch (CannotDeserializeMessageException e)
            {
                // If we can't deserialize the transport message or inner message, there is no reason to retry.
                // note: error is already logged so we don't need to log here
                logger.Debug(e, "DeserializeError: Moving message {0} from queue {1} to poison message table.", messageId, WorkQueue);
                //MoveToPoisonMessage(transaction, message, transportMessage, e, "DeserializeError", 0);
                awkMessage = true;
            }
            catch (Exception ex)
            {
                faultManager.IncrementFailuresForMessage(messageId, ex);
                logger.Error(ex, "Exception caught handling message {0} from queue {1}. Requeuing.", messageId, WorkQueue);
#if DEBUG
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    System.Diagnostics.Debugger.Break();
                }
#endif
                awkMessage = false;
                requeueMessage = true;
            }
            finally
            {
                if (awkMessage)
                {
                    channel.BasicAck(message.DeliveryTag, false);
                }
                else
                {
                    channel.BasicReject(message.DeliveryTag, requeueMessage);
                }
            }
        }

        private void TryProcessMessage(IncomingTransportMessage transportMessage)
        {
            if (!transportMessage.Headers.ContainsKey(StandardHeaders.OriginatingAddress))
            {
                transportMessage.Headers[StandardHeaders.OriginatingAddress] = ServiceBrokerWrapper.TryGetConversationFarService(transaction, message.ConversationHandle);
            }

            Exception lastException = null;
            if (faultManager.MaxRetriesExceeded(transportMessage, out lastException))
            {
                // TODO: poison message handling
                //logger.Debug("MaxRetriesExceeded: Moving message {0} from queue {1} to poison message table.", message.ConversationHandle.ToString(), ServiceBrokerQueue);
                //MoveToPoisonMessage(transaction, message, transportMessage, lastException, "MaxRetriesExceeded", MaxRetries);
                return; // return without error to commit transaction
            }
            //logger.Debug("Notifying observers of new TransportMessage for message {0} from queue {1}.", message.ConversationHandle, ServiceBrokerQueue);
            if (this.MessageAvailable != null)
            {
                this.MessageAvailable(this, new MessageAvailable(transportMessage));
            }
        }

        private static IncomingTransportMessage ToTransportMessage(BasicDeliverEventArgs rMessage)
        {
            var props = rMessage.BasicProperties;
            var transportMessage = new IncomingTransportMessage(
                messageId: props.MessageId
                , headers: props.Headers.ToDictionary(x => x.Key, x => x.Value.ToString())
                , serializedMessagedata: Encoding.UTF8.GetString(rMessage.Body));
            return transportMessage;
        }

        private static BasicDeliverEventArgs TryDequeueMessage(QueueingBasicConsumer consumer)
        {
            try
            {
                BasicDeliverEventArgs message = null;
                consumer.Queue.Dequeue(1000, out message);
                return message;
            }
            catch (EndOfStreamException)
            {
                // If no items are present and the queue is in a closed
                // state, or if at any time while waiting the queue
                // transitions to a closed state (by a call to Close()), this
                // method will throw EndOfStreamException.
                return null;
            }
        }

        private void LoopAndReceiveMessage()
        {
            logger.Info("Receiving messages on queue {0}", WorkQueue);
            while (hasStarted)
            {
                workerThreadPool.WaitOne(); // will block if reached max level of concurrency



            }
        }

    }
}
