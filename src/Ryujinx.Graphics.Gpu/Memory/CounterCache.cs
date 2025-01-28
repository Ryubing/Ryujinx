using Ryujinx.Graphics.GAL;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gpu.Memory
{
    /// <summary>
    /// Represents the GPU counter cache.
    /// </summary>
    class CounterCache
    {
        private readonly struct CounterEntry
        {
            public ulong Address { get; }
            public ICounterEvent Event { get; }

            public CounterEntry(ulong address, ICounterEvent evt)
            {
                Address = address;
                Event = evt;
            }
        }

        private readonly List<CounterEntry> _items;

        /// <summary>
        /// Creates a new instance of the GPU counter cache.
        /// </summary>
        public CounterCache()
        {
            _items = [];
        }

        /// <summary>
        /// Adds a new counter to the counter cache, or updates a existing one.
        /// </summary>
        /// <param name="gpuVa">GPU virtual address where the counter will be written in memory</param>
        /// <param name="evt">The new counter</param>
        public void AddOrUpdate(ulong gpuVa, ICounterEvent evt)
        {
            int index = BinarySearch(gpuVa);

            CounterEntry entry = new(gpuVa, evt);

            if (index < 0)
            {
                _items.Insert(~index, entry);
            }
            else
            {
                _items[index] = entry;
            }
        }

        /// <summary>
        /// Handles removal of counters written to a memory region being unmapped.
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        public void MemoryUnmappedHandler(object sender, UnmapEventArgs e) => RemoveRange(e.Address, e.Size);

        private void RemoveRange(ulong gpuVa, ulong size)
        {
            int index = BinarySearch(gpuVa + size - 1);

            if (index < 0)
            {
                index = ~index;
            }

            if (index >= _items.Count || !InRange(gpuVa, size, _items[index].Address))
            {
                return;
            }

            int count = 1;

            while (index > 0 && InRange(gpuVa, size, _items[index - 1].Address))
            {
                index--;
                count++;
            }

            // Notify the removed counter events that their result should no longer be written out.
            for (int i = 0; i < count; i++)
            {
                ICounterEvent evt = _items[index + i].Event;
                if (evt != null)
                {
                    evt.Invalid = true;
                }
            }

            _items.RemoveRange(index, count);
        }

        /// <summary>
        /// Checks whenever an address falls inside a given range.
        /// </summary>
        /// <param name="startVa">Range start address</param>
        /// <param name="size">Range size</param>
        /// <param name="gpuVa">Address being checked</param>
        /// <returns>True if the address falls inside the range, false otherwise</returns>
        private static bool InRange(ulong startVa, ulong size, ulong gpuVa)
        {
            return gpuVa >= startVa && gpuVa < startVa + size;
        }

        /// <summary>
        /// Check if any counter value was written to the specified GPU virtual memory address.
        /// </summary>
        /// <param name="gpuVa">GPU virtual address</param>
        /// <returns>True if any counter value was written on the specified address, false otherwise</returns>
        public bool Contains(ulong gpuVa)
        {
            return BinarySearch(gpuVa) >= 0;
        }

        /// <summary>
        /// Flush any counter value written to the specified GPU virtual memory address.
        /// </summary>
        /// <param name="gpuVa">GPU virtual address</param>
        /// <returns>True if any counter value was written on the specified address, false otherwise</returns>
        public bool FindAndFlush(ulong gpuVa)
        {
            int index = BinarySearch(gpuVa);
            if (index > 0)
            {
                _items[index].Event?.Flush();

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Find any counter event that would write to the specified GPU virtual memory address.
        /// </summary>
        /// <param name="gpuVa">GPU virtual address</param>
        /// <returns>The counter event, or null if not present</returns>
        public ICounterEvent FindEvent(ulong gpuVa)
        {
            int index = BinarySearch(gpuVa);
            if (index > 0)
            {
                return _items[index].Event;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Performs binary search of an address on the list.
        /// </summary>
        /// <param name="address">Address to search</param>
        /// <returns>Index of the item, or complement of the index of the nearest item with lower value</returns>
        private int BinarySearch(ulong address)
        {
            int left = 0;
            int right = _items.Count - 1;

            while (left <= right)
            {
                int range = right - left;

                int middle = left + (range >> 1);

                CounterEntry item = _items[middle];

                if (item.Address == address)
                {
                    return middle;
                }

                if (address < item.Address)
                {
                    right = middle - 1;
                }
                else
                {
                    left = middle + 1;
                }
            }

            return ~left;
        }
    }
}
