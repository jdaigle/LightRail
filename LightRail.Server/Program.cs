using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LightRail.Server.Network;

namespace LightRail.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Trace.EnabledTraceLevel = TraceLevel.Debug;
            Trace.OnTraceEvent += NLogTraceHandler;
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(2);

            new TcpListener().Start();

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
