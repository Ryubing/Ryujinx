using Ryujinx.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Ryujinx.Horizon
{
    class HeapAllocator
    {
        private const ulong InvalidAddress = ulong.MaxValue;

        private readonly struct Range : IComparable<Range>
        {
            public ulong Offset { get; }
            public ulong Size { get; }

            public Range(ulong offset, ulong size)
            {
                Offset = offset;
                Size = size;
            }

            public int CompareTo(Range other)
            {
                return Offset.CompareTo(other.Offset);
            }
        }

        private readonly List<Range> _freeRanges;
        private ulong _currentHeapSize;

        public HeapAllocator()
        {
            _freeRanges = [];
            _currentHeapSize = 0;
        }

        public ulong Allocate(ulong size, ulong alignment = 1UL)
        {
            ulong address = AllocateImpl(size, alignment);

            if (address == InvalidAddress)
            {
                ExpandHeap(size + alignment - 1UL);

                address = AllocateImpl(size, alignment);

                Debug.Assert(address != InvalidAddress);
            }

            return address;
        }

        private void ExpandHeap(ulong expansionSize)
        {
            ulong oldHeapSize = _currentHeapSize;
            ulong newHeapSize = BitUtils.AlignUp(oldHeapSize + expansionSize, 0x200000UL);

            _currentHeapSize = newHeapSize;

            HorizonStatic.Syscall.SetHeapSize(out ulong heapAddress, newHeapSize).AbortOnFailure();

            Free(heapAddress + oldHeapSize, newHeapSize - oldHeapSize);
        }

        private ulong AllocateImpl(ulong size, ulong alignment)
        {
            for (int i = 0; i < _freeRanges.Count; i++)
            {
                Range range = _freeRanges[i];

                ulong alignedOffset = BitUtils.AlignUp(range.Offset, alignment);
                ulong sizeDelta = alignedOffset - range.Offset;
                ulong usableSize = range.Size - sizeDelta;

                if (sizeDelta < range.Size && usableSize >= size)
                {
                    _freeRanges.RemoveAt(i);

                    if (sizeDelta != 0)
                    {
                        InsertFreeRange(range.Offset, sizeDelta);
                    }

                    ulong endOffset = range.Offset + range.Size;
                    ulong remainingSize = endOffset - (alignedOffset + size);
                    if (remainingSize != 0)
                    {
                        InsertFreeRange(endOffset - remainingSize, remainingSize);
                    }

                    return alignedOffset;
                }
            }

            return InvalidAddress;
        }

        public void Free(ulong offset, ulong size)
        {
            InsertFreeRangeComingled(offset, size);
        }

        private void InsertFreeRange(ulong offset, ulong size)
        {
            Range range = new(offset, size);
            int index = _freeRanges.BinarySearch(range);
            if (index < 0)
            {
                index = ~index;
            }

            _freeRanges.Insert(index, range);
        }

        private void InsertFreeRangeComingled(ulong offset, ulong size)
        {
            ulong endOffset = offset + size;
            Range range = new(offset, size);
            int index = _freeRanges.BinarySearch(range);
            if (index < 0)
            {
                index = ~index;
            }

            if (index < _freeRanges.Count && _freeRanges[index].Offset == endOffset)
            {
                endOffset = _freeRanges[index].Offset + _freeRanges[index].Size;
                _freeRanges.RemoveAt(index);
            }

            if (index > 0 && _freeRanges[index - 1].Offset + _freeRanges[index - 1].Size == offset)
            {
                offset = _freeRanges[index - 1].Offset;
                _freeRanges.RemoveAt(--index);
            }

            range = new Range(offset, endOffset - offset);

            _freeRanges.Insert(index, range);
        }
    }
}
