using Ryujinx.Cpu.AppleHv.Arm;
using Ryujinx.Memory;
using System;
using System.Runtime.Versioning;

namespace Ryujinx.Cpu.AppleHv
{
    [SupportedOSPlatform("macos")]
    class HvAddressSpace : IDisposable
    {
        private const ulong KernelRegionBase = unchecked((ulong)-(1L << 39));
        private const ulong KernelRegionCodeOffset = 0UL;
        private const ulong KernelRegionCodeSize = 0x2000UL;
        private const ulong KernelRegionTlbiEretOffset = KernelRegionCodeOffset + 0x1000UL;
        private const ulong KernelRegionEretOffset = KernelRegionTlbiEretOffset + 4UL;

        public const ulong KernelRegionEretAddress = KernelRegionBase + KernelRegionEretOffset;
        public const ulong KernelRegionTlbiEretAddress = KernelRegionBase + KernelRegionTlbiEretOffset;

        private const ulong AllocationGranule = 1UL << 14;

        private readonly ulong _asBase;
        private readonly ulong _backingSize;

        private readonly HvAddressSpaceRange _userRange;
        private readonly HvAddressSpaceRange _kernelRange;

        private readonly MemoryBlock _kernelCodeBlock;

        public HvAddressSpace(MemoryBlock backingMemory, ulong asSize)
        {
            (_asBase, HvIpaAllocator ipaAllocator) = HvVm.CreateAddressSpace(backingMemory);
            _backingSize = backingMemory.Size;

            _userRange = new HvAddressSpaceRange(ipaAllocator);
            _kernelRange = new HvAddressSpaceRange(ipaAllocator);

            _kernelCodeBlock = new MemoryBlock(AllocationGranule);

            InitializeKernelCode(ipaAllocator);
        }

        private void InitializeKernelCode(HvIpaAllocator ipaAllocator)
        {
            // Write exception handlers.
            for (ulong offset = 0; offset < 0x800; offset += 0x80)
            {
                // Offsets:
                // 0x0: Synchronous
                // 0x80: IRQ
                // 0x100: FIQ
                // 0x180: SError
                _kernelCodeBlock.Write(KernelRegionCodeOffset + offset, 0xD41FFFE2u); // HVC #0xFFFF
                _kernelCodeBlock.Write(KernelRegionCodeOffset + offset + 4, 0xD69F03E0u); // ERET
            }

            _kernelCodeBlock.Write(KernelRegionTlbiEretOffset, 0xD508831Fu); // TLBI VMALLE1IS
            _kernelCodeBlock.Write(KernelRegionEretOffset, 0xD69F03E0u); // ERET

            ulong kernelCodePa = ipaAllocator.Allocate(AllocationGranule);
            HvApi.hv_vm_map((ulong)_kernelCodeBlock.Pointer, kernelCodePa, AllocationGranule, HvMemoryFlags.Read | HvMemoryFlags.Exec).ThrowOnError();

            _kernelRange.Map(KernelRegionCodeOffset, kernelCodePa, KernelRegionCodeSize, ApFlags.UserNoneKernelReadExecute);
        }

        public void InitializeMmu(ulong vcpu)
        {
            HvApi.hv_vcpu_set_sys_reg(vcpu, HvSysReg.VBAR_EL1, KernelRegionBase + KernelRegionCodeOffset);

            HvApi.hv_vcpu_set_sys_reg(vcpu, HvSysReg.TTBR0_EL1, _userRange.GetIpaBase());
            HvApi.hv_vcpu_set_sys_reg(vcpu, HvSysReg.TTBR1_EL1, _kernelRange.GetIpaBase());
            HvApi.hv_vcpu_set_sys_reg(vcpu, HvSysReg.MAIR_EL1, 0xffUL);
            HvApi.hv_vcpu_set_sys_reg(vcpu, HvSysReg.TCR_EL1, 0x00000011B5193519UL);
            HvApi.hv_vcpu_set_sys_reg(vcpu, HvSysReg.SCTLR_EL1, 0x0000000034D5D925UL);
        }

        public bool GetAndClearUserTlbInvalidationPending()
        {
            return _userRange.GetAndClearTlbInvalidationPending();
        }

        public void MapUser(ulong va, ulong pa, ulong size, MemoryPermission permission)
        {
            pa += _asBase;

            lock (_userRange)
            {
                _userRange.Map(va, pa, size, GetApFlags(permission));
            }
        }

        public void UnmapUser(ulong va, ulong size)
        {
            lock (_userRange)
            {
                _userRange.Unmap(va, size);
            }
        }

        public void ReprotectUser(ulong va, ulong size, MemoryPermission permission)
        {
            lock (_userRange)
            {
                _userRange.Reprotect(va, size, GetApFlags(permission));
            }
        }

        private static ApFlags GetApFlags(MemoryPermission permission)
        {
            return permission switch
            {
                MemoryPermission.None => ApFlags.UserNoneKernelRead,
                MemoryPermission.Execute => ApFlags.UserExecuteKernelRead,
                MemoryPermission.Read => ApFlags.UserReadKernelRead,
                MemoryPermission.ReadAndWrite => ApFlags.UserReadWriteKernelReadWrite,
                MemoryPermission.ReadAndExecute => ApFlags.UserReadExecuteKernelRead,
                MemoryPermission.ReadWriteExecute => ApFlags.UserReadWriteExecuteKernelReadWrite,
                _ => throw new ArgumentException($"Permission \"{permission}\" is invalid."),
            };
        }

        public void Dispose()
        {
            _userRange.Dispose();
            _kernelRange.Dispose();
            HvVm.DestroyAddressSpace(_asBase, _backingSize);
        }
    }
}
