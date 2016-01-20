using System;
using System.Collections.Generic;
using Amqp;
using Amqp.Framing;
using Amqp.Types;
using LightRail.Client.Amqp.Config;
using LightRail.Client.Logging;
using LightRail.Client.Transport;

namespace LightRail.Client.Amqp
{
    public class AmqpTransportSender : ITransportSender
    {
        public AmqpTransportSender(AmqpHost host, AmqpServiceBusConfiguration config)
        {
            this.host = host;
        }

        private static ILogger logger = LogManager.GetLogger("LightRail.Client.Amqp");
        private readonly AmqpHost host;

        public void Send(OutgoingTransportMessage transportMessage, IEnumerable<string> addresses)
        {
            // TODO: O.M.G. this is terrible
            var map = new Map();
            foreach (var prop in transportMessage.Message.GetType().GetProperties())
            {
                map[prop.Name] = prop.GetValue(transportMessage.Message);
            }

            var message = new Message(map);
            message.Header = new Header();
            message.Header.Durable = true;
            message.Properties = new Properties();
            message.Properties.CreationTime = DateTime.UtcNow;
            message.Properties.MessageId = Guid.NewGuid().ToString();
            message.Properties.ReplyTo = "TODO";
            foreach (var pair in transportMessage.Headers)
            {
                message.ApplicationProperties[pair.Key] = pair.Value;
            }
            var session = host.GetOrOpenSession();
            // Azure does not support Amqp transactions "The server was unable to process the request; please retry the operation. If the problem persists, please contact your Service Bus administrator and provide the tracking id..TrackingId:583da4f8d58d4fa59dc9521c6f799cb8_GWIN-AN5B307EEHM,TimeStamp:11.7.2014. 7:44:17"
            //using (var ts = new TransactionScope())
            //{
            foreach (var address in addresses)
            {
                logger.Info("Sending Message {0} to {1}", message.Properties.MessageId, address);
                var senderLink = new SenderLink(session, Guid.NewGuid().ToString(), address);
                try
                {
                    senderLink.Send(message);
                }
                finally
                {
                    senderLink.Close();
                }
            }
            //ts.Complete();
            //}
        }
    }
}
