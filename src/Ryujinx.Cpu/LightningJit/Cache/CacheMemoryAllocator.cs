using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Ryujinx.Cpu.LightningJit.Cache
{
    class CacheMemoryAllocator
    {
        private readonly struct MemoryBlock : IComparable<MemoryBlock>
        {
            public int Offset { get; }
            public int Size { get; }

            public MemoryBlock(int offset, int size)
            {
                Offset = offset;
                Size = size;
            }

            public int CompareTo([AllowNull] MemoryBlock other)
            {
                return Offset.CompareTo(other.Offset);
            }
        }

        private readonly List<MemoryBlock> _blocks = [];

        public CacheMemoryAllocator(int capacity)
        {
            _blocks.Add(new MemoryBlock(0, capacity));
        }

        public int Allocate(int size)
        {
            for (int i = 0; i < _blocks.Count; i++)
            {
                MemoryBlock block = _blocks[i];

                if (block.Size > size)
                {
                    _blocks[i] = new(block.Offset + size, block.Size - size);
                    return block.Offset;
                }
                else if (block.Size == size)
                {
                    _blocks.RemoveAt(i);
                    return block.Offset;
                }
            }

            // We don't have enough free memory to perform the allocation.
            return -1;
        }

        public void ForceAllocation(int offset, int size)
        {
            int index = _blocks.BinarySearch(new(offset, size));

            if (index < 0)
            {
                index = ~index;
            }

            int endOffset = offset + size;

            MemoryBlock block = _blocks[index];

            Debug.Assert(block.Offset <= offset && block.Offset + block.Size >= endOffset);

            if (offset > block.Offset && endOffset < block.Offset + block.Size)
            {
                _blocks[index] = new(block.Offset, offset - block.Offset);
                _blocks.Insert(index + 1, new(endOffset, (block.Offset + block.Size) - endOffset));
            }
            else if (offset > block.Offset)
            {
                _blocks[index] = new(block.Offset, offset - block.Offset);
            }
            else if (endOffset < block.Offset + block.Size)
            {
                _blocks[index] = new(endOffset, (block.Offset + block.Size) - endOffset);
            }
            else
            {
                _blocks.RemoveAt(index);
            }
        }

        public void Free(int offset, int size)
        {
            Insert(new MemoryBlock(offset, size));
        }

        private void Insert(MemoryBlock block)
        {
            int index = _blocks.BinarySearch(block);

            if (index < 0)
            {
                index = ~index;
            }

            if (index < _blocks.Count)
            {
                MemoryBlock next = _blocks[index];

                int endOffs = block.Offset + block.Size;

                if (next.Offset == endOffs)
                {
                    block = new MemoryBlock(block.Offset, block.Size + next.Size);
                    _blocks.RemoveAt(index);
                }
            }

            if (index > 0)
            {
                MemoryBlock prev = _blocks[index - 1];

                if (prev.Offset + prev.Size == block.Offset)
                {
                    block = new MemoryBlock(block.Offset - prev.Size, block.Size + prev.Size);
                    _blocks.RemoveAt(--index);
                }
            }

            _blocks.Insert(index, block);
        }

        public void Clear()
        {
            _blocks.Clear();
        }
    }
}
