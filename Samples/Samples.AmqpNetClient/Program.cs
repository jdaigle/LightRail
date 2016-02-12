using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amqp;
using Amqp.Framing;

namespace Samples.AmqpNetClient
{
    public static class Program
    {
        static Address amqpAddress = new Address("localhost", 5672, user: null, password: null, scheme: "AMQP");
        static Connection connection = null;
        static Session session = null;
        static SenderLink senderLink = null;
        static ReceiverLink receiverLink = null;

        public static void Main(string[] args)
        {
            Trace.TraceLevel = TraceLevel.Frame;
            Trace.TraceListener = (f, a) =>
            {
                var t = DateTime.Now.ToString("[hh:ss.fff]") + " " + string.Format(f, a);
                Console.WriteLine(t);
            };

            Console.WriteLine("Opening Connection");
            connection = new Connection(amqpAddress, null, new Open()
            {
                ContainerId = Guid.NewGuid().ToString(),
                ChannelMax = 64,
                MaxFrameSize = 200,
            }, null);
            connection.Closed = OnClosed;

            Console.WriteLine("Beginning Session");
            session = new Session(connection);
            session.Closed = OnClosed;

            //System.Threading.Thread.Sleep(2000);

            Console.WriteLine("Attaching Link to Send");
            var linkName = Guid.NewGuid().ToString();
            senderLink = new SenderLink(session, linkName, "TestQueue1");
            senderLink.Closed = OnClosed;

            for (int i = 0; i < 1; i++)
            {
                senderLink.Send(CreateMessage(), 5000);
            }

            senderLink.Close();

            Console.WriteLine("Attaching Link to Receive");
            linkName = Guid.NewGuid().ToString();
            receiverLink = new ReceiverLink(session, linkName, "TestQueue1");
            receiverLink.Closed = OnClosed;

            var message = receiverLink.Receive(20000);
            int receiveCount = 0;
            while(message != null)
            {
                receiveCount++;

                Console.WriteLine("Receive #{0}. Message = \"{1}\"", receiveCount.ToString(), Encoding.UTF8.GetString(message.GetBody<byte[]>()));

                if (receiveCount % 7 == 0)
                    receiverLink.Release(message);
                else if (receiveCount % 4 == 0)
                    receiverLink.Reject(message);
                else
                    receiverLink.Accept(message);
            }

            receiverLink.Close();
            session.Close();
            connection.Close();
        }

        private static Message CreateMessage()
        {
            Message message = new Message();
            message.Properties = new Properties();
            message.Properties.MessageId = Guid.NewGuid().ToString();
            message.MessageAnnotations = new MessageAnnotations();
            message.MessageAnnotations.Map["Test.Header"] = Guid.NewGuid().ToString();
            message.BodySection = new Data() { Binary = Encoding.UTF8.GetBytes("BIG msg " + Guid.NewGuid().ToString() + Guid.NewGuid().ToString() + Guid.NewGuid().ToString() + Guid.NewGuid().ToString() + Guid.NewGuid().ToString() + Guid.NewGuid().ToString() + Guid.NewGuid().ToString() + Guid.NewGuid().ToString() + Guid.NewGuid().ToString() + Guid.NewGuid().ToString() + Guid.NewGuid().ToString() + Guid.NewGuid().ToString()) };
            return message;
        }

        private static void OnClosed(object sender, Error error)
        {
            if (sender is Connection)
            {
                connection = null;
                session = null;
                receiverLink = null;
            }
            if (sender is Session)
            {
                session = null;
                receiverLink = null;
            }
            if (sender is ReceiverLink)
            {
                receiverLink = null;
            }
            if (sender is SenderLink)
            {
                senderLink = null;
            }
            Console.WriteLine(sender.GetType() + " Closed");
            if (error != null)
            {
                Console.Error.WriteLine(sender.GetType() + " Closed With Error: " + error.Condition + " " + error.Description);
            }
        }
    }
}
