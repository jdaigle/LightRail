using System;
using System.Text;
using System.Threading;
using Amqp;
using Amqp.Framing;

namespace AmqpPerfTest
{
    class Program
    {
        static Address amqpAddress = new Address("localhost", 5672, user: null, password: null, scheme: "AMQP");
        static Connection connection = null;
        static Session session = null;
        static ReceiverLink receiverLink = null;

        static void Main(string[] args)
        {
            Trace.TraceLevel = TraceLevel.Frame;
            Trace.TraceListener = (f, a) =>
            {
                var t = DateTime.Now.ToString("[hh:ss.fff]") + " " + string.Format(f, a);
                Console.WriteLine(t);
            };

            while (true)
            {

                try
                {
                    try
                    {
                        Console.WriteLine("Connection Opening");
                        connection = new Connection(amqpAddress);
                        connection.Closed = OnClosed;

                        Console.WriteLine("Session Beginning");
                        session = new Session(connection);
                        session.Closed = OnClosed;

                        Thread.Sleep(250);
                        session.Close();
                        Thread.Sleep(250);
                        session = new Session(connection);
                        session.Closed = OnClosed;

                        var linkName = Guid.NewGuid().ToString();
                        var senderLink = new SenderLink(session, linkName, "TestQueue1");
                        senderLink.Closed = OnClosed;

                        Message message = new Message();
                        message.Properties = new Properties();
                        message.Properties.MessageId = Guid.NewGuid().ToString();
                        message.BodySection = new Data() { Binary = Encoding.UTF8.GetBytes("msg " + Guid.NewGuid().ToString()) };
                        senderLink.Send(message);
                        senderLink.Close();

                        //Console.WriteLine("Link Attaching");
                        //receiverLink = new ReceiverLink(session, linkName, "TestQueue1");
                        //receiverLink.Closed = OnClosed;

                        //Thread.Sleep(250);
                        //receiverLink.Close();
                        //Thread.Sleep(250);
                        //receiverLink = new ReceiverLink(session, linkName, "TestQueue1");
                        //receiverLink.Closed = OnClosed;

                        //Thread.Sleep(2000);

                        //if (receiverLink != null)
                        //{
                        //    //Console.Write("Starting Receive");
                        //    var amqpMessage = receiverLink.Receive(60 * 1000);
                        //    if (amqpMessage != null)
                        //    {
                        //        Console.Write("Received Message");
                        //        Thread.Sleep(1000);
                        //        Console.Write("Accepting Message");
                        //        receiverLink.Accept(amqpMessage);
                        //    }
                        //}

                        Thread.Sleep(10 * 1000);
                    }
                    catch (Exception fatalException)
                    {
                        Console.Error.WriteLine("Fatal Exception: " + fatalException);
                        TryClose();
                        Thread.Sleep(10000);
                    }
                }
                finally
                {
                    TryClose();
                }
                Console.WriteLine("Press Any Key To Try Again");
                Console.ReadKey();
                Console.Clear();
            }
        }

        private static void TryClose()
        {
            try
            {
                if (receiverLink != null)
                    receiverLink.Close();
                if (session != null)
                    session.Close();
                if (connection != null)
                    connection.Close();
            }
            catch (Exception) { } // intentionally swallow
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
            Console.WriteLine(sender.GetType() + " Closed");
            if (error != null)
            {
                Console.Error.WriteLine(sender.GetType() + " Closed With Error: " + error.Condition + " " + error.Description);
            }
        }
    }
}