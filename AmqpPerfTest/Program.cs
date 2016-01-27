using System;
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

                        Console.WriteLine("Link Attaching");
                        receiverLink = new ReceiverLink(session, Guid.NewGuid().ToString(), "TestQueue1");
                        receiverLink.Closed = OnClosed;

                        Thread.Sleep(2000);

                        if (receiverLink != null)
                        {
                            //Console.Write("Starting Receive");
                            var amqpMessage = receiverLink.Receive(60 * 1000);
                            if (amqpMessage != null)
                            {
                                Console.Write("Received Message");
                                Thread.Sleep(1000);
                                Console.Write("Accepting Message");
                                receiverLink.Accept(amqpMessage);
                            }
                        }

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