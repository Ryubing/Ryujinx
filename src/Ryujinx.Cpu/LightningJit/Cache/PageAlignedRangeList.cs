using Ryujinx.Common;
using Ryujinx.Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Ryujinx.Cpu.LightningJit.Cache
{
    class PageAlignedRangeList
    {
        private readonly struct Range : IComparable<Range>
        {
            public int Offset { get; }
            public int Size { get; }

            public Range(int offset, int size)
            {
                Offset = offset;
                Size = size;
            }

            public int CompareTo([AllowNull] Range other)
            {
                return Offset.CompareTo(other.Offset);
            }
        }

        private readonly Action<int, int> _alignedRangeAction;
        private readonly Action<ulong, TranslatedFunction> _alignedFunctionAction;
        private readonly List<(Range, ulong, TranslatedFunction)> _pendingFunctions;
        private readonly List<Range> _ranges;

        public PageAlignedRangeList(Action<int, int> alignedRangeAction, Action<ulong, TranslatedFunction> alignedFunctionAction)
        {
            _alignedRangeAction = alignedRangeAction;
            _alignedFunctionAction = alignedFunctionAction;
            _pendingFunctions = [];
            _ranges = [];
        }

        public bool Has(ulong address)
        {
            foreach ((_, ulong guestAddress, _) in _pendingFunctions)
            {
                if (guestAddress == address)
                {
                    return true;
                }
            }

            return false;
        }

        public void Add(int offset, int size, ulong address, TranslatedFunction function)
        {
            Range range = new(offset, size);

            Insert(range);
            _pendingFunctions.Add((range, address, function));
            ProcessAlignedRanges();
        }

        public void Pad(CacheMemoryAllocator allocator)
        {
            int pageSize = (int)MemoryBlock.GetPageSize();

            for (int index = 0; index < _ranges.Count; index++)
            {
                Range range = _ranges[index];

                int endOffset = range.Offset + range.Size;

                int alignedStart = BitUtils.AlignDown(range.Offset, pageSize);
                int alignedEnd = BitUtils.AlignUp(endOffset, pageSize);
                int alignedSize = alignedEnd - alignedStart;

                if (alignedStart < range.Offset)
                {
                    allocator.ForceAllocation(alignedStart, range.Offset - alignedStart);
                }

                if (alignedEnd > endOffset)
                {
                    allocator.ForceAllocation(endOffset, alignedEnd - endOffset);
                }

                _alignedRangeAction(alignedStart, alignedSize);
                _ranges.RemoveAt(index--);
                ProcessPendingFunctions(index, alignedEnd);
            }
        }

        private void ProcessAlignedRanges()
        {
            int pageSize = (int)MemoryBlock.GetPageSize();

            for (int index = 0; index < _ranges.Count; index++)
            {
                Range range = _ranges[index];

                int alignedStart = BitUtils.AlignUp(range.Offset, pageSize);
                int alignedEnd = BitUtils.AlignDown(range.Offset + range.Size, pageSize);
                int alignedSize = alignedEnd - alignedStart;

                if (alignedSize <= 0)
                {
                    continue;
                }

                _alignedRangeAction(alignedStart, alignedSize);
                SplitAt(ref index, alignedStart, alignedEnd);
                ProcessPendingFunctions(index, alignedEnd);
            }
        }

        private void ProcessPendingFunctions(int rangeIndex, int alignedEnd)
        {
            if ((rangeIndex > 0 && rangeIndex == _ranges.Count) ||
                (rangeIndex >= 0 && rangeIndex < _ranges.Count && _ranges[rangeIndex].Offset >= alignedEnd))
            {
                rangeIndex--;
            }

            int alignedStart;

            if (rangeIndex >= 0)
            {
                alignedStart = _ranges[rangeIndex].Offset + _ranges[rangeIndex].Size;
            }
            else
            {
                alignedStart = 0;
            }

            if (rangeIndex < _ranges.Count - 1)
            {
                alignedEnd = _ranges[rangeIndex + 1].Offset;
            }
            else
            {
                alignedEnd = int.MaxValue;
            }

            for (int index = 0; index < _pendingFunctions.Count; index++)
            {
                (Range range, ulong address, TranslatedFunction function) = _pendingFunctions[index];

                if (range.Offset >= alignedStart && range.Offset + range.Size <= alignedEnd)
                {
                    _alignedFunctionAction(address, function);
                    _pendingFunctions.RemoveAt(index--);
                }
            }
        }

        private void Insert(Range range)
        {
            int index = _ranges.BinarySearch(range);

            if (index < 0)
            {
                index = ~index;
            }

            if (index < _ranges.Count)
            {
                Range next = _ranges[index];

                int endOffs = range.Offset + range.Size;

                if (next.Offset == endOffs)
                {
                    range = new Range(range.Offset, range.Size + next.Size);
                    _ranges.RemoveAt(index);
                }
            }

            if (index > 0)
            {
                Range prev = _ranges[index - 1];

                if (prev.Offset + prev.Size == range.Offset)
                {
                    range = new Range(range.Offset - prev.Size, range.Size + prev.Size);
                    _ranges.RemoveAt(--index);
                }
            }

            _ranges.Insert(index, range);
        }

        private void SplitAt(ref int index, int alignedStart, int alignedEnd)
        {
            Range range = _ranges[index];

            if (range.Offset < alignedStart)
            {
                _ranges[index++] = new(range.Offset, alignedStart - range.Offset);

                if (range.Offset + range.Size > alignedEnd)
                {
                    _ranges.Insert(index, new(alignedEnd, (range.Offset + range.Size) - alignedEnd));
                }
            }
            else if (range.Offset + range.Size > alignedEnd)
            {
                _ranges[index] = new(alignedEnd, (range.Offset + range.Size) - alignedEnd);
            }
            else if (range.Offset == alignedStart && range.Offset + range.Size == alignedEnd)
            {
                Debug.Assert(range.Offset == alignedStart && range.Offset + range.Size == alignedEnd);

                _ranges.RemoveAt(index--);
            }
        }
    }
}
