# The AMQP Server Stack.

1. The server starts up a TCP Listener thread. Each time a socket is accepted,
   we create an instance of the AMQP TcpSocket abstraction.
2. Each active TCP connection is associated with an AmqpConnection state
   machine.
3. The AmqpConnection constantly receives data from the underlying socket
   in a loop, 1 frame at a time. Each frame is processed via the state machine;
   beginning/ending session, attaching/detaching links, etc. Also receiving
   transfer frames.
4. The AmqpConnection references an IContainer, which is abstraction for the
   application. In this case, a message broker/queue.
5. All writes to the underlying socket are buffered in the application. Writes
   are enqueued in a ConcurrentQueue and flushed. A RegisteredWaitHandle is
   signaled on each Enqueue(). An atomic register ensures only a single thread
   is actively looping over the queue and writing the queued writes to the
   underlying socket. The worker thread returns when no more queued writes exist.
6. The buffering of writes allows the system to continue receiving data while
   asynchronously writing. The thread handling a received frame must not block!
   That means I/O operations should be queued. And all shared state should be 
   non-blocking, non-locking. Ideally wait-free, but that's harder to do.

# Message Broker Queueing Architecture

1. Each message queue is implemented a concurrent, lock-free, non-blocking queue.
   The implementation makes heavy use of synchronization primatives including
   such as "compare-and-swap".
2. The queue implementation is an unbounded linked list. Items are enqueued at
   the Tail.
3. The "ConcurrentQueue" class implements a producer/consumer pattern. It supports
   multiple "producers" which enqueue new items. "Consumers" are registered with
   the queue.
4. Consumers are notified of messages in an event loop. Whenever an item is
   enqueued a RegisteredWaitHandle is signaled to start a thread with a callback.
   The callback method is protected from concurrent executions with an atomic flag.
   The event loop loops over each consumer and *synchronously* attempts delivery
   of exactly one item. If nothing is queued and/or there are no consumers then the 
   loop exits immediately.
5. Note that all consumers of a queue aquire messages *synchronously*. The aquired
   message should pass through the AmqpConnection state machine and the transfer
   is written to a buffer. The consumers' handler must not block! Since we're
   encoding a transfer, it's enqueue/buffered for writing to the underlying socket.