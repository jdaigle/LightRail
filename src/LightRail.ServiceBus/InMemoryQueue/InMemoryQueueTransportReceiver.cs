using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LightRail.ServiceBus.Config;
using LightRail.ServiceBus.InMemoryQueue.Config;
using LightRail.ServiceBus.Logging;
using LightRail.ServiceBus.Transport;

namespace LightRail.ServiceBus.InMemoryQueue
{
    public class InMemoryQueueTransportReceiver : ITransportReceiver
    {
        public InMemoryQueueTransportReceiver(InMemoryQueueMessageReceiverConfiguration config, IServiceBusConfig serviceBusConfig)
        {
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

        private static ILogger logger = LogManager.GetLogger("LightRail.ServiceBus.InMemory");

        internal static readonly ConcurrentDictionary<string, ConcurrentQueue<object>> queues = new ConcurrentDictionary<string, ConcurrentQueue<object>>(StringComparer.InvariantCultureIgnoreCase);
        internal static readonly ConcurrentDictionary<string, AutoResetEvent> queueNotifiers = new ConcurrentDictionary<string, AutoResetEvent>(StringComparer.InvariantCultureIgnoreCase);

        public event EventHandler<MessageAvailableEventArgs> MessageAvailable;
        public event EventHandler<PoisonMessageDetectedEventArgs> PoisonMessageDetected;

        public string QueueName { get; }
        private ConcurrentQueue<object> queue;
        private AutoResetEvent queueNotifier;

        public int MaxRetries { get; }
        public int MaxConcurrency { get; }
        private readonly TransportMessageFaultManager faultManager;

        private bool hasStarted;
        private readonly object startLock = new object();
        private readonly List<Task> receiverThreads = new List<Task>();

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

                // init queue if does not exist
                queue = queues.GetOrAdd(QueueName, new ConcurrentQueue<object>());
                queueNotifier = queueNotifiers.GetOrAdd(QueueName, new AutoResetEvent(false));

                for (int threadIndex = 0; threadIndex < MaxConcurrency; threadIndex++)
                {
                    receiverThreads.Add(Task.Factory.StartNew(LoopAndReceiveMessage, threadIndex.ToString(), TaskCreationOptions.LongRunning));
                }

                hasStarted = true;
            }
        }

        public void Stop(TimeSpan timeSpan)
        {
            hasStarted = false;
            var tasks = receiverThreads.ToArray();
            Task.WaitAll(tasks, timeSpan);
        }

        private void LoopAndReceiveMessage(object threadIndex)
        {
            logger.Info("Receiving messages on in memory queue [{0}]", QueueName);
            while (hasStarted)
            {
                TryReceiveMessage();
            }
        }

        private void TryReceiveMessage()
        {
            object message = null;
            if (!queue.TryDequeue(out message))
            {
                queueNotifier.WaitOne(TimeSpan.FromSeconds(1));
            }
            if (message == null)
            {
                return;
            }
            TryHandleMessage(message);
        }

        private void TryHandleMessage(object message)
        {
            var messageID = MessageId(message).ToString();
            logger.Debug("Received message {0} from queue {1}", messageID, QueueName);
            // NOTE this method _should not_ throw an exception!
            try
            {
                var transportMessage = new IncomingTransportMessage(messageID, new Dictionary<string, string>(), message);
                Exception lastException = null;
                if (faultManager.HasMaxRetriesExceeded(transportMessage, out lastException))
                {
                    logger.Debug("MaxRetriesExceeded. Will not re-enque.", messageID.ToString(), QueueName);
                    OnPoisonMessageDetected(new PoisonMessageDetectedEventArgs()
                    {
                        QueueName = QueueName,
                        Retries = MaxRetries,
                        Exception = lastException,
                        MessageId = messageID,
                        ErrorCode = "MaxRetriesExceeded",
                    });
                    return; // return without error to commit transaction
                }
                logger.Debug("Notifying observers of new TransportMessage for message {0} from queue {1}.", messageID.ToString(), QueueName);
                OnMessageAvailable(transportMessage);
                faultManager.ClearFailuresForMessage(messageID);
                logger.Debug("Committed message {0} from queue {1}", messageID.ToString(), QueueName);
            }
            catch (Exception e)
            {
                faultManager.IncrementFailuresForMessage(messageID.ToString(), e);
                logger.Error(e, "Exception caught handling message {0} from queue {1}. Re-enqueing.", messageID, QueueName);

                Thread.Sleep(1000); // TODO possibly implement a backoff with the fault manager based on number of retries?
                queue.Enqueue(message);
                queueNotifier.Set();
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

        private static System.Runtime.Serialization.ObjectIDGenerator gen = new System.Runtime.Serialization.ObjectIDGenerator();
        private static long MessageId(object message)
        {
            bool firstTime = false;
            return gen.GetId(message, out firstTime);
        }
    }
}
