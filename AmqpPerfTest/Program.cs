using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amqp;

namespace AmqpPerfTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var amqpAddress = new Address("localhost", 5672, user: null, password: null, scheme: "AMQP");

            Connection connection = null;
            Session session = null;
            ReceiverLink receiverLink = null;
            try
            {
                try
                {
                    if (connection == null)
                    {
                        Console.WriteLine("Connection Opening");
                        connection = new Connection(amqpAddress);
                        connection.Closed = (sender, error) =>
                        {
                            connection = null;
                            session = null;
                            receiverLink = null;
                            Console.WriteLine("Connection Opening");
                            if (error != null)
                            {
                                Console.Error.WriteLine("Connection Closed With Error: " + error.Condition + " "+error.Condition);
                            }
                        };
                    }
                    if (session == null)
                    {
                        Console.WriteLine("Connection Beginning");
                        session = new Session(connection);
                        session.Closed = (sender, error) =>
                        {
                            session = null;
                            receiverLink = null;
                            Console.WriteLine("Session Ended");
                            if (error != null)
                            {
                                Console.Error.WriteLine("Session Ended With Error: " + error.Condition + " " + error.Condition);
                            }
                        };
                    }
                    if (receiverLink == null)
                    {
                        Console.WriteLine("Link Attaching");
                        receiverLink = new ReceiverLink(session, Guid.NewGuid().ToString(), "TestQueue1");
                        receiverLink.Closed = (sender, error) =>
                        {
                            session = null;
                            Console.WriteLine("Link Detached");
                            if (error != null)
                            {
                                Console.Error.WriteLine("Link Detached With Error: " + error.Condition + " " + error.Condition);
                            }
                        };
                    }

                    if (receiverLink != null)
                    {
                        Console.Write("Starting Receive");
                        var amqpMessage = receiverLink.Receive(10000);
                        if (amqpMessage != null)
                        {
                            Console.Write("Received Message");
                            Thread.Sleep(1000);
                            Console.Write("Accepting Message");
                            receiverLink.Accept(amqpMessage);
                        }
                    }
                }
                catch (Exception fatalException)
                {
                    Console.Error.WriteLine("Fatal Exception: " + fatalException);
                    try
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
                    catch (Exception) { } // intentionally swallow
                    Thread.Sleep(10000);
                }
            }
            finally
            {
                try
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
                catch (Exception) { } // intentionally swallow
            }
            Console.WriteLine("Press Any Key TO Exit");
            Console.ReadKey();
        }
    }
}