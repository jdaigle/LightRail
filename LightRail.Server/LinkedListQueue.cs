using System;

namespace LightRail.Server
{
    public class LinkedListQueue<T>
    {
        public LinkedListQueue(ushort id, string path)
        {
            this.ID = id;
            this.Path = path;
        }

        public ushort ID { get; }
        public string Path { get; }

        /// <summary>
        /// The "first" node in the queue. This is the node that is popped.
        /// If Head == Tail == Empty, then the queue is empty.
        /// </summary>
        private LinkedListQueueNode<T> head = null;
        /// <summary>
        /// The "last" node in the queue. Nodes are enqueued here.
        /// </summary>
        public LinkedListQueueNode<T> tail = null;
        /// <summary>
        /// The number of nodes currently in the queue.
        /// </summary>
        public uint Count { get; private set; } = 0;

        public T TryDequeue()
        {
            if (head == null)
            {
                return default(T);
            }
            var current = head;
            head = current.Next;
            Count--;
            var item = current.Item;
            ReleaseQueueNode(current);
            return item;
        }

        public uint Enqueue(T item)
        {
            var current = GetQueueNode() ?? new LinkedListQueueNode<T>();
            current.Item = item;
            current.Next = null;
            if (head == null)
            {
                head = tail = current; ;
            }
            else
            {
                tail = tail.Next = current;
            }
            Count++;
            return Count;
        }

        
        private static uint nextReusableQueueNodePointer = 0;
        /// <summary>
        /// The maximum number of LinkedListQueueNode<T> objects we will cache in memory.
        /// </summary>
        private const uint maxReusableQueueNodePointer = (100 * 100) - 1;
        /// <summary>
        /// A cache of reusable LinkedListQueueNode<T> objects. By releasing and caching the nodes, we can achieve
        /// performance gains by allocating fewer objects and therefore putting less strain on the GC.
        /// 
        /// If we allocate and release more objects than fit in the cache, then
        /// we will simply discard the objects.
        /// </summary>
        private static readonly LinkedListQueueNode<T>[] resuableQueueNodes = new LinkedListQueueNode<T>[maxReusableQueueNodePointer + 1];

        private static void ReleaseQueueNode(LinkedListQueueNode<T> node)
        {
            node.Item = default(T);
            node.Next = null;
            if (nextReusableQueueNodePointer == maxReusableQueueNodePointer)
            {
                return;
            }
            resuableQueueNodes[nextReusableQueueNodePointer] = node;
            nextReusableQueueNodePointer++;
        }

        private static LinkedListQueueNode<T> GetQueueNode()
        {
            if (nextReusableQueueNodePointer > 0)
            {
                nextReusableQueueNodePointer--;
            }
            var current = resuableQueueNodes[nextReusableQueueNodePointer];
            resuableQueueNodes[nextReusableQueueNodePointer] = null; // swap to NULL
            return current;
        }
    }
}
