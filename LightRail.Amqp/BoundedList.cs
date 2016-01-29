using System;

namespace LightRail.Amqp
{
    /// <summary>
    /// Like List<T> but with an upper bound to the growth.
    /// 
    /// NOT THREAD SAFE!!
    /// </summary>
    public class BoundedList<T>
    {
        public BoundedList(uint initialCapacity, uint maximumCapacity)
        {
            if (initialCapacity > maximumCapacity)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "initialCapacity must be less than maximumCapacity");
            MaximumCapacity = maximumCapacity;
            items = new T[initialCapacity];
        }

        private T[] items;
        private static readonly T nullItem = default(T);

        /// <summary>
        /// Returns the current capacity of the list.
        /// </summary>
        public uint Capacity
        {
            get { return (uint)items.Length; }
            private set
            {
                if (value > MaximumCapacity)
                    throw new ArgumentOutOfRangeException(nameof(value), "Capacity must be less than MaximumCapacity");
                var newItems = new T[value];
                Array.Copy(items, 0, newItems, 0, items.Length);
                items = newItems;
            }
        }

        /// <summary>
        /// Returns the maximum capcity of the list.
        /// </summary>
        public uint MaximumCapacity { get; }

        /// <summary>
        /// Adds an item (growing the list if necessary) and returns the index at which it was added.
        /// 
        /// Throws an exception if the list is full.
        /// </summary>
        public uint Add(T item)
        {
            var nextIndex = Length;
            EnsureCapacity(nextIndex + 1);
            items[nextIndex] = item;
            Length++;
            return nextIndex;
        }

        /// <summary>
        /// Gets or sets an item at the specific index.
        /// 
        /// The index MAY be past the current Length. If so, the Length will be increased.
        /// 
        /// Throws an exception if the list is full.
        /// </summary>
        public T this[uint index]
        {
            get
            {
                return items[index];
            }
            set
            {
                EnsureCapacity(index + 1);
                items[index] = value;
                // if adding to an index past the current length
                // set length to show the new length. Elements in between may be empty.
                if (index > (Length - 1) || Length == 0)
                {
                    Length = index + 1;
                }
            }
        }

        /// <summary>
        /// Removes all items (sets each index to default(T)) and sets Length to 0.
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < items.Length; i++)
            {
                items[i] = nullItem;
            }
            Length = 0;
        }

        /// <summary>
        /// Returns the current length of the list. That is, the last index in which a value as added.
        /// </summary>
        public uint Length { get; private set; }

        /// <summary>
        /// Returns true if the current list length is at or above the maximum capcity of the list. Adding any new
        /// item will result in an exception.
        /// </summary>
        public bool IsFull
        {
            get
            {
                return Length >= MaximumCapacity;
            }
        }

        private double _growthRate = 0.5;
        /// <summary>
        /// The rate at which the list will grow, as needed. As a percentage. Must be 0 &lt; x &lte; 1.
        /// Default is 50%.
        /// </summary>
        public double GrowthRate
        {
            get { return _growthRate; }
            set
            {
                if (!(0 < value && value <= 1))
                    throw new ArgumentOutOfRangeException(nameof(value), "Value must be 0 < x <= 1");
                _growthRate = value;
            }
        }

        private void EnsureCapacity(uint requiredCapacity)
        {
            if (requiredCapacity > Capacity)
            {
                if (requiredCapacity > MaximumCapacity)
                {
                    throw new ArgumentOutOfRangeException(nameof(requiredCapacity), "Capacity of list must be less or equal to than MaximumCapacity");
                }
                // grow by the growth rate, up to the max capacity
                Capacity = Math.Min(requiredCapacity + (uint)Math.Ceiling(requiredCapacity * GrowthRate), MaximumCapacity);
            }
        }

        /// <summary>
        /// Returns in the index of the first null item in the list up to the Length.
        /// </summary>
        /// <returns></returns>
        public uint? IndexOfFirstNullItem()
        {
            for (uint i = 0; i < Length; i++)
            {
                if (items[i] == null)
                    return i;
            }
            return null;
        }
    }
}
