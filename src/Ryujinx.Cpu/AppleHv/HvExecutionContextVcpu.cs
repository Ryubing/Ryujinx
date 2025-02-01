using ARMeilleure.State;
using Ryujinx.Memory;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Ryujinx.Cpu.AppleHv
{
    [SupportedOSPlatform("macos")]
    class HvExecutionContextVcpu : IHvExecutionContext
    {
        private static readonly MemoryBlock _setSimdFpRegFuncMem;
        private delegate HvResult SetSimdFpReg(ulong vcpu, HvSimdFPReg reg, in V128 value, nint funcPtr);
        private static readonly SetSimdFpReg _setSimdFpReg;
        private static readonly nint _setSimdFpRegNativePtr;

        static HvExecutionContextVcpu()
        {
            // .NET does not support passing vectors by value, so we need to pass a pointer and use a native
            // function to load the value into a vector register.
            _setSimdFpRegFuncMem = new MemoryBlock(MemoryBlock.GetPageSize());
            _setSimdFpRegFuncMem.Write(0, 0x3DC00040u); // LDR Q0, [X2]
            _setSimdFpRegFuncMem.Write(4, 0xD61F0060u); // BR X3
            _setSimdFpRegFuncMem.Reprotect(0, _setSimdFpRegFuncMem.Size, MemoryPermission.ReadAndExecute);

            _setSimdFpReg = Marshal.GetDelegateForFunctionPointer<SetSimdFpReg>(_setSimdFpRegFuncMem.Pointer);

            if (NativeLibrary.TryLoad(HvApi.LibraryName, out nint hvLibHandle))
            {
                _setSimdFpRegNativePtr = NativeLibrary.GetExport(hvLibHandle, nameof(HvApi.hv_vcpu_set_simd_fp_reg));
            }
        }

        public ulong Pc
        {
            get
            {
                HvApi.hv_vcpu_get_reg(_vcpu, HvReg.PC, out ulong pc).ThrowOnError();
                return pc;
            }
            set
            {
                HvApi.hv_vcpu_set_reg(_vcpu, HvReg.PC, value).ThrowOnError();
            }
        }

        public ulong ElrEl1
        {
            get
            {
                HvApi.hv_vcpu_get_sys_reg(_vcpu, HvSysReg.ELR_EL1, out ulong elr).ThrowOnError();
                return elr;
            }
            set
            {
                HvApi.hv_vcpu_set_sys_reg(_vcpu, HvSysReg.ELR_EL1, value).ThrowOnError();
            }
        }

        public ulong EsrEl1
        {
            get
            {
                HvApi.hv_vcpu_get_sys_reg(_vcpu, HvSysReg.ESR_EL1, out ulong esr).ThrowOnError();
                return esr;
            }
            set
            {
                HvApi.hv_vcpu_set_sys_reg(_vcpu, HvSysReg.ESR_EL1, value).ThrowOnError();
            }
        }

        public long TpidrEl0
        {
            get
            {
                HvApi.hv_vcpu_get_sys_reg(_vcpu, HvSysReg.TPIDR_EL0, out ulong tpidrEl0).ThrowOnError();
                return (long)tpidrEl0;
            }
            set
            {
                HvApi.hv_vcpu_set_sys_reg(_vcpu, HvSysReg.TPIDR_EL0, (ulong)value).ThrowOnError();
            }
        }

        public long TpidrroEl0
        {
            get
            {
                HvApi.hv_vcpu_get_sys_reg(_vcpu, HvSysReg.TPIDRRO_EL0, out ulong tpidrroEl0).ThrowOnError();
                return (long)tpidrroEl0;
            }
            set
            {
                HvApi.hv_vcpu_set_sys_reg(_vcpu, HvSysReg.TPIDRRO_EL0, (ulong)value).ThrowOnError();
            }
        }

        public uint Pstate
        {
            get
            {
                HvApi.hv_vcpu_get_reg(_vcpu, HvReg.CPSR, out ulong cpsr).ThrowOnError();
                return (uint)cpsr;
            }
            set
            {
                HvApi.hv_vcpu_set_reg(_vcpu, HvReg.CPSR, (ulong)value).ThrowOnError();
            }
        }

        public uint Fpcr
        {
            get
            {
                HvApi.hv_vcpu_get_reg(_vcpu, HvReg.FPCR, out ulong fpcr).ThrowOnError();
                return (uint)fpcr;
            }
            set
            {
                HvApi.hv_vcpu_set_reg(_vcpu, HvReg.FPCR, (ulong)value).ThrowOnError();
            }
        }

        public uint Fpsr
        {
            get
            {
                HvApi.hv_vcpu_get_reg(_vcpu, HvReg.FPSR, out ulong fpsr).ThrowOnError();
                return (uint)fpsr;
            }
            set
            {
                HvApi.hv_vcpu_set_reg(_vcpu, HvReg.FPSR, (ulong)value).ThrowOnError();
            }
        }

        private readonly ulong _vcpu;

        public HvExecutionContextVcpu(ulong vcpu)
        {
            _vcpu = vcpu;
        }

        public ulong GetX(int index)
        {
            if (index == 31)
            {
                HvApi.hv_vcpu_get_sys_reg(_vcpu, HvSysReg.SP_EL0, out ulong value).ThrowOnError();
                return value;
            }
            else
            {
                HvApi.hv_vcpu_get_reg(_vcpu, HvReg.X0 + (uint)index, out ulong value).ThrowOnError();
                return value;
            }
        }

        public void SetX(int index, ulong value)
        {
            if (index == 31)
            {
                HvApi.hv_vcpu_set_sys_reg(_vcpu, HvSysReg.SP_EL0, value).ThrowOnError();
            }
            else
            {
                HvApi.hv_vcpu_set_reg(_vcpu, HvReg.X0 + (uint)index, value).ThrowOnError();
            }
        }

        public V128 GetV(int index)
        {
            HvApi.hv_vcpu_get_simd_fp_reg(_vcpu, HvSimdFPReg.Q0 + (uint)index, out HvSimdFPUchar16 value).ThrowOnError();
            return new V128(value.Low, value.High);
        }

        public void SetV(int index, V128 value)
        {
            _setSimdFpReg(_vcpu, HvSimdFPReg.Q0 + (uint)index, value, _setSimdFpRegNativePtr).ThrowOnError();
        }

        public void RequestInterrupt()
        {
            ulong vcpu = _vcpu;
            HvApi.hv_vcpus_exit(ref vcpu, 1);
        }
    }
}
