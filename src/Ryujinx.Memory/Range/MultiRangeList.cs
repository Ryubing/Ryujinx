using Ryujinx.Common.Collections;
using System.Collections;
using System.Collections.Generic;

namespace Ryujinx.Memory.Range
{
    public class MultiRangeList<T> : IEnumerable<T> where T : IMultiRangeItem
    {
        private readonly IntervalTree<ulong, T> _items;

        public int Count { get; private set; }

        /// <summary>
        /// Creates a new range list.
        /// </summary>
        public MultiRangeList()
        {
            _items = new IntervalTree<ulong, T>();
        }

        /// <summary>
        /// Adds a new item to the list.
        /// </summary>
        /// <param name="item">The item to be added</param>
        public void Add(T item)
        {
            MultiRange range = item.Range;

            for (int i = 0; i < range.Count; i++)
            {
                MemoryRange subrange = range.GetSubRange(i);

                if (MemoryRange.IsInvalid(ref subrange))
                {
                    continue;
                }

                _items.Add(subrange.Address, subrange.EndAddress, item);
            }

            Count++;
        }

        /// <summary>
        /// Removes an item from the list.
        /// </summary>
        /// <param name="item">The item to be removed</param>
        /// <returns>True if the item was removed, or false if it was not found</returns>
        public bool Remove(T item)
        {
            MultiRange range = item.Range;

            int removed = 0;

            for (int i = 0; i < range.Count; i++)
            {
                MemoryRange subrange = range.GetSubRange(i);

                if (MemoryRange.IsInvalid(ref subrange))
                {
                    continue;
                }

                removed += _items.Remove(subrange.Address, item);
            }

            if (removed > 0)
            {
                // All deleted intervals are for the same item - the one we removed.
                Count--;
            }

            return removed > 0;
        }

        /// <summary>
        /// Gets all items on the list overlapping the specified memory range.
        /// </summary>
        /// <param name="address">Start address of the range</param>
        /// <param name="size">Size in bytes of the range</param>
        /// <param name="output">Output array where matches will be written. It is automatically resized to fit the results</param>
        /// <returns>The number of overlapping items found</returns>
        public int FindOverlaps(ulong address, ulong size, ref T[] output)
        {
            return FindOverlaps(new MultiRange(address, size), ref output);
        }

        /// <summary>
        /// Gets all items on the list overlapping the specified memory ranges.
        /// </summary>
        /// <param name="range">Ranges of memory being searched</param>
        /// <param name="output">Output array where matches will be written. It is automatically resized to fit the results</param>
        /// <returns>The number of overlapping items found</returns>
        public int FindOverlaps(MultiRange range, ref T[] output)
        {
            int overlapCount = 0;

            for (int i = 0; i < range.Count; i++)
            {
                MemoryRange subrange = range.GetSubRange(i);

                if (MemoryRange.IsInvalid(ref subrange))
                {
                    continue;
                }

                overlapCount = _items.Get(subrange.Address, subrange.EndAddress, ref output, overlapCount);
            }

            // Remove any duplicates, caused by items having multiple sub range nodes in the tree.
            if (overlapCount > 1)
            {
                int insertPtr = 0;
                for (int i = 0; i < overlapCount; i++)
                {
                    T item = output[i];
                    bool duplicate = false;

                    for (int j = insertPtr - 1; j >= 0; j--)
                    {
                        if (item.Equals(output[j]))
                        {
                            duplicate = true;
                            break;
                        }
                    }

                    if (!duplicate)
                    {
                        if (insertPtr != i)
                        {
                            output[insertPtr] = item;
                        }

                        insertPtr++;
                    }
                }

                overlapCount = insertPtr;
            }

            return overlapCount;
        }

        /// <summary>
        /// Gets all items on the list starting at the specified memory address.
        /// </summary>
        /// <param name="baseAddress">Base address to find</param>
        /// <param name="output">Output array where matches will be written. It is automatically resized to fit the results</param>
        /// <returns>The number of matches found</returns>
        public int FindOverlaps(ulong baseAddress, ref T[] output)
        {
            int count = _items.Get(baseAddress, ref output);

            // Only output items with matching base address
            int insertPtr = 0;
            for (int i = 0; i < count; i++)
            {
                if (output[i].BaseAddress == baseAddress)
                {
                    if (i != insertPtr)
                    {
                        output[insertPtr] = output[i];
                    }

                    insertPtr++;
                }
            }

            return insertPtr;
        }

        private List<T> GetList()
        {
            List<RangeNode<ulong, T>> items = _items.AsList();
            List<T> result = [];

            foreach (RangeNode<ulong, T> item in items)
            {
                if (item.Start == item.Value.BaseAddress)
                {
                    result.Add(item.Value);
                }
            }

            return result;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return GetList().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetList().GetEnumerator();
        }
    }
}
