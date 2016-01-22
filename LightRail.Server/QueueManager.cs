using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LightRail.Server
{
    public class QueueManager
    {
        private readonly Dictionary<string, LinkedListQueue<QueuedMessage>> queuesByName = new Dictionary<string, LinkedListQueue<QueuedMessage>>();

        private volatile int stopTheWorld = 0;

        public LinkedListQueue<QueuedMessage> GetOrCreateQueue(string path)
        {
            var spinWait = new SpinWait();
            while (Interlocked.CompareExchange(ref stopTheWorld, 1, 0) != 0)
            {
                Thread.MemoryBarrier();
                spinWait.SpinOnce();
            }
            path = path.ToLowerInvariant();
            LinkedListQueue<QueuedMessage> queue;
            if (!queuesByName.TryGetValue(path, out queue))
            {
                queuesByName[path] = queue = new LinkedListQueue<QueuedMessage>(0, path);
            }
            stopTheWorld = 0;
            return queue;
        }
    }
}
