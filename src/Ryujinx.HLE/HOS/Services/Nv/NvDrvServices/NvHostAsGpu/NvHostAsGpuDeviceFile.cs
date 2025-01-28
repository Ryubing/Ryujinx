using Ryujinx.Common.Logging;
using Ryujinx.Graphics.Gpu.Memory;
using Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostAsGpu.Types;
using Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostChannel;
using Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvMap;
using Ryujinx.Memory;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostAsGpu
{
    class NvHostAsGpuDeviceFile : NvDeviceFile
    {
        private const uint SmallPageSize = 0x1000;
        private const uint BigPageSize = 0x10000;

        private static readonly uint[] _pageSizes = [SmallPageSize, BigPageSize];

        private const ulong SmallRegionLimit = 0x400000000UL; // 16 GiB
        private const ulong DefaultUserSize = 1UL << 37;

        private readonly struct VmRegion
        {
            public ulong Start { get; }
            public ulong Limit { get; }

            public VmRegion(ulong start, ulong limit)
            {
                Start = start;
                Limit = limit;
            }
        }

        private static readonly VmRegion[] _vmRegions =
        [
            new((ulong)BigPageSize << 16, SmallRegionLimit),
            new(SmallRegionLimit, DefaultUserSize)
        ];

        private readonly AddressSpaceContext _asContext;
        private readonly NvMemoryAllocator _memoryAllocator;

        public NvHostAsGpuDeviceFile(ServiceCtx context, IVirtualMemoryManager memory, ulong owner) : base(context, owner)
        {
            _asContext = new AddressSpaceContext(context.Device.Gpu.CreateMemoryManager(owner, context.Device.Memory.Size));
            _memoryAllocator = new NvMemoryAllocator();
        }

        public override NvInternalResult Ioctl(NvIoctl command, Span<byte> arguments)
        {
            NvInternalResult result = NvInternalResult.NotImplemented;

            if (command.Type == NvIoctl.NvGpuAsMagic)
            {
                switch (command.Number)
                {
                    case 0x01:
                        result = CallIoctlMethod<BindChannelArguments>(BindChannel, arguments);
                        break;
                    case 0x02:
                        result = CallIoctlMethod<AllocSpaceArguments>(AllocSpace, arguments);
                        break;
                    case 0x03:
                        result = CallIoctlMethod<FreeSpaceArguments>(FreeSpace, arguments);
                        break;
                    case 0x05:
                        result = CallIoctlMethod<UnmapBufferArguments>(UnmapBuffer, arguments);
                        break;
                    case 0x06:
                        result = CallIoctlMethod<MapBufferExArguments>(MapBufferEx, arguments);
                        break;
                    case 0x08:
                        result = CallIoctlMethod<GetVaRegionsArguments>(GetVaRegions, arguments);
                        break;
                    case 0x09:
                        result = CallIoctlMethod<InitializeExArguments>(InitializeEx, arguments);
                        break;
                    case 0x14:
                        result = CallIoctlMethod<RemapArguments>(Remap, arguments);
                        break;
                }
            }

            return result;
        }

        public override NvInternalResult Ioctl3(NvIoctl command, Span<byte> arguments, Span<byte> inlineOutBuffer)
        {
            NvInternalResult result = NvInternalResult.NotImplemented;

            if (command.Type == NvIoctl.NvGpuAsMagic)
            {
                switch (command.Number)
                {
                    case 0x08:
                        // This is the same as the one in ioctl as inlineOutBuffer is empty.
                        result = CallIoctlMethod<GetVaRegionsArguments>(GetVaRegions, arguments);
                        break;
                }
            }

            return result;
        }

        private NvInternalResult BindChannel(ref BindChannelArguments arguments)
        {
            NvHostChannelDeviceFile channelDeviceFile = INvDrvServices.DeviceFileIdRegistry.GetData<NvHostChannelDeviceFile>(arguments.Fd);
            if (channelDeviceFile == null)
            {
                // TODO: Return invalid Fd error.
            }

            channelDeviceFile.Channel.BindMemory(_asContext.Gmm);

            return NvInternalResult.Success;
        }

        private NvInternalResult AllocSpace(ref AllocSpaceArguments arguments)
        {
            ulong size = (ulong)arguments.Pages * (ulong)arguments.PageSize;

            NvInternalResult result = NvInternalResult.Success;

            lock (_asContext)
            {
                // Note: When the fixed offset flag is not set,
                // the Offset field holds the alignment size instead.
                if ((arguments.Flags & AddressSpaceFlags.FixedOffset) != 0)
                {
                    bool regionInUse = _memoryAllocator.IsRegionInUse(arguments.Offset, size, out ulong freeAddressStartPosition);
                    ulong address;

                    if (!regionInUse)
                    {
                        _memoryAllocator.AllocateRange(arguments.Offset, size, freeAddressStartPosition);
                        address = freeAddressStartPosition;
                    }
                    else
                    {
                        address = NvMemoryAllocator.PteUnmapped;
                    }

                    arguments.Offset = address;
                }
                else
                {
                    ulong address = _memoryAllocator.GetFreeAddress(size, out ulong freeAddressStartPosition, arguments.Offset);
                    if (address != NvMemoryAllocator.PteUnmapped)
                    {
                        _memoryAllocator.AllocateRange(address, size, freeAddressStartPosition);
                    }

                    arguments.Offset = address;
                }

                if (arguments.Offset == NvMemoryAllocator.PteUnmapped)
                {
                    arguments.Offset = 0;

                    Logger.Warning?.Print(LogClass.ServiceNv, $"Failed to allocate size {size:x16}!");

                    result = NvInternalResult.OutOfMemory;
                }
                else
                {
                    _asContext.AddReservation(arguments.Offset, size);
                }
            }

            return result;
        }

        private NvInternalResult FreeSpace(ref FreeSpaceArguments arguments)
        {
            ulong size = (ulong)arguments.Pages * (ulong)arguments.PageSize;

            NvInternalResult result = NvInternalResult.Success;

            lock (_asContext)
            {
                if (_asContext.RemoveReservation(arguments.Offset))
                {
                    _memoryAllocator.DeallocateRange(arguments.Offset, size);
                    _asContext.Gmm.Unmap(arguments.Offset, size);
                }
                else
                {
                    Logger.Warning?.Print(LogClass.ServiceNv,
                        $"Failed to free offset 0x{arguments.Offset:x16} size 0x{size:x16}!");

                    result = NvInternalResult.InvalidInput;
                }
            }

            return result;
        }

        private NvInternalResult UnmapBuffer(ref UnmapBufferArguments arguments)
        {
            lock (_asContext)
            {
                if (_asContext.RemoveMap(arguments.Offset, out ulong size))
                {
                    if (size != 0)
                    {
                        _memoryAllocator.DeallocateRange(arguments.Offset, size);
                        _asContext.Gmm.Unmap(arguments.Offset, size);
                    }
                }
                else
                {
                    Logger.Warning?.Print(LogClass.ServiceNv, $"Invalid buffer offset {arguments.Offset:x16}!");
                }
            }

            return NvInternalResult.Success;
        }

        private NvInternalResult MapBufferEx(ref MapBufferExArguments arguments)
        {
            const string MapErrorMsg = "Failed to map fixed buffer with offset 0x{0:x16}, size 0x{1:x16} and alignment 0x{2:x16}!";

            ulong physicalAddress;

            if ((arguments.Flags & AddressSpaceFlags.RemapSubRange) != 0)
            {
                lock (_asContext)
                {
                    if (_asContext.TryGetMapPhysicalAddress(arguments.Offset, out physicalAddress))
                    {
                        ulong virtualAddress = arguments.Offset + arguments.BufferOffset;

                        physicalAddress += arguments.BufferOffset;
                        _asContext.Gmm.Map(physicalAddress, virtualAddress, arguments.MappingSize, (PteKind)arguments.Kind);

                        return NvInternalResult.Success;
                    }
                    else
                    {
                        Logger.Warning?.Print(LogClass.ServiceNv, $"Address 0x{arguments.Offset:x16} not mapped!");

                        return NvInternalResult.InvalidInput;
                    }
                }
            }

            NvMapHandle map = NvMapDeviceFile.GetMapFromHandle(Owner, arguments.NvMapHandle);

            if (map == null)
            {
                Logger.Warning?.Print(LogClass.ServiceNv, $"Invalid NvMap handle 0x{arguments.NvMapHandle:x8}!");

                return NvInternalResult.InvalidInput;
            }

            ulong pageSize = (ulong)arguments.PageSize;

            if (pageSize == 0)
            {
                pageSize = (ulong)map.Align;
            }

            physicalAddress = map.Address + arguments.BufferOffset;

            ulong size = arguments.MappingSize;

            if (size == 0)
            {
                size = map.Size;
            }

            NvInternalResult result = NvInternalResult.Success;

            lock (_asContext)
            {
                // Note: When the fixed offset flag is not set,
                // the Offset field holds the alignment size instead.
                bool virtualAddressAllocated = (arguments.Flags & AddressSpaceFlags.FixedOffset) == 0;

                if (!virtualAddressAllocated)
                {
                    if (_asContext.ValidateFixedBuffer(arguments.Offset, size, pageSize))
                    {
                        _asContext.Gmm.Map(physicalAddress, arguments.Offset, size, (PteKind)arguments.Kind);
                    }
                    else
                    {
                        string message = string.Format(MapErrorMsg, arguments.Offset, size, pageSize);

                        Logger.Warning?.Print(LogClass.ServiceNv, message);

                        result = NvInternalResult.InvalidInput;
                    }
                }
                else
                {
                    ulong va = _memoryAllocator.GetFreeAddress(size, out ulong freeAddressStartPosition, pageSize);
                    if (va != NvMemoryAllocator.PteUnmapped)
                    {
                        _memoryAllocator.AllocateRange(va, size, freeAddressStartPosition);
                    }

                    _asContext.Gmm.Map(physicalAddress, va, size, (PteKind)arguments.Kind);
                    arguments.Offset = va;
                }

                if (arguments.Offset == NvMemoryAllocator.PteUnmapped)
                {
                    arguments.Offset = 0;

                    Logger.Warning?.Print(LogClass.ServiceNv, $"Failed to map size 0x{size:x16}!");

                    result = NvInternalResult.InvalidInput;
                }
                else
                {
                    _asContext.AddMap(arguments.Offset, size, physicalAddress, virtualAddressAllocated);
                }
            }

            return result;
        }

        private NvInternalResult GetVaRegions(ref GetVaRegionsArguments arguments)
        {
            int vaRegionStructSize = Unsafe.SizeOf<VaRegion>();

            Debug.Assert(vaRegionStructSize == 0x18);
            Debug.Assert(_pageSizes.Length == 2);

            uint writeEntries = (uint)(arguments.BufferSize / vaRegionStructSize);
            if (writeEntries > _pageSizes.Length)
            {
                writeEntries = (uint)_pageSizes.Length;
            }

            for (uint i = 0; i < writeEntries; i++)
            {
                ref VaRegion region = ref arguments.Regions[(int)i];

                VmRegion vmRegion = _vmRegions[i];
                uint pageSize = _pageSizes[i];

                region.PageSize = pageSize;
                region.Offset = vmRegion.Start;
                region.Pages = (vmRegion.Limit - vmRegion.Start) / pageSize;
                region.Padding = 0;
            }

            arguments.BufferSize = (uint)(_pageSizes.Length * vaRegionStructSize);

            return NvInternalResult.Success;
        }

        private NvInternalResult InitializeEx(ref InitializeExArguments arguments)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceNv);

            return NvInternalResult.Success;
        }

        private NvInternalResult Remap(Span<RemapArguments> arguments)
        {
            MemoryManager gmm = _asContext.Gmm;

            for (int index = 0; index < arguments.Length; index++)
            {
                ref RemapArguments argument = ref arguments[index];
                ulong gpuVa = (ulong)argument.GpuOffset << 16;
                ulong size = (ulong)argument.Pages << 16;
                int nvmapHandle = argument.NvMapHandle;

                if (nvmapHandle == 0)
                {
                    gmm.Unmap(gpuVa, size);
                }
                else
                {
                    ulong mapOffs = (ulong)argument.MapOffset << 16;
                    PteKind kind = (PteKind)argument.Kind;

                    NvMapHandle map = NvMapDeviceFile.GetMapFromHandle(Owner, nvmapHandle);

                    if (map == null)
                    {
                        Logger.Warning?.Print(LogClass.ServiceNv, $"Invalid NvMap handle 0x{nvmapHandle:x8}!");

                        return NvInternalResult.InvalidInput;
                    }

                    gmm.Map(mapOffs + map.Address, gpuVa, size, kind);
                }
            }

            return NvInternalResult.Success;
        }

        public override void Close() { }
    }
}
