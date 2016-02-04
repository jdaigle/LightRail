using System;
using System.Threading;
using LightRail.Server.Network;
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

            new TcpListener().Start();

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
