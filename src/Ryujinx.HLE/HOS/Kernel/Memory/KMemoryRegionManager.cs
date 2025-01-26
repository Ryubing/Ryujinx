using Ryujinx.Horizon.Common;
using System.Diagnostics;

namespace Ryujinx.HLE.HOS.Kernel.Memory
{
    class KMemoryRegionManager
    {
        private readonly KPageHeap _pageHeap;

        public ulong Address { get; }
        public ulong Size { get; }
        public ulong EndAddr => Address + Size;

        private readonly ushort[] _pageReferenceCounts;

        public KMemoryRegionManager(ulong address, ulong size, ulong endAddr)
        {
            Address = address;
            Size = size;

            _pageReferenceCounts = new ushort[size / KPageTableBase.PageSize];

            _pageHeap = new KPageHeap(address, size);
            _pageHeap.Free(address, size / KPageTableBase.PageSize);
            _pageHeap.UpdateUsedSize();
        }

        public Result AllocatePages(out KPageList pageList, ulong pagesCount)
        {
            if (pagesCount == 0)
            {
                pageList = new KPageList();

                return Result.Success;
            }

            lock (_pageHeap)
            {
                Result result = AllocatePagesImpl(out pageList, pagesCount, false);

                if (result == Result.Success)
                {
                    foreach (KPageNode node in pageList)
                    {
                        IncrementPagesReferenceCount(node.Address, node.PagesCount);
                    }
                }

                return result;
            }
        }

        public ulong AllocatePagesContiguous(KernelContext context, ulong pagesCount, bool backwards)
        {
            if (pagesCount == 0)
            {
                return 0;
            }

            lock (_pageHeap)
            {
                ulong address = AllocatePagesContiguousImpl(pagesCount, 1, backwards);

                if (address != 0)
                {
                    IncrementPagesReferenceCount(address, pagesCount);
                    context.CommitMemory(address - DramMemoryMap.DramBase, pagesCount * KPageTableBase.PageSize);
                }

                return address;
            }
        }

        private Result AllocatePagesImpl(out KPageList pageList, ulong pagesCount, bool random)
        {
            pageList = new KPageList();

            int heapIndex = KPageHeap.GetBlockIndex(pagesCount);

            if (heapIndex < 0)
            {
                return KernelResult.OutOfMemory;
            }

            for (int index = heapIndex; index >= 0; index--)
            {
                ulong pagesPerAlloc = KPageHeap.GetBlockPagesCount(index);

                while (pagesCount >= pagesPerAlloc)
                {
                    ulong allocatedBlock = _pageHeap.AllocateBlock(index, random);

                    if (allocatedBlock == 0)
                    {
                        break;
                    }

                    Result result = pageList.AddRange(allocatedBlock, pagesPerAlloc);

                    if (result != Result.Success)
                    {
                        FreePages(pageList);
                        _pageHeap.Free(allocatedBlock, pagesPerAlloc);

                        return result;
                    }

                    pagesCount -= pagesPerAlloc;
                }
            }

            if (pagesCount != 0)
            {
                FreePages(pageList);

                return KernelResult.OutOfMemory;
            }

            return Result.Success;
        }

        private ulong AllocatePagesContiguousImpl(ulong pagesCount, ulong alignPages, bool random)
        {
            int heapIndex = KPageHeap.GetAlignedBlockIndex(pagesCount, alignPages);

            ulong allocatedBlock = _pageHeap.AllocateBlock(heapIndex, random);

            if (allocatedBlock == 0)
            {
                return 0;
            }

            ulong allocatedPages = KPageHeap.GetBlockPagesCount(heapIndex);

            if (allocatedPages > pagesCount)
            {
                _pageHeap.Free(allocatedBlock + pagesCount * KPageTableBase.PageSize, allocatedPages - pagesCount);
            }

            return allocatedBlock;
        }

        public void FreePage(ulong address)
        {
            lock (_pageHeap)
            {
                _pageHeap.Free(address, 1);
            }
        }

        public void FreePages(KPageList pageList)
        {
            lock (_pageHeap)
            {
                foreach (KPageNode pageNode in pageList)
                {
                    _pageHeap.Free(pageNode.Address, pageNode.PagesCount);
                }
            }
        }

        public void FreePages(ulong address, ulong pagesCount)
        {
            lock (_pageHeap)
            {
                _pageHeap.Free(address, pagesCount);
            }
        }

        public ulong GetFreePages()
        {
            lock (_pageHeap)
            {
                return _pageHeap.GetFreePagesCount();
            }
        }

        public void IncrementPagesReferenceCount(ulong address, ulong pagesCount)
        {
            ulong index = GetPageOffset(address);
            ulong endIndex = index + pagesCount;

            while (index < endIndex)
            {
                ushort referenceCount = ++_pageReferenceCounts[index];
                Debug.Assert(referenceCount >= 1);

                index++;
            }
        }

        public void DecrementPagesReferenceCount(ulong address, ulong pagesCount)
        {
            ulong index = GetPageOffset(address);
            ulong endIndex = index + pagesCount;

            ulong freeBaseIndex = 0;
            ulong freePagesCount = 0;

            while (index < endIndex)
            {
                Debug.Assert(_pageReferenceCounts[index] > 0);
                ushort referenceCount = --_pageReferenceCounts[index];

                if (referenceCount == 0)
                {
                    if (freePagesCount != 0)
                    {
                        freePagesCount++;
                    }
                    else
                    {
                        freeBaseIndex = index;
                        freePagesCount = 1;
                    }
                }
                else if (freePagesCount != 0)
                {
                    FreePages(Address + freeBaseIndex * KPageTableBase.PageSize, freePagesCount);
                    freePagesCount = 0;
                }

                index++;
            }

            if (freePagesCount != 0)
            {
                FreePages(Address + freeBaseIndex * KPageTableBase.PageSize, freePagesCount);
            }
        }

        public ulong GetPageOffset(ulong address)
        {
            return (address - Address) / KPageTableBase.PageSize;
        }

        public ulong GetPageOffsetFromEnd(ulong address)
        {
            return (EndAddr - address) / KPageTableBase.PageSize;
        }
    }
}
