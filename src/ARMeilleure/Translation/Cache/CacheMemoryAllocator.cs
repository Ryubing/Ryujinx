using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ARMeilleure.Translation.Cache
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
                    _blocks[i] = new MemoryBlock(block.Offset + size, block.Size - size);
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
    }
}
