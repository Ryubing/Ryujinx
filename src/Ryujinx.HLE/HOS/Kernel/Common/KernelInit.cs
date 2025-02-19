using Ryujinx.HLE.HOS.Kernel.Memory;
using Ryujinx.Horizon.Common;
using System;

namespace Ryujinx.HLE.HOS.Kernel.Common
{
    static class KernelInit
    {
        private readonly struct MemoryRegion
        {
            public ulong Address { get; }
            public ulong Size { get; }

            public ulong EndAddress => Address + Size;

            public MemoryRegion(ulong address, ulong size)
            {
                Address = address;
                Size = size;
            }
        }

        public static void InitializeResourceLimit(KResourceLimit resourceLimit, MemorySize size)
        {
            static void EnsureSuccess(Result result)
            {
                if (result != Result.Success)
                {
                    throw new InvalidOperationException($"Unexpected result \"{result}\".");
                }
            }

            ulong ramSize = KSystemControl.GetDramSize(size);

            EnsureSuccess(resourceLimit.SetLimitValue(LimitableResource.Memory, (long)ramSize));
            EnsureSuccess(resourceLimit.SetLimitValue(LimitableResource.Thread, 800));
            EnsureSuccess(resourceLimit.SetLimitValue(LimitableResource.Event, 700));
            EnsureSuccess(resourceLimit.SetLimitValue(LimitableResource.TransferMemory, 200));
            EnsureSuccess(resourceLimit.SetLimitValue(LimitableResource.Session, 900));

            if (!resourceLimit.Reserve(LimitableResource.Memory, 0) ||
                !resourceLimit.Reserve(LimitableResource.Memory, 0x60000))
            {
                throw new InvalidOperationException("Unexpected failure reserving memory on resource limit.");
            }
        }

        public static KMemoryRegionManager[] GetMemoryRegions(MemorySize size, MemoryArrange arrange)
        {
            ulong poolEnd = KSystemControl.GetDramEndAddress(size);
            ulong applicationPoolSize = KSystemControl.GetApplicationPoolSize(arrange);
            ulong appletPoolSize = KSystemControl.GetAppletPoolSize(arrange);

            MemoryRegion servicePool;
            MemoryRegion nvServicesPool;
            MemoryRegion appletPool;
            MemoryRegion applicationPool;

            ulong nvServicesPoolSize = KSystemControl.GetMinimumNonSecureSystemPoolSize();

            applicationPool = new MemoryRegion(poolEnd - applicationPoolSize, applicationPoolSize);

            ulong nvServicesPoolEnd = applicationPool.Address - appletPoolSize;

            nvServicesPool = new MemoryRegion(nvServicesPoolEnd - nvServicesPoolSize, nvServicesPoolSize);
            appletPool = new MemoryRegion(nvServicesPoolEnd, appletPoolSize);

            // Note: There is an extra region used by the kernel, however
            // since we are doing HLE we are not going to use that memory, so give all
            // the remaining memory space to services.
            ulong servicePoolSize = nvServicesPool.Address - DramMemoryMap.SlabHeapEnd;

            servicePool = new MemoryRegion(DramMemoryMap.SlabHeapEnd, servicePoolSize);

            return
            [
                GetMemoryRegion(applicationPool),
                GetMemoryRegion(appletPool),
                GetMemoryRegion(servicePool),
                GetMemoryRegion(nvServicesPool)
            ];
        }

        private static KMemoryRegionManager GetMemoryRegion(MemoryRegion region)
        {
            return new KMemoryRegionManager(region.Address, region.Size, region.EndAddress);
        }
    }
}
