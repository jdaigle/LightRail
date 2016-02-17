using System;
using LightRail.Amqp;
using LightRail.Amqp.Messaging;
using LightRail.Amqp.Protocol;
using LightRail.Server.Queuing;

namespace LightRail.Server
{
    public class LinkConsumer : Consumer
    {
        private AmqpLink link;

        public LinkConsumer(ConcurrentQueue queue, AmqpLink link)
            : base(queue)
        {
            this.link = link;
            this.link.ReceivedFlow += LinkReceivedFlow;
        }

        private void LinkReceivedFlow(object sender, EventArgs e)
        {
            queue.SignalConsumerHasCredit();
        }

        protected override bool HasCreditToDeliver()
        {
            return link.LinkCredit > 0;
        }

        protected override void OnMessageAquired(QueueEntry next)
        {
            var message = (AnnotatedMessage)next.Item;
            var payloadBuffer = new ByteBuffer(AnnotatedMessage.GetEstimatedMessageSize(message), false);
            AnnotatedMessage.Encode(message, payloadBuffer);
            var deliveryTag = Guid.NewGuid().ToByteArray();
            link.SendTransfer(deliveryTag, payloadBuffer);
        }
    }
}