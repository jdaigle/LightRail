/*
 * This is a port of ConcurrentLinkedQueue from JSR-166. Original public domain
 * license is below.
 * 
 * With modifications necessary to efficiently implement a message broker.
 * 
 * http://gee.cs.oswego.edu/dl/concurrency-interest/
 * http://gee.cs.oswego.edu/cgi-bin/viewcvs.cgi/jsr166/src/main/java/util/concurrent/ConcurrentLinkedQueue.java?view=markup
 * https://gist.github.com/jdaigle/90f5a7e255d17de736e9
 *
 *
 * Written by Doug Lea and Martin Buchholz with assistance from members of
 * JCP JSR-166 Expert Group and released to the public domain, as explained
 * at http://creativecommons.org/publicdomain/zero/1.0/
 */

using System.Threading;

namespace LightRail.Server.Queuing
{
    /// <summary>
    /// An unbounded thread-safe queue based on linked nodes.
    /// This queue orders elements FIFO (first-in-first-out).
    /// The <em>head</em> of the queue is that element that has been on the
    /// queue the longest time.
    /// The <em>tail</em> of the queue is that element that has been on the
    /// queue the shortest time. New elements
    /// are inserted at the tail of the queue, and the queue retrieval
    /// operations obtain elements at the head of the queue.
    /// A ConcurrentLinkedQueue is an appropriate choice when
    /// many threads will share access to a common collection.
    /// Like most other concurrent collection implementations, this class
    /// does not permit the use of null elements.
    ///
    /// <p>This implementation employs an efficient <em>non-blocking</em>
    /// algorithm based on one described in
    /// <a href="http://www.cs.rochester.edu/~scott/papers/1996_PODC_queues.pdf">
    /// Simple, Fast, and Practical Non-Blocking and Blocking Concurrent Queue
    /// Algorithms</a> by Maged M. Michael and Michael L. Scott.
    /// </summary>
    public sealed class ConcurrentLinkedQueue<T> where T : class
    {
        /*
         * This is a modification of the Michael & Scott algorithm,
         * adapted for a garbage-collected environment, with support for
         * interior node deletion (to support remove(Object)).  For
         * explanation, read the paper.
         *
         * Note that like most non-blocking algorithms in this package,
         * this implementation relies on the fact that in garbage
         * collected systems, there is no possibility of ABA problems due
         * to recycled nodes, so there is no need to use "counted
         * pointers" or related techniques seen in versions used in
         * non-GC'ed settings.
         *
         * The fundamental invariants are:
         * - There is exactly one (last) Node with a null next reference,
         *   which is CASed when enqueueing.  This last Node can be
         *   reached in O(1) time from tail, but tail is merely an
         *   optimization - it can always be reached in O(N) time from
         *   head as well.
         * - The elements contained in the queue are the non-null items in
         *   Nodes that are reachable from head.  CASing the item
         *   reference of a Node to null atomically removes it from the
         *   queue.  Reachability of all elements from head must remain
         *   true even in the case of concurrent modifications that cause
         *   head to advance.  A dequeued Node may remain in use
         *   indefinitely due to creation of an Iterator or simply a
         *   poll() that has lost its time slice.
         *
         * The above might appear to imply that all Nodes are GC-reachable
         * from a predecessor dequeued Node.  That would cause two problems:
         * - allow a rogue Iterator to cause unbounded memory retention
         * - cause cross-generational linking of old Nodes to new Nodes if
         *   a Node was tenured while live, which generational GCs have a
         *   hard time dealing with, causing repeated major collections.
         * However, only non-deleted Nodes need to be reachable from
         * dequeued Nodes, and reachability does not necessarily have to
         * be of the kind understood by the GC.  We use the trick of
         * linking a Node that has just been dequeued to itself.  Such a
         * self-link implicitly means to advance to head.
         *
         * Both head and tail are permitted to lag.  In fact, failing to
         * update them every time one could is a significant optimization
         * (fewer CASes). As with LinkedTransferQueue (see the internal
         * documentation for that class), we use a slack threshold of two;
         * that is, we update head/tail when the current pointer appears
         * to be two or more steps away from the first/last node.
         *
         * Since head and tail are updated concurrently and independently,
         * it is possible for tail to lag behind head (why not)?
         *
         * CASing a Node's item reference to null atomically removes the
         * element from the queue.  Iterators skip over Nodes with null
         * items.  Prior implementations of this class had a race between
         * poll() and remove(Object) where the same element would appear
         * to be successfully removed by two concurrent operations.  The
         * method remove(Object) also lazily unlinks deleted Nodes, but
         * this is merely an optimization.
         *
         * When constructing a Node (before enqueuing it) we avoid paying
         * for a volatile write to item by using Unsafe.putObject instead
         * of a normal write.  This allows the cost of enqueue to be
         * "one-and-a-half" CASes.
         *
         * Both head and tail may or may not point to a Node with a
         * non-null item.  If the queue is empty, all items must of course
         * be null.  Upon creation, both head and tail refer to a dummy
         * Node with null item.  Both head and tail are only updated using
         * CAS, so they never regress, although again this is merely an
         * optimization.
         */

        /// <summary>
        /// A node from which the first live (non-deleted) node (if any)
        /// can be reached in O(1) time.
        /// Invariants:
        /// - all live nodes are reachable from head via succ()
        /// - head != null
        /// - (tmp = head).next != tmp || tmp != head
        /// Non-invariants:
        /// - head.item may or may not be null.
        /// - it is permitted for tail to lag behind head, that is, for tail
        /// to not be reachable from head!
        /// </summary>
        private volatile Node head;
        /// <summary>
        /// A node from which the last node on list (that is, the unique
        /// node with node.next == null) can be reached in O(1) time.
        /// Invariants:
        /// - the last node is always reachable from tail via succ()
        /// - tail != null
        /// Non-invariants:
        /// - tail.item may or may not be null.
        /// - it is permitted for tail to lag behind head, that is, for tail
        /// to not be reachable from head!
        /// - tail.next may or may not be self-pointing to tail.
        /// </summary>
        private volatile Node tail;

        public ConcurrentLinkedQueue()
        {
            head = tail = new Node(null);
        }

        /// <summary>
        /// Inserts the specified element at the tail of this queue.
        /// As the queue is unbounded, this method will never fail.
        /// </summary>
        public Node Offer(T item)
        {
            var newNode = new Node(item);

            for (Node t = tail, p = t; ;)
            {
                Node q = p.next;
                if (q == null)
                {
                    // p is last node
                    if (casNext(p, null, newNode))
                    {
                        // Successful CAS is the linearization point
                        // for item to become an element of this queue,
                        // and for n to become "live".

                        if (p != t)              // skip the CAS until the tail has lagged (this is an optimization only)
                            casTail(t, newNode); // Failure is OK.
                        return newNode; // exit loop
                    }
                    // Lost CAS race to another thread; re-read next
                }
                else if (p == q)
                {
                    // We have fallen off list.  If tail is unchanged, it
                    // will also be off-list, in which case we need to
                    // jump to head, from which all live nodes are always
                    // reachable.  Else the new tail is a better bet.
                    p = (t != (t = tail)) ? t : head;
                }
                else
                {
                    // Check for tail updates after two hops.
                    p = (p != t && t != (t = tail)) ? t : q;
                }
            }
        }

        /// <summary>
        /// Removes and returns the first live element on the list, or null if none.
        /// </summary>
        public T poll()
        {
            restartFromHead:
            for (;;)
            {
                for (Node h = head, p = h, q; ;)
                {
                    T item = p.item;

                    if (item != null && casItem(p, item, null))
                    {
                        // Successful CAS is the linearization point
                        // for item to be removed from this queue.
                        if (p != h) // hop two nodes at a time
                            UpdateHead(h, ((q = p.next) != null) ? q : p);
                        return item;
                    }
                    else if ((q = p.next) == null)
                    {
                        UpdateHead(h, p);
                        return null;
                    }
                    else if (p == q)
                        goto restartFromHead;
                    else
                        p = q;
                }
            }
        }

        /// <summary>
        /// Returns the first live element on the list, or null if none, without
        /// removing the element.
        /// </summary>
        public T Peek()
        {
            restartFromHead:
            for (;;)
            {
                for (Node h = head, p = h, q; ;)
                {
                    T item = p.item;
                    if (item != null || (q = p.next) == null)
                    {
                        UpdateHead(h, p);
                        return item;
                    }
                    else if (p == q)
                        goto restartFromHead;
                    else
                        p = q;
                }
            }
        }

        /// <summary>
        /// Returns the first live (non-deleted) node on list, or null if none.
        /// This is yet another variant of poll/peek; here returning the
        /// first node, not element. We could make peek() a wrapper around
        /// first(), but that would cost an extra volatile read of item,
        /// and the need to add a retry loop to deal with the possibility
        /// of losing a race to a concurrent poll().
        /// </summary>
        public Node First()
        {
            restartFromHead:
            for (;;)
            {
                for (Node h = head, p = h, q; ;)
                {
                    bool hasItem = (p.item != null);
                    if (hasItem || (q = p.next) == null)
                    {
                        UpdateHead(h, p);
                        return hasItem ? p : null;
                    }
                    else if (p == q)
                        goto restartFromHead;
                    else
                        p = q;
                }
            }
        }

        /// <summary>
        /// Returns true if this queue contains no elements.
        /// </summary>
        public bool IsEmpty()
        {
            return First() == null;
        }

        /// <summary>
        /// Removes a single instance of the specified element from this queue,
        /// if it is present. More formally, removes an element T such
        /// that o.equals(e), if this queue contains one or more such
        /// elements.
        /// 
        /// Returns true if this queue contained the specified element
        /// (or equivalently, if this queue changed as a result of the call).
        /// </summary>
        /// <param name="o">element to be removed from this queue, if present</param>
        /// <returns>true if this queue changed as a result of the call</returns>
        public bool Remove(T o)
        {
            if (o != null)
            {
                Node next, pred = null;
                for (Node p = First(); p != null; pred = p, p = next)
                {
                    bool removed = false;
                    T item = p.item;
                    if (item != null)
                    {
                        if (!o.Equals(item))
                        {
                            next = Succ(p);
                            continue;
                        }
                        removed = casItem(p, item, null);
                    }
                    next = Succ(p);
                    if (pred != null && next != null) // unlink
                        casNext(pred, p, next);
                    if (removed)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Tries to CAS head to p. If successful, repoint old head to itself
        /// as sentinel for succ().
        /// </summary>
        private void UpdateHead(Node h, Node p)
        {
            if (h != p && casHead(h, p))
                Interlocked.Exchange(ref h.next, h);
        }

        /// <summary>
        /// Returns the successor of p, or the head node if p.next has been
        /// linked to self, which will only be true if traversing with a
        /// stale pointer that is now off the list.
        /// </summary>
        private Node Succ(Node p)
        {
            Node next = p.next;
            return (p == next) ? head : next;
        }

        private bool casHead(Node cmp, Node val) => Interlocked.CompareExchange(ref head, val, cmp) == cmp;
        private bool casTail(Node cmp, Node val) => Interlocked.CompareExchange(ref tail, val, cmp) == cmp;

        private static bool casItem(Node node, T cmp, T val) => Interlocked.CompareExchange(ref node.item, val, cmp) == cmp;
        private static bool casNext(Node node, Node cmp, Node val) => Interlocked.CompareExchange(ref node.next, val, cmp) == cmp;

        public sealed class Node
        {
            public Node(T item)
            {
                this.item = item;
            }

            public volatile T item;
            public volatile Node next;
        }
    }
}
