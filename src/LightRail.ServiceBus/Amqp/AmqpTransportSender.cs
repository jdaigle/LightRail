using System;
using System.Linq;
using System.Collections.Generic;
using Amqp;
using Amqp.Framing;
using LightRail.ServiceBus.Amqp.Config;
using LightRail.ServiceBus.Logging;
using LightRail.ServiceBus.Transport;

namespace LightRail.ServiceBus.Amqp
{
    public class AmqpTransportSender : ITransportSender
    {
        public AmqpTransportSender(AmqpServiceBusConfiguration config)
        {
            amqpAddress = config.AmqpAddress;
            messageEncoder = config.MessageEncoder;
            messageMapper = config.MessageMapper;
        }

        private static ILogger logger = LogManager.GetLogger("LightRail.ServiceBus.Amqp");
        private readonly Address amqpAddress;
        private readonly IMessageEncoder messageEncoder;
        private readonly IMessageMapper messageMapper;

        public void Send(OutgoingTransportMessage transportMessage, IEnumerable<string> addresses)
        {
            var messageBuffer = messageEncoder.Encode(transportMessage.Message);

            var message = new Message(messageBuffer);
            message.Header = new Header();
            message.Header.Durable = true;
            message.Properties = new Properties();
            message.Properties.CreationTime = DateTime.UtcNow;
            message.Properties.MessageId = Guid.NewGuid().ToString();
            message.Properties.ReplyTo = "TODO";
            message.ApplicationProperties = new ApplicationProperties();
            message.ApplicationProperties["LightRail.ContentType"] = messageEncoder.ContentType;
            message.ApplicationProperties["LightRail.EnclosedMessageTypes"] = string.Join(",", messageMapper.GetEnclosedMessageTypes(transportMessage.Message.GetType()).Distinct());
            foreach (var pair in transportMessage.Headers)
            {
                message.ApplicationProperties[pair.Key] = pair.Value;
            }

            var connection = new Connection(amqpAddress);
            var session = new Session(connection);
            // Azure does not support Amqp transactions "The server was unable to process the request; please retry the operation. If the problem persists, please contact your Service Bus administrator and provide the tracking id..TrackingId:583da4f8d58d4fa59dc9521c6f799cb8_GWIN-AN5B307EEHM,TimeStamp:11.7.2014. 7:44:17"
            try
            {
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
            }
            finally
            {
                session.Close();
                connection.Close();
            }
        }
    }
}
