using System;
using System.Collections.Generic;

namespace Ryujinx.Memory.Range
{
    /// <summary>
    /// A range list that assumes ranges are non-overlapping, with list items that can be split in two to avoid overlaps.
    /// </summary>
    /// <typeparam name="T">Type of the range.</typeparam>
    class NonOverlappingRangeList<T> : RangeList<T> where T : INonOverlappingRange
    {
        /// <summary>
        /// Finds a list of regions that cover the desired (address, size) range.
        /// If this range starts or ends in the middle of an existing region, it is split and only the relevant part is added.
        /// If there is no matching region, or there is a gap, then new regions are created with the factory.
        /// Regions are added to the list in address ascending order.
        /// </summary>
        /// <param name="list">List to add found regions to</param>
        /// <param name="address">Start address of the search region</param>
        /// <param name="size">Size of the search region</param>
        /// <param name="factory">Factory for creating new ranges</param>
        public void GetOrAddRegions(List<T> list, ulong address, ulong size, Func<ulong, ulong, T> factory)
        {
            // (regarding the specific case this generalized function is used for)
            // A new region may be split into multiple parts if multiple virtual regions have mapped to it.
            // For instance, while a virtual mapping could cover 0-2 in physical space, the space 0-1 may have already been reserved...
            // So we need to return both the split 0-1 and 1-2 ranges.

            T[] results = new T[1];
            int count = FindOverlapsNonOverlapping(address, size, ref results);

            if (count == 0)
            {
                // The region is fully unmapped. Create and add it to the range list.
                T region = factory(address, size);
                list.Add(region);
                Add(region);
            }
            else
            {
                ulong lastAddress = address;
                ulong endAddress = address + size;

                for (int i = 0; i < count; i++)
                {
                    T region = results[i];
                    if (count == 1 && region.Address == address && region.Size == size)
                    {
                        // Exact match, no splitting required.
                        list.Add(region);
                        return;
                    }

                    if (lastAddress < region.Address)
                    {
                        // There is a gap between this region and the last. We need to fill it.
                        T fillRegion = factory(lastAddress, region.Address - lastAddress);
                        list.Add(fillRegion);
                        Add(fillRegion);
                    }

                    if (region.Address < address)
                    {
                        // Split the region around our base address and take the high half.

                        region = Split(region, address);
                    }

                    if (region.EndAddress > address + size)
                    {
                        // Split the region around our end address and take the low half.

                        Split(region, address + size);
                    }

                    list.Add(region);
                    lastAddress = region.EndAddress;
                }

                if (lastAddress < endAddress)
                {
                    // There is a gap between this region and the end. We need to fill it.
                    T fillRegion = factory(lastAddress, endAddress - lastAddress);
                    list.Add(fillRegion);
                    Add(fillRegion);
                }
            }
        }

        /// <summary>
        /// Splits a region around a target point and updates the region list. 
        /// The original region's size is modified, but its address stays the same.
        /// A new region starting from the split address is added to the region list and returned.
        /// </summary>
        /// <param name="region">The region to split</param>
        /// <param name="splitAddress">The address to split with</param>
        /// <returns>The new region (high part)</returns>
        private T Split(T region, ulong splitAddress)
        {
            T newRegion = (T)region.Split(splitAddress);
            Update(region);
            Add(newRegion);
            return newRegion;
        }
    }
}
