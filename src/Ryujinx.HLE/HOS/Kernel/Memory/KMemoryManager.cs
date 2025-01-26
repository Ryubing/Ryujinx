using Ryujinx.HLE.HOS.Kernel.Common;
using System;

namespace Ryujinx.HLE.HOS.Kernel.Memory
{
    class KMemoryManager
    {
        public KMemoryRegionManager[] MemoryRegions { get; }

        public KMemoryManager(MemorySize size, MemoryArrange arrange)
        {
            MemoryRegions = KernelInit.GetMemoryRegions(size, arrange);
        }

        private KMemoryRegionManager GetMemoryRegion(ulong address)
        {
            for (int i = 0; i < MemoryRegions.Length; i++)
            {
                KMemoryRegionManager region = MemoryRegions[i];

                if (address >= region.Address && address < region.EndAddr)
                {
                    return region;
                }
            }

            return null;
        }

        public void IncrementPagesReferenceCount(ulong address, ulong pagesCount)
        {
            IncrementOrDecrementPagesReferenceCount(address, pagesCount, true);
        }

        public void DecrementPagesReferenceCount(ulong address, ulong pagesCount)
        {
            IncrementOrDecrementPagesReferenceCount(address, pagesCount, false);
        }

        private void IncrementOrDecrementPagesReferenceCount(ulong address, ulong pagesCount, bool increment)
        {
            while (pagesCount != 0)
            {
                KMemoryRegionManager region = GetMemoryRegion(address);

                ulong countToProcess = Math.Min(pagesCount, region.GetPageOffsetFromEnd(address));

                lock (region)
                {
                    if (increment)
                    {
                        region.IncrementPagesReferenceCount(address, countToProcess);
                    }
                    else
                    {
                        region.DecrementPagesReferenceCount(address, countToProcess);
                    }
                }

                pagesCount -= countToProcess;
                address += countToProcess * KPageTableBase.PageSize;
            }
        }
    }
}
