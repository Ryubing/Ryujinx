using Ryujinx.Common;
using System;

namespace Ryujinx.HLE.HOS.Kernel.Memory
{
    class KPageHeap
    {
        private class Block
        {
            private readonly KPageBitmap _bitmap = new();
            private ulong _heapAddress;
            private ulong _endOffset;

            public int Shift { get; private set; }
            public int NextShift { get; private set; }
            public ulong Size => 1UL << Shift;
            public int PagesCount => (int)(Size / KPageTableBase.PageSize);
            public int FreeBlocksCount => _bitmap.BitsCount;
            public int FreePagesCount => FreeBlocksCount * PagesCount;

            public ArraySegment<ulong> Initialize(ulong address, ulong size, int blockShift, int nextBlockShift, ArraySegment<ulong> bitStorage)
            {
                Shift = blockShift;
                NextShift = nextBlockShift;

                ulong endAddress = address + size;

                ulong align = nextBlockShift != 0
                    ? 1UL << nextBlockShift
                    : 1UL << blockShift;

                address = BitUtils.AlignDown(address, align);
                endAddress = BitUtils.AlignUp(endAddress, align);

                _heapAddress = address;
                _endOffset = (endAddress - address) / (1UL << blockShift);

                return _bitmap.Initialize(bitStorage, _endOffset);
            }

            public ulong PushBlock(ulong address)
            {
                ulong offset = (address - _heapAddress) >> Shift;

                _bitmap.SetBit(offset);

                if (NextShift != 0)
                {
                    int diff = 1 << (NextShift - Shift);

                    offset = BitUtils.AlignDown(offset, (ulong)diff);

                    if (_bitmap.ClearRange(offset, diff))
                    {
                        return _heapAddress + (offset << Shift);
                    }
                }

                return 0;
            }

            public ulong PopBlock(bool random)
            {
                long sOffset = (long)_bitmap.FindFreeBlock(random);

                if (sOffset < 0L)
                {
                    return 0;
                }

                ulong offset = (ulong)sOffset;

                _bitmap.ClearBit(offset);

                return _heapAddress + (offset << Shift);
            }

            public static int CalculateManagementOverheadSize(ulong regionSize, int currBlockShift, int nextBlockShift)
            {
                ulong currBlockSize = 1UL << currBlockShift;
                ulong nextBlockSize = 1UL << nextBlockShift;
                ulong align = nextBlockShift != 0 ? nextBlockSize : currBlockSize;
                return KPageBitmap.CalculateManagementOverheadSize((align * 2 + BitUtils.AlignUp(regionSize, align)) / currBlockSize);
            }
        }

        private static readonly int[] _memoryBlockPageShifts = [12, 16, 21, 22, 25, 29, 30];

#pragma warning disable IDE0052 // Remove unread private member
        private readonly ulong _heapAddress;
        private readonly ulong _heapSize;
        private ulong _usedSize;
#pragma warning restore IDE0052
        private readonly int _blocksCount;
        private readonly Block[] _blocks;

        public KPageHeap(ulong address, ulong size) : this(address, size, _memoryBlockPageShifts)
        {
        }

        public KPageHeap(ulong address, ulong size, int[] blockShifts)
        {
            _heapAddress = address;
            _heapSize = size;
            _blocksCount = blockShifts.Length;
            _blocks = new Block[_memoryBlockPageShifts.Length];

            ArraySegment<ulong> currBitmapStorage = new(new ulong[CalculateManagementOverheadSize(size, blockShifts)]);

            for (int i = 0; i < blockShifts.Length; i++)
            {
                int currBlockShift = blockShifts[i];
                int nextBlockShift = i != blockShifts.Length - 1 ? blockShifts[i + 1] : 0;

                _blocks[i] = new Block();

                currBitmapStorage = _blocks[i].Initialize(address, size, currBlockShift, nextBlockShift, currBitmapStorage);
            }
        }

        public void UpdateUsedSize()
        {
            _usedSize = _heapSize - (GetFreePagesCount() * KPageTableBase.PageSize);
        }

        public ulong GetFreePagesCount()
        {
            ulong freeCount = 0;

            for (int i = 0; i < _blocksCount; i++)
            {
                freeCount += (ulong)_blocks[i].FreePagesCount;
            }

            return freeCount;
        }

        public ulong AllocateBlock(int index, bool random)
        {
            ulong neededSize = _blocks[index].Size;

            for (int i = index; i < _blocksCount; i++)
            {
                ulong address = _blocks[i].PopBlock(random);

                if (address != 0)
                {
                    ulong allocatedSize = _blocks[i].Size;

                    if (allocatedSize > neededSize)
                    {
                        Free(address + neededSize, (allocatedSize - neededSize) / KPageTableBase.PageSize);
                    }

                    return address;
                }
            }

            return 0;
        }

        private void FreeBlock(ulong block, int index)
        {
            do
            {
                block = _blocks[index++].PushBlock(block);
            }
            while (block != 0);
        }

        public void Free(ulong address, ulong pagesCount)
        {
            if (pagesCount == 0)
            {
                return;
            }

            int bigIndex = _blocksCount - 1;

            ulong start = address;
            ulong end = address + pagesCount * KPageTableBase.PageSize;
            ulong beforeStart = start;
            ulong beforeEnd = start;
            ulong afterStart = end;
            ulong afterEnd = end;

            while (bigIndex >= 0)
            {
                ulong blockSize = _blocks[bigIndex].Size;

                ulong bigStart = BitUtils.AlignUp(start, blockSize);
                ulong bigEnd = BitUtils.AlignDown(end, blockSize);

                if (bigStart < bigEnd)
                {
                    for (ulong block = bigStart; block < bigEnd; block += blockSize)
                    {
                        FreeBlock(block, bigIndex);
                    }

                    beforeEnd = bigStart;
                    afterStart = bigEnd;

                    break;
                }

                bigIndex--;
            }

            for (int i = bigIndex - 1; i >= 0; i--)
            {
                ulong blockSize = _blocks[i].Size;

                while (beforeStart + blockSize <= beforeEnd)
                {
                    beforeEnd -= blockSize;
                    FreeBlock(beforeEnd, i);
                }
            }

            for (int i = bigIndex - 1; i >= 0; i--)
            {
                ulong blockSize = _blocks[i].Size;

                while (afterStart + blockSize <= afterEnd)
                {
                    FreeBlock(afterStart, i);
                    afterStart += blockSize;
                }
            }
        }

        public static int GetAlignedBlockIndex(ulong pagesCount, ulong alignPages)
        {
            ulong targetPages = Math.Max(pagesCount, alignPages);

            for (int i = 0; i < _memoryBlockPageShifts.Length; i++)
            {
                if (targetPages <= GetBlockPagesCount(i))
                {
                    return i;
                }
            }

            return -1;
        }

        public static int GetBlockIndex(ulong pagesCount)
        {
            for (int i = _memoryBlockPageShifts.Length - 1; i >= 0; i--)
            {
                if (pagesCount >= GetBlockPagesCount(i))
                {
                    return i;
                }
            }

            return -1;
        }

        public static ulong GetBlockSize(int index)
        {
            return 1UL << _memoryBlockPageShifts[index];
        }

        public static ulong GetBlockPagesCount(int index)
        {
            return GetBlockSize(index) / KPageTableBase.PageSize;
        }

        private static int CalculateManagementOverheadSize(ulong regionSize, int[] blockShifts)
        {
            int overheadSize = 0;

            for (int i = 0; i < blockShifts.Length; i++)
            {
                int currBlockShift = blockShifts[i];
                int nextBlockShift = i != blockShifts.Length - 1 ? blockShifts[i + 1] : 0;
                overheadSize += Block.CalculateManagementOverheadSize(regionSize, currBlockShift, nextBlockShift);
            }

            return BitUtils.AlignUp(overheadSize, KPageTableBase.PageSize);
        }
    }
}
