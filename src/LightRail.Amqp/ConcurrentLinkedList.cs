/*
 * This is a port of ConcurrentLinkedQueue from JSR-166. Original public domain
 * license is below.
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

using System;
using System.Threading;

namespace LightRail.Amqp
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
    /// 
    /// A ConcurrentLinkedList is an appropriate choice when
    /// many threads will share access to a common collection.
    ///
    /// <p>This implementation employs an efficient <em>non-blocking</em>
    /// algorithm based on one described in
    /// <a href="http://www.cs.rochester.edu/~scott/papers/1996_PODC_queues.pdf">
    /// Simple, Fast, and Practical Non-Blocking and Blocking Concurrent Queue
    /// Algorithms</a> by Maged M. Michael and Michael L. Scott.
    /// </summary>
    public sealed class ConcurrentLinkedList<T> where T : ConcurrentLinkedList<T>.Node, new()
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
        private volatile T head;
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
        private volatile T tail;

        public ConcurrentLinkedList()
        {
            head = tail = new T();
            head.removed = 1;
        }

        /// <summary>
        /// Inserts the specified node at the tail of this list.
        /// As the list is unbounded, this method will never fail.
        /// </summary>
        public bool Add(T newNode)
        {
            for (T t = tail, p = t; ;)
            {
                T q = p.next;
                if (q == null)
                {
                    // p is last node
                    if (casNext(p, null, newNode))
                    {
                        // Successful CAS is the linearization point
                        // for node to become an element of this list,
                        // and for n to become "live".

                        if (!ReferenceEquals(p, t)) // skip the CAS until the tail has lagged (this is an optimization only)
                            casTail(t, newNode);    // Failure is OK.
                        return true; // exit loop
                    }
                    // Lost CAS race to another thread; re-read next
                }
                else if (ReferenceEquals(p, q))
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
        /// Removes and returns the first live node on the list, or null if none.
        /// </summary>
        public T Pop()
        {
            restartFromHead:
            for (;;)
            {
                for (T h = head, p = h, q; ;)
                {
                    if (p.removed == 0 && casRemoved(p, 0, 1))
                    {
                        // Successful CAS is the linearization point
                        // for node to be removed from this list.
                        if (!ReferenceEquals(p, h)) // hop two nodes at a time
                            UpdateHead(h, ((q = p.next) != null) ? q : p);
                        return p;
                    }
                    else if ((q = p.next) == null)
                    {
                        UpdateHead(h, p);
                        return null;
                    }
                    else if (ReferenceEquals(p, q))
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
        public T First()
        {
            restartFromHead:
            for (;;)
            {
                for (T h = head, p = h, q; ;)
                {
                    if (p.removed == 0 || (q = p.next) == null)
                    {
                        UpdateHead(h, p);
                        return p.removed == 0 ? p : null;
                    }
                    else if (ReferenceEquals(p, q))
                        goto restartFromHead;
                    else
                        p = q;
                }
            }
        }

        /// <summary>
        /// Returns true if this list contains no elements.
        /// </summary>
        public bool IsEmpty()
        {
            return First() == null;
        }

        /// <summary>
        /// Removes a single instance of a node matching the specified predicate
        /// from this list.
        /// 
        /// Returns true if this list contained a matching node
        /// (or equivalently, if this list changed as a result of the call).
        /// </summary>
        public bool Remove(Func<T, bool> where)
        {
            T next, pred = null;
            for (T p = First(); p != null; pred = p, p = next)
            {
                bool removed = false;
                if (p.removed == 0)
                {
                    if (where(p) == false)
                    {
                        next = Succ(p);
                        continue;
                    }
                    removed = casRemoved(p, 0, 1);
                }
                next = Succ(p);
                if (pred != null && next != null) // unlink
                    casNext(pred, p, next);
                if (removed)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Attempts to first the specified node from the list.
        /// 
        /// Returns true if this list contained a matching node
        /// (or equivalently, if this list changed as a result of the call).
        /// </summary>
        public bool Remove(T node)
        {
            return Remove(x => ReferenceEquals(x, node));
        }

        /// <summary>
        /// Finds the first node in the list, started at the head,
        /// that matches specified predicate expression.
        /// Or null if none is found.
        /// </summary>
        public T Find(Func<T, bool> where)
        {
            T next, pred = null;
            for (T p = First(); p != null; pred = p, p = next)
            {
                T found = null;
                if (p.removed == 0)
                {
                    if (where(p) == false)
                    {
                        next = Succ(p);
                        continue;
                    }
                    found = p;
                }
                next = Succ(p);
                if (pred != null && next != null) // unlink
                    casNext(pred, p, next);
                if (found != null)
                    return found;
            }
            return null;
        }

        /// <summary>
        /// Tries to CAS head to p. If successful, repoint old head to itself
        /// as sentinel for succ().
        /// </summary>
        private void UpdateHead(T h, T p)
        {
            if (!ReferenceEquals(h, p) && casHead(h, p))
                Interlocked.Exchange(ref h.next, h);
        }

        /// <summary>
        /// Returns the successor of p, or the head node if p.next has been
        /// linked to self, which will only be true if traversing with a
        /// stale pointer that is now off the list.
        /// </summary>
        private T Succ(T p)
        {
            T next = p.next;
            return ReferenceEquals(p, next) ? head : next;
        }

        private bool casHead(T cmp, T val) => Interlocked.CompareExchange(ref head, val, cmp) == cmp;
        private bool casTail(T cmp, T val) => Interlocked.CompareExchange(ref tail, val, cmp) == cmp;

        private static bool casRemoved(T node, byte cmp, byte val) => Interlocked.CompareExchange(ref node.removed, val, cmp) == cmp;
        private static bool casNext(T node, T cmp, T val) => Interlocked.CompareExchange(ref node.next, val, cmp) == cmp;

        public abstract class Node
        {
            internal volatile int removed = 0;
            internal volatile T next;
        }
    }
}
