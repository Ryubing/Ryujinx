using Ryujinx.Common;
using Ryujinx.Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Ryujinx.Cpu
{
    class PrivateMemoryAllocator : PrivateMemoryAllocatorImpl<PrivateMemoryAllocator.Block>
    {
        public const ulong InvalidOffset = ulong.MaxValue;

        public class Block : IComparable<Block>
        {
            public MemoryBlock Memory { get; private set; }
            public ulong Size { get; }

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

            public Block(MemoryBlock memory, ulong size)
            {
                Memory = memory;
                Size = size;
                _freeRanges =
                [
                    new(0, size)
                ];
            }

            public ulong Allocate(ulong size, ulong alignment)
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

                return InvalidOffset;
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

            public bool IsTotallyFree()
            {
                if (_freeRanges.Count == 1 && _freeRanges[0].Size == Size)
                {
                    Debug.Assert(_freeRanges[0].Offset == 0);
                    return true;
                }

                return false;
            }

            public int CompareTo(Block other)
            {
                return Size.CompareTo(other.Size);
            }

            public virtual void Destroy()
            {
                Memory.Dispose();
            }
        }

        public PrivateMemoryAllocator(ulong blockAlignment, MemoryAllocationFlags allocationFlags) : base(blockAlignment, allocationFlags)
        {
        }

        public PrivateMemoryAllocation Allocate(ulong size, ulong alignment)
        {
            Allocation allocation = Allocate(size, alignment, CreateBlock);

            return new PrivateMemoryAllocation(this, allocation.Block, allocation.Offset, allocation.Size);
        }

        private Block CreateBlock(MemoryBlock memory, ulong size)
        {
            return new Block(memory, size);
        }
    }

    class PrivateMemoryAllocatorImpl<T> : IDisposable where T : PrivateMemoryAllocator.Block
    {
        private const ulong InvalidOffset = ulong.MaxValue;

        public readonly struct Allocation
        {
            public T Block { get; }
            public ulong Offset { get; }
            public ulong Size { get; }

            public Allocation(T block, ulong offset, ulong size)
            {
                Block = block;
                Offset = offset;
                Size = size;
            }
        }

        private readonly List<T> _blocks;

        private readonly ulong _blockAlignment;
        private readonly MemoryAllocationFlags _allocationFlags;

        public PrivateMemoryAllocatorImpl(ulong blockAlignment, MemoryAllocationFlags allocationFlags)
        {
            _blocks = [];
            _blockAlignment = blockAlignment;
            _allocationFlags = allocationFlags;
        }

        protected Allocation Allocate(ulong size, ulong alignment, Func<MemoryBlock, ulong, T> createBlock)
        {
            // Ensure we have a sane alignment value.
            if ((ulong)(int)alignment != alignment || (int)alignment <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(alignment), $"Invalid alignment 0x{alignment:X}.");
            }

            for (int i = 0; i < _blocks.Count; i++)
            {
                T block = _blocks[i];

                if (block.Size >= size)
                {
                    ulong offset = block.Allocate(size, alignment);
                    if (offset != InvalidOffset)
                    {
                        return new Allocation(block, offset, size);
                    }
                }
            }

            ulong blockAlignedSize = BitUtils.AlignUp(size, _blockAlignment);

            MemoryBlock memory = new(blockAlignedSize, _allocationFlags);
            T newBlock = createBlock(memory, blockAlignedSize);

            InsertBlock(newBlock);

            ulong newBlockOffset = newBlock.Allocate(size, alignment);
            Debug.Assert(newBlockOffset != InvalidOffset);

            return new Allocation(newBlock, newBlockOffset, size);
        }

        public void Free(PrivateMemoryAllocator.Block block, ulong offset, ulong size)
        {
            block.Free(offset, size);

            if (block.IsTotallyFree())
            {
                for (int i = 0; i < _blocks.Count; i++)
                {
                    if (_blocks[i] == block)
                    {
                        _blocks.RemoveAt(i);
                        break;
                    }
                }

                block.Destroy();
            }
        }

        private void InsertBlock(T block)
        {
            int index = _blocks.BinarySearch(block);
            if (index < 0)
            {
                index = ~index;
            }

            _blocks.Insert(index, block);
        }

        public void Dispose()
        {
            for (int i = 0; i < _blocks.Count; i++)
            {
                _blocks[i].Destroy();
            }

            _blocks.Clear();
        }
    }
}
