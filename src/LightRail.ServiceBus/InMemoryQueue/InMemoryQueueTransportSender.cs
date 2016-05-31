using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using LightRail.ServiceBus.Config;
using LightRail.ServiceBus.Transport;

namespace LightRail.ServiceBus.InMemoryQueue
{
    public class InMemoryQueueTransportSender : ITransportSender
    {
        public InMemoryQueueTransportSender(BaseServiceBusConfig serviceBusConfig)
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
