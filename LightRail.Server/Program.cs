using System;
using System.Threading;
using LightRail.Server.Queuing;

namespace LightRail.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Trace.EnabledTraceLevel = TraceLevel.Debug;
            Trace.OnTraceEvent += NLogTraceHandler;

            //Thread.CurrentThread.Priority = ThreadPriority.Highest;
            //Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(2);

            var rand = new Random();

            var queue = new ConcurrentQueue(1, new QueueLogWriter());

            new Consumer(queue, entry =>
            {
                Console.WriteLine($"Consumer 1 Delivered Entry {entry.SeqNum.ToString()} attempt number {entry.DeliveryCount + 1}");
                Thread.Sleep(rand.Next(150, 550));
                if (rand.Next(1, 100) % 2 == 0)
                    queue.Archive(entry);
                else
                    queue.Release(entry, true);
            });
            new Consumer(queue, entry =>
            {
                Console.WriteLine($"Consumer 2 Delivered Entry {entry.SeqNum.ToString()} attempt number {entry.DeliveryCount + 1}");
                Thread.Sleep(rand.Next(150, 550));
                if (rand.Next(1, 100) % 2 == 0)
                    queue.Archive(entry);
                else
                    queue.Release(entry, true);
            });

            for (int i = 0; i < 100; i++)
            {
                queue.Enqueue("hello world " + i);
                //Console.WriteLine("Enqueued " + (i+2));
                //if (i > 0 && i % 25 == 0)
                //    Thread.Sleep(5000);
                Thread.Sleep(20);
            }

            //var queue = new LinkedListQueue<string>(0, "");

            //Console.WriteLine("Enqueing 100 items");
            //for (int i = 0; i < 100; i++)
            //{
            //    queue.Enqueue("hello world " + i);
            //}

            //Thread.Sleep(1000);

            //Console.WriteLine("Dequeuing 100 items");
            //var current = queue.GetHead();
            //while (current != null)
            //{
            //    var next = current.GetNextValidEntry();
            //    if (next != null)
            //    {
            //        current = next;
            //        Console.WriteLine("reading: " + current.SeqNum);
            //    }
            //    else
            //    {
            //        break;
            //    }
            //    if (current.SeqNum > 10 && current.SeqNum % 2 == 0)
            //    {
            //        current.IsDeleted = true;
            //        queue.EntryDeleted(current);
            //    }
            //}

            //Console.WriteLine("Dequeuing 100 items again...");
            //current = queue.GetHead();
            //while (current != null)
            //{
            //    var next = current.Next;
            //    if (next != null)
            //    {
            //        current = next;
            //        Console.WriteLine("reading: " + current.SeqNum);
            //    }
            //    else
            //    {
            //        break;
            //    }
            //}

            //new TcpListener().Start();

            //var sw = new Stopwatch();
            //var times = new List<double>();

            //var manager = new QueueManager();


            //while (true)
            //{

            //    times.Clear();
            //    Debug.Assert(manager.GetOrCreateQueue("eventQueue").Count == 0);
            //    for (int i = 0; i < 100 * 1000; i++)
            //    {
            //        sw.Restart();
            //        var message = new QueuedMessage(Guid.Empty, i, DateTime.UtcNow);
            //        manager.GetOrCreateQueue("eventQueue").Enqueue(message);
            //        sw.Stop();
            //        times.Add(sw.Elapsed.TotalMilliseconds);
            //    }
            //    Console.WriteLine($"Avg Enqueue Time 1= {times.Average()}");

            //    times.Clear();
            //    for (int i = 0; i < 100 * 1000; i++)
            //    {
            //        sw.Restart();
            //        var m = manager.GetOrCreateQueue("eventQueue").TryDequeue();
            //        sw.Stop();
            //        Debug.Assert(i == (int)m.Body, $"Fail: {i} != {(int)m.Body}!");
            //        times.Add(sw.Elapsed.TotalMilliseconds);
            //    }
            //    Debug.Assert(manager.GetOrCreateQueue("eventQueue").Count == 0);
            //    Console.WriteLine($"Avg Dequeue Time 1= {times.Average()}");
            //}

            Console.WriteLine("Press Any Key To Exit");
            Console.ReadKey();
        }

        public static void NLogTraceHandler(TraceSource source, TraceLevel traceLevel, Exception exception, string message, params object[] formatArgs)
        {
            var logger = NLog.LogManager.GetLogger(source.Name);
            switch (traceLevel)
            {
                case TraceLevel.Debug:
                    if (exception == null)
                    {
                        logger.Debug(message, formatArgs);
                    }
                    else
                    {
                        logger.Debug(exception, message, formatArgs);
                    }
                    break;
                case TraceLevel.Info:
                    if (exception == null)
                    {
                        logger.Info(message, formatArgs);
                    }
                    else
                    {
                        logger.Info(exception, message, formatArgs);
                    }
                    break;
                case TraceLevel.Warn:
                    if (exception == null)
                    {
                        logger.Warn(message, formatArgs);
                    }
                    else
                    {
                        logger.Warn(exception, message, formatArgs);
                    }
                    break;
                case TraceLevel.Error:
                    if (exception == null)
                    {
                        logger.Error(message, formatArgs);
                    }
                    else
                    {
                        logger.Error(exception, message, formatArgs);
                    }
                    break;
                case TraceLevel.Fatal:
                    if (exception == null)
                    {
                        logger.Fatal(message, formatArgs);
                    }
                    else
                    {
                        logger.Fatal(exception, message, formatArgs);
                    }
                    break;
            }
        }
    }
}
