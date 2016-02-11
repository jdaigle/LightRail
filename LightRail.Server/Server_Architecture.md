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

### AMQP Session and Message Transfer Notes

* Session flow control is window based, effectively controls the number of unsettled
  messages transfers held in a buffer (across all links attached a session).
* Link flow control is credit-based, used to implement various messaging patterns
  (async, sync, drain, etc.).

### AMQP Session Flow

The specification is super unclear about Session flow control. But at the end
of the day Session flow control is about ensuring that the infrastructure is not
overburdended with unsettled deliveries.

The session has an "incoming-window" which is the number of transfers the session
can received. This is decremented on each received transfer. But when is it
incremented or reset? When we settle it? What about multi-transfer deliveries?
In theory, when it reaches zero, we shouldn't handle any more transfers. When
set or reset, we should send a flow.

The session maintains a "remote-incoming-window" which is the number of transfers
the session expects the *remote* session to be able to handle. This is decremented
on sent transfer. When it reaches zero, we must stop transfers. It's recalculated
when we receive a flow. The flow contains the remote's incoming-window, and it's
last observed transfer-id (our outgoing-transfer-id). The algorithm substracts
from the the remote's incoming window any outstanding transfers on our end.

In theory the session also tracks a seperate "outgoing-window". While it seems that
many implementations simply synchronize this with "remote-incoming-window", it
seems possible to have an indepedent state which controls how many transfers we can
send independent of what they can receive. Obviously in practice, this must necessarily
be less than or equal to the "remote-incoming-window".

Finally the "remote-outgoing-window". It's not clear what value this has other
than from the spec: "When this window shrinks, it is an indication of
outstanding transfers. Settling outstanding transfers can cause the window to grow."

 What about multi-transfer deliveries?

### Message Unsettled Delivery State (for Link Recovery)
* TODO:
* **I think that for version 1 we won't support suspending and resuming links.**

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

# Random Thoughts

I can send product specific properties: 
[03:47.365] RECV (ch=0) open(container-id:cf2bbc72-0962-48a0-bebe-83cc9b4ec181,max-frame-size:32768,channel-max:3,properties:[qpid.instance_name:Broker,product:qpid,version:0.26,qpid.build:1563358])