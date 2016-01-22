namespace LightRail.Server
{
    public class LinkedListQueueNode<T>
    {
        public T Item { get; set; }
        public LinkedListQueueNode<T> Next { get; set; }
    }
}
