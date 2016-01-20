using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Amqp;
using LightRail.Client.Amqp.Config;
using LightRail.Client.Config;
using LightRail.Client.Logging;
using LightRail.Client.Transport;

namespace LightRail.Client.Amqp
{
    public class AmqpTransportReceiver : ITransportReceiver
    {
        public AmqpTransportReceiver(AmqpHost host, AmqpMessageReceiverConfiguration config, AmqpServiceBusConfiguration serviceBusConfig)
        {
            this.host = host;
            QueueName = config.Address;
            MaxRetries = config.MaxRetries;
            if (MaxRetries < 0)
            {
                MaxRetries = 0;
            }
            MaxConcurrency = config.MaxConcurrency;
            if (MaxConcurrency < 0)
            {
                MaxConcurrency = 0;
            }
            faultManager = new TransportMessageFaultManager(MaxRetries);
        }

        private static ILogger logger = LogManager.GetLogger("LightRail.Client.Amqp");

        public event EventHandler<MessageAvailableEventArgs> MessageAvailable;
        public event EventHandler<PoisonMessageDetectedEventArgs> PoisonMessageDetected;

        private readonly AmqpHost host;
        public string QueueName { get; }

        public int MaxRetries { get; }
        public int MaxConcurrency { get; }
        private readonly TransportMessageFaultManager faultManager;

        private bool hasStarted;
        private readonly object startLock = new object();

        public void Start()
        {
            if (hasStarted)
            {
                return;
            }
            lock (startLock)
            {
                if (hasStarted)
                {
                    throw new InvalidOperationException("Transport Receiver Already Started");
                }

                // cannot init queues

                for (int threadIndex = 0; threadIndex < MaxConcurrency; threadIndex++)
                {
                    Task.Factory.StartNew(LoopAndReceiveMessage, threadIndex.ToString(), TaskCreationOptions.LongRunning);
                }

                hasStarted = true;
            }
        }

        public void Stop(TimeSpan timeSpan)
        {
            hasStarted = false;
        }

        private void LoopAndReceiveMessage(object threadIndex)
        {
            var threadName = $"Receiver[{QueueName}][{threadIndex}]";
            logger.Info("{0} Started", threadName);
            Connection connection = null;
            Session session = null;
            ReceiverLink receiverLink = null;
            try
            {
                while (hasStarted)
                {
                    if (connection == null)
                    {
                        logger.Debug("{0}: Connection Opening", threadName);
                        connection = new Connection(host.Address);
                        connection.Closed = (sender, error) =>
                        {
                            connection = null;
                            session = null;
                            receiverLink = null;
                            logger.Debug("{0}: Connection Closed", threadName);
                            if (error != null)
                            {
                                logger.Error("{0}: Connection Closed With Error: {1}", threadName, error.Description);
                            }
                        };
                    }
                    if (session == null)
                    {
                        logger.Debug("{0}: Session Beginning", threadName);
                        session = new Session(connection);
                        session.Closed = (sender, error) =>
                        {
                            session = null;
                            receiverLink = null;
                            logger.Debug("{0}: Session Ended", threadName);
                            if (error != null)
                            {
                                logger.Error("{0}: Session Ended With Error: {1}", threadName, error.Description);
                            }
                        };
                    }
                    if (receiverLink == null)
                    {
                        logger.Debug("{0}: Link Attaching", threadName);
                        receiverLink = new ReceiverLink(session, threadName, QueueName);
                        receiverLink.Closed = (sender, error) =>
                        {
                            session = null;
                            logger.Debug("{0}: Link Detached", threadName);
                            if (error != null)
                            {
                                logger.Error("{0}: Link Detached With Error: {1}", threadName, error.Description);
                            }
                        };
                    }
                    TryReceiveMessage(receiverLink);
                }
            }
            finally
            {
                if (receiverLink != null)
                {
                    receiverLink.Close();
                }
                if (session != null)
                {
                    session.Close();
                }
                if (connection != null)
                {
                    connection.Close();
                }
            }
        }

        private void TryReceiveMessage(ReceiverLink receiverLink)
        {
            var threadName = receiverLink.Name;
            var sw = Stopwatch.StartNew();
            var amqpMessage = receiverLink.Receive(10000);
            sw.Stop();

            if (amqpMessage == null)
            {
                logger.Debug("{0}: Receive() timed out after {1}ms", threadName, sw.Elapsed.TotalMilliseconds);
                return;
            }

            logger.Debug("{0}: Receive() succeeded after {1}ms", threadName, sw.Elapsed.TotalMilliseconds);

            // TryHandleHessage _should not_ throw an exception.
            TryHandleMessage(receiverLink, amqpMessage);
        }

        private void TryHandleMessage(ReceiverLink receiverLink, Message amqpMessage)
        {
            // NOTE this method _should not_ throw an exception!
            var threadName = receiverLink.Name;
            var messageID = amqpMessage.Properties?.MessageId ?? "?";
            try
            {
                logger.Debug("{0}: Received Message with MessageId={1}", threadName, messageID);

                var headers = new Dictionary<string, string>();
                if (amqpMessage.ApplicationProperties != null)
                {
                    foreach (var key in amqpMessage.ApplicationProperties.Map.Keys)
                    {
                        headers[key.ToString()] = amqpMessage.ApplicationProperties.Map[key].ToString();
                    }
                }

                // TODO: decode message
                var transportMessage = new IncomingTransportMessage(messageID, headers, amqpMessage.Body);

                Exception lastException = null;
                if (faultManager.HasMaxRetriesExceeded(transportMessage, out lastException))
                {
                    logger.Info("{0}: MaxRetriesExceeded for MessageId={1}. Will not re-enque.", threadName, messageID.ToString());
                    OnPoisonMessageDetected(new PoisonMessageDetectedEventArgs()
                    {
                        QueueName = QueueName,
                        Retries = MaxRetries,
                        Exception = lastException,
                        MessageId = messageID,
                        ErrorCode = "MaxRetriesExceeded",
                    });
                }
                else
                {
                    logger.Debug("{0}: Notifying observers of new TransportMessage for MessageId={1}.", threadName, messageID.ToString());
                    OnMessageAvailable(transportMessage);
                    faultManager.ClearFailuresForMessage(messageID);
                }
                receiverLink.Accept(amqpMessage);
                logger.Debug("{0}: Accepting MessageId={1}", threadName, messageID.ToString());
            }
            catch (Exception e)
            {
                faultManager.IncrementFailuresForMessage(messageID.ToString(), e);
                receiverLink.Reject(amqpMessage);
                logger.Error(e, "{0}: Exception caught handling MessageId={1}. Rejecting.", threadName, messageID.ToString());
                Thread.Sleep(1000); // TODO possibly implement a backoff with the fault manager based on number of retries?
            }
        }

        private void OnMessageAvailable(IncomingTransportMessage transportMessage)
        {
            var callback = MessageAvailable;
            if (callback != null)
            {
                callback(this, new MessageAvailableEventArgs(transportMessage));
            }
        }

        private void OnPoisonMessageDetected(PoisonMessageDetectedEventArgs args)
        {
            var callback = PoisonMessageDetected;
            if (callback != null)
            {
                callback(this, args);
            }
        }
    }
}
