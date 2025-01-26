using System;
using System.Collections.Generic;

namespace Ryujinx.Memory.Range
{
    /// <summary>
    /// Sequence of physical memory regions that a single non-contiguous virtual memory region maps to.
    /// </summary>
    public readonly struct MultiRange : IEquatable<MultiRange>
    {
        private const ulong InvalidAddress = ulong.MaxValue;

        private readonly MemoryRange _singleRange;
        private readonly MemoryRange[] _ranges;

        private bool HasSingleRange => _ranges == null;

        /// <summary>
        /// Indicates that the range is fully unmapped.
        /// </summary>
        public bool IsUnmapped => HasSingleRange && _singleRange.Address == InvalidAddress;

        /// <summary>
        /// Total of physical sub-ranges on the virtual memory region.
        /// </summary>
        public int Count => HasSingleRange ? 1 : _ranges.Length;

        /// <summary>
        /// Creates a new multi-range with a single physical region.
        /// </summary>
        /// <param name="address">Start address of the region</param>
        /// <param name="size">Size of the region in bytes</param>
        public MultiRange(ulong address, ulong size)
        {
            _singleRange = new MemoryRange(address, size);
            _ranges = null;
        }

        /// <summary>
        /// Creates a new multi-range with multiple physical regions.
        /// </summary>
        /// <param name="ranges">Array of physical regions</param>
        /// <exception cref="ArgumentNullException"><paramref name="ranges"/> is null</exception>
        public MultiRange(MemoryRange[] ranges)
        {
            ArgumentNullException.ThrowIfNull(ranges);

            if (ranges.Length == 1)
            {
                _singleRange = ranges[0];
                _ranges = null;
            }
            else
            {
                _singleRange = MemoryRange.Empty;
                _ranges = ranges;
            }
        }

        /// <summary>
        /// Gets a slice of the multi-range.
        /// </summary>
        /// <param name="offset">Offset of the slice into the multi-range in bytes</param>
        /// <param name="size">Size of the slice in bytes</param>
        /// <returns>A new multi-range representing the given slice of this one</returns>
        public MultiRange Slice(ulong offset, ulong size)
        {
            if (HasSingleRange)
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThan(size, _singleRange.Size - offset);

                return new MultiRange(_singleRange.Address + offset, size);
            }
            else
            {
                List<MemoryRange> ranges = [];

                foreach (MemoryRange range in _ranges)
                {
                    if ((long)offset <= 0)
                    {
                        ranges.Add(new MemoryRange(range.Address, Math.Min(size, range.Size)));
                        size -= range.Size;
                    }
                    else if (offset < range.Size)
                    {
                        ulong sliceSize = Math.Min(size, range.Size - offset);

                        if (range.Address == InvalidAddress)
                        {
                            ranges.Add(new MemoryRange(range.Address, sliceSize));
                        }
                        else
                        {
                            ranges.Add(new MemoryRange(range.Address + offset, sliceSize));
                        }

                        size -= sliceSize;
                    }

                    if ((long)size <= 0)
                    {
                        break;
                    }

                    offset -= range.Size;
                }

                return ranges.Count == 1 ? new MultiRange(ranges[0].Address, ranges[0].Size) : new MultiRange(ranges.ToArray());
            }
        }

        /// <summary>
        /// Gets the physical region at the specified index.
        /// </summary>
        /// <param name="index">Index of the physical region</param>
        /// <returns>Region at the index specified</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is invalid</exception>
        public MemoryRange GetSubRange(int index)
        {
            if (HasSingleRange)
            {
                ArgumentOutOfRangeException.ThrowIfNotEqual(index, 0);

                return _singleRange;
            }
            else
            {
                if ((uint)index >= _ranges.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return _ranges[index];
            }
        }

        /// <summary>
        /// Gets the physical region at the specified index, without explicit bounds checking.
        /// </summary>
        /// <param name="index">Index of the physical region</param>
        /// <returns>Region at the index specified</returns>
        private MemoryRange GetSubRangeUnchecked(int index)
        {
            return HasSingleRange ? _singleRange : _ranges[index];
        }

        /// <summary>
        /// Check if two multi-ranges overlap with each other.
        /// </summary>
        /// <param name="other">Other multi-range to check for overlap</param>
        /// <returns>True if any sub-range overlaps, false otherwise</returns>
        public bool OverlapsWith(MultiRange other)
        {
            if (HasSingleRange && other.HasSingleRange)
            {
                return _singleRange.OverlapsWith(other._singleRange);
            }
            else
            {
                for (int i = 0; i < Count; i++)
                {
                    MemoryRange currentRange = GetSubRangeUnchecked(i);

                    for (int j = 0; j < other.Count; j++)
                    {
                        if (currentRange.OverlapsWith(other.GetSubRangeUnchecked(j)))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a given multi-range is fully contained inside another.
        /// </summary>
        /// <param name="other">Multi-range to be checked</param>
        /// <returns>True if all the sub-ranges on <paramref name="other"/> are contained inside the multi-range, with the same order, false otherwise</returns>
        public bool Contains(MultiRange other)
        {
            return FindOffset(other) >= 0;
        }

        /// <summary>
        /// Calculates the offset of a given multi-range inside another, when the multi-range is fully contained
        /// inside the other multi-range, otherwise returns -1.
        /// </summary>
        /// <param name="other">Multi-range that should be fully contained inside this one</param>
        /// <returns>Offset in bytes if fully contained, otherwise -1</returns>
        public int FindOffset(MultiRange other)
        {
            int thisCount = Count;
            int otherCount = other.Count;

            if (thisCount == 1 && otherCount == 1)
            {
                MemoryRange otherFirstRange = other.GetSubRangeUnchecked(0);
                MemoryRange currentFirstRange = GetSubRangeUnchecked(0);

                if (otherFirstRange.Address >= currentFirstRange.Address &&
                    otherFirstRange.EndAddress <= currentFirstRange.EndAddress)
                {
                    return (int)(otherFirstRange.Address - currentFirstRange.Address);
                }
            }
            else if (thisCount >= otherCount)
            {
                ulong baseOffset = 0;

                MemoryRange otherFirstRange = other.GetSubRangeUnchecked(0);
                MemoryRange otherLastRange = other.GetSubRangeUnchecked(otherCount - 1);

                for (int i = 0; i < (thisCount - otherCount) + 1; baseOffset += GetSubRangeUnchecked(i).Size, i++)
                {
                    MemoryRange currentFirstRange = GetSubRangeUnchecked(i);
                    MemoryRange currentLastRange = GetSubRangeUnchecked(i + otherCount - 1);

                    if (otherCount > 1)
                    {
                        if (otherFirstRange.Address < currentFirstRange.Address ||
                            otherFirstRange.EndAddress != currentFirstRange.EndAddress)
                        {
                            continue;
                        }

                        if (otherLastRange.Address != currentLastRange.Address ||
                            otherLastRange.EndAddress > currentLastRange.EndAddress)
                        {
                            continue;
                        }

                        bool fullMatch = true;

                        for (int j = 1; j < otherCount - 1; j++)
                        {
                            if (!GetSubRangeUnchecked(i + j).Equals(other.GetSubRangeUnchecked(j)))
                            {
                                fullMatch = false;
                                break;
                            }
                        }

                        if (!fullMatch)
                        {
                            continue;
                        }
                    }
                    else if (currentFirstRange.Address > otherFirstRange.Address ||
                             currentFirstRange.EndAddress < otherFirstRange.EndAddress)
                    {
                        continue;
                    }

                    return (int)(baseOffset + (otherFirstRange.Address - currentFirstRange.Address));
                }
            }

            return -1;
        }

        /// <summary>
        /// Gets the total size of all sub-ranges in bytes.
        /// </summary>
        /// <returns>Total size in bytes</returns>
        public ulong GetSize()
        {
            if (HasSingleRange)
            {
                return _singleRange.Size;
            }

            ulong sum = 0;

            foreach (MemoryRange range in _ranges)
            {
                sum += range.Size;
            }

            return sum;
        }

        public override bool Equals(object obj)
        {
            return obj is MultiRange other && Equals(other);
        }

        public bool Equals(MultiRange other)
        {
            if (HasSingleRange && other.HasSingleRange)
            {
                return _singleRange.Equals(other._singleRange);
            }

            int thisCount = Count;
            if (thisCount != other.Count)
            {
                return false;
            }

            for (int i = 0; i < thisCount; i++)
            {
                if (!GetSubRangeUnchecked(i).Equals(other.GetSubRangeUnchecked(i)))
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            if (HasSingleRange)
            {
                return _singleRange.GetHashCode();
            }

            HashCode hash = new();

            foreach (MemoryRange range in _ranges)
            {
                hash.Add(range);
            }

            return hash.ToHashCode();
        }

        /// <summary>
        /// Returns a string summary of the ranges contained in the MultiRange.
        /// </summary>
        /// <returns>A string summary of the ranges contained within</returns>
        public override string ToString()
        {
            return HasSingleRange ? _singleRange.ToString() : string.Join(", ", _ranges);
        }

        public static bool operator ==(MultiRange left, MultiRange right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MultiRange left, MultiRange right)
        {
            return !(left == right);
        }
    }
}
