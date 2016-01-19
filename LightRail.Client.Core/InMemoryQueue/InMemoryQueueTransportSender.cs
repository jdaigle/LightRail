using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using LightRail.Client.Config;
using LightRail.Client.Transport;

namespace LightRail.Client.InMemoryQueue
{
    public class InMemoryQueueTransportSender : ITransportSender
    {
        public InMemoryQueueTransportSender(IServiceBusConfig serviceBusConfig)
        {

        }

        public void Send(OutgoingTransportMessage transportMessage, IEnumerable<string> addresses)
        {
            foreach (var queueName in addresses)
            {
                var _sendQueue = InMemoryQueueTransportReceiver.queues.GetOrAdd(queueName, new ConcurrentQueue<object>());
                var _sendQueueNotifier = InMemoryQueueTransportReceiver.queueNotifiers.GetOrAdd(queueName, new AutoResetEvent(false));

                _sendQueue.Enqueue(transportMessage.Message);
                _sendQueueNotifier.Set();
            }
        }
    }
}
