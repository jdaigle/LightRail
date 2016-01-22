using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LightRail.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(2);
            var sw = new Stopwatch();
            var times = new List<double>();

            var manager = new QueueManager();


            while (true)
            {

                times.Clear();
                Debug.Assert(manager.GetOrCreateQueue("eventQueue").Count == 0);
                for (int i = 0; i < 100 * 1000; i++)
                {
                    sw.Restart();
                    var message = new QueuedMessage(Guid.Empty, i, DateTime.UtcNow);
                    manager.GetOrCreateQueue("eventQueue").Enqueue(message);
                    sw.Stop();
                    times.Add(sw.Elapsed.TotalMilliseconds);
                }
                Console.WriteLine($"Avg Enqueue Time 1= {times.Average()}");

                times.Clear();
                for (int i = 0; i < 100 * 1000; i++)
                {
                    sw.Restart();
                    var m = manager.GetOrCreateQueue("eventQueue").TryDequeue();
                    sw.Stop();
                    Debug.Assert(i == (int)m.Body, $"Fail: {i} != {(int)m.Body}!");
                    times.Add(sw.Elapsed.TotalMilliseconds);
                }
                Debug.Assert(manager.GetOrCreateQueue("eventQueue").Count == 0);
                Console.WriteLine($"Avg Dequeue Time 1= {times.Average()}");
            }


        }
    }
}
