using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using Ryujinx.Cpu;
using Ryujinx.Cpu.AppleHv;
using Ryujinx.Cpu.Jit;
using Ryujinx.Cpu.LightningJit;
using Ryujinx.Graphics.Gpu;
using Ryujinx.HLE.HOS.Kernel;
using Ryujinx.HLE.HOS.Kernel.Process;
using Ryujinx.Memory;
using System;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS
{
    class ArmProcessContextFactory : IProcessContextFactory
    {
        private readonly ITickSource _tickSource;
        private readonly GpuContext _gpu;
        private readonly string _titleIdText;
        private readonly string _displayVersion;
        private readonly bool _diskCacheEnabled;
        private readonly string _diskCacheSelector;
        private readonly ulong _codeAddress;
        private readonly ulong _codeSize;

        public IDiskCacheLoadState DiskCacheLoadState { get; private set; }

        public ArmProcessContextFactory(
            ITickSource tickSource,
            GpuContext gpu,
            string titleIdText,
            string displayVersion,
            bool diskCacheEnabled,
            string diskCacheSelector,
            ulong codeAddress,
            ulong codeSize)
        {
            _tickSource = tickSource;
            _gpu = gpu;
            _titleIdText = titleIdText;
            _displayVersion = displayVersion;
            _diskCacheEnabled = diskCacheEnabled;
            _diskCacheSelector = diskCacheSelector;
            _codeAddress = codeAddress;
            _codeSize = codeSize;
        }

        public IProcessContext Create(KernelContext context, ulong pid, ulong addressSpaceSize, InvalidAccessHandler invalidAccessHandler, bool for64Bit)
        {
            IArmProcessContext processContext;

            bool isArm64Host = RuntimeInformation.ProcessArchitecture == Architecture.Arm64;

            if (OperatingSystem.IsMacOS() && isArm64Host && for64Bit && context.Device.Configuration.UseHypervisor)
            {
                HvEngine cpuEngine = new(_tickSource);
                HvMemoryManager memoryManager = new(context.Memory, addressSpaceSize, invalidAccessHandler);
                processContext = new ArmProcessContext<HvMemoryManager>(pid, cpuEngine, _gpu, memoryManager, addressSpaceSize, for64Bit);
            }
            else
            {
                MemoryManagerMode mode = context.Device.Configuration.MemoryManagerMode;

                if (!MemoryBlock.SupportsFlags(MemoryAllocationFlags.ViewCompatible))
                {
                    Logger.Warning?.Print(LogClass.Cpu, "Host system doesn't support views, falling back to software page table");

                    mode = MemoryManagerMode.SoftwarePageTable;
                }

                ICpuEngine cpuEngine = isArm64Host && (mode == MemoryManagerMode.HostMapped || mode == MemoryManagerMode.HostMappedUnsafe)
                    ? new LightningJitEngine(_tickSource)
                    : new JitEngine(_tickSource);

                AddressSpace addressSpace = null;

                // We want to use host tracked mode if the host page size is > 4KB.
                if ((mode == MemoryManagerMode.HostMapped || mode == MemoryManagerMode.HostMappedUnsafe) && MemoryBlock.GetPageSize() <= 0x1000)
                {
                    if (!AddressSpace.TryCreate(context.Memory, addressSpaceSize, out addressSpace))
                    {
                        Logger.Warning?.Print(LogClass.Cpu, "Address space creation failed, falling back to software page table");

                        mode = MemoryManagerMode.SoftwarePageTable;
                    }
                }

                switch (mode)
                {
                    case MemoryManagerMode.SoftwarePageTable:
                        MemoryManager memoryManager = new(context.Memory, addressSpaceSize, invalidAccessHandler);
                        processContext = new ArmProcessContext<MemoryManager>(pid, cpuEngine, _gpu, memoryManager, addressSpaceSize, for64Bit);
                        break;

                    case MemoryManagerMode.HostMapped:
                    case MemoryManagerMode.HostMappedUnsafe:
                        if (addressSpace == null)
                        {
                            MemoryManagerHostTracked memoryManagerHostTracked = new(context.Memory, addressSpaceSize, mode == MemoryManagerMode.HostMappedUnsafe, invalidAccessHandler);
                            processContext = new ArmProcessContext<MemoryManagerHostTracked>(pid, cpuEngine, _gpu, memoryManagerHostTracked, addressSpaceSize, for64Bit);
                        }
                        else
                        {
                            if (addressSpaceSize != addressSpace.AddressSpaceSize)
                            {
                                Logger.Warning?.Print(LogClass.Emulation, $"Allocated address space (0x{addressSpace.AddressSpaceSize:X}) is smaller than guest application requirements (0x{addressSpaceSize:X})");
                            }

                            MemoryManagerHostMapped memoryManagerHostMapped = new(addressSpace, mode == MemoryManagerMode.HostMappedUnsafe, invalidAccessHandler);
                            processContext = new ArmProcessContext<MemoryManagerHostMapped>(pid, cpuEngine, _gpu, memoryManagerHostMapped, addressSpace.AddressSpaceSize, for64Bit);
                        }
                        break;

                    default:
                        throw new InvalidOperationException($"{nameof(mode)} contains an invalid value: {mode}");
                }
            }

            DiskCacheLoadState = processContext.Initialize(_titleIdText, _displayVersion, _diskCacheEnabled, _codeAddress, _codeSize, _diskCacheSelector ?? "default");

            return processContext;
        }
    }
}
