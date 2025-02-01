using ARMeilleure.Memory;

namespace ARMeilleure.State
{
    public class ExecutionContext
    {
        private const int MinCountForCheck = 4000;

        private readonly NativeContext _nativeContext;

        internal nint NativeContextPtr => _nativeContext.BasePtr;

        private bool _interrupted;

        private readonly ICounter _counter;

        public ulong Pc => _nativeContext.GetPc();

#pragma warning disable CA1822 // Mark member as static
        public uint CtrEl0 => 0x8444c004;
        public uint DczidEl0 => 0x00000004;
#pragma warning restore CA1822

        public ulong CntfrqEl0 => _counter.Frequency;
        public ulong CntpctEl0 => _counter.Counter;

        // CNTVCT_EL0 = CNTPCT_EL0 - CNTVOFF_EL2
        // Since EL2 isn't implemented, CNTVOFF_EL2 = 0
        public ulong CntvctEl0 => CntpctEl0;

        public long TpidrEl0
        {
            get => _nativeContext.GetTpidrEl0();
            set => _nativeContext.SetTpidrEl0(value);
        }

        public long TpidrroEl0
        {
            get => _nativeContext.GetTpidrroEl0();
            set => _nativeContext.SetTpidrroEl0(value);
        }

        public uint Pstate
        {
            get => _nativeContext.GetPstate();
            set => _nativeContext.SetPstate(value);
        }

        public FPSR Fpsr
        {
            get => (FPSR)_nativeContext.GetFPState((uint)FPSR.Mask);
            set => _nativeContext.SetFPState((uint)value, (uint)FPSR.Mask);
        }

        public FPCR Fpcr
        {
            get => (FPCR)_nativeContext.GetFPState((uint)FPCR.Mask);
            set => _nativeContext.SetFPState((uint)value, (uint)FPCR.Mask);
        }
        public FPCR StandardFpcrValue => (Fpcr & (FPCR.Ahp)) | FPCR.Dn | FPCR.Fz;

        public FPSCR Fpscr
        {
            get => (FPSCR)_nativeContext.GetFPState((uint)FPSCR.Mask);
            set => _nativeContext.SetFPState((uint)value, (uint)FPSCR.Mask);
        }

        public bool IsAarch32 { get; set; }

        internal ExecutionMode ExecutionMode
        {
            get
            {
                if (IsAarch32)
                {
                    return GetPstateFlag(PState.TFlag)
                        ? ExecutionMode.Aarch32Thumb
                        : ExecutionMode.Aarch32Arm;
                }
                else
                {
                    return ExecutionMode.Aarch64;
                }
            }
        }

        public bool Running
        {
            get => _nativeContext.GetRunning();
            private set => _nativeContext.SetRunning(value);
        }

        private readonly ExceptionCallbackNoArgs _interruptCallback;
        private readonly ExceptionCallback _breakCallback;
        private readonly ExceptionCallback _supervisorCallback;
        private readonly ExceptionCallback _undefinedCallback;

        public ExecutionContext(
            IJitMemoryAllocator allocator,
            ICounter counter,
            ExceptionCallbackNoArgs interruptCallback = null,
            ExceptionCallback breakCallback = null,
            ExceptionCallback supervisorCallback = null,
            ExceptionCallback undefinedCallback = null)
        {
            _nativeContext = new NativeContext(allocator);
            _counter = counter;
            _interruptCallback = interruptCallback;
            _breakCallback = breakCallback;
            _supervisorCallback = supervisorCallback;
            _undefinedCallback = undefinedCallback;

            Running = true;

            _nativeContext.SetCounter(MinCountForCheck);
        }

        public ulong GetX(int index) => _nativeContext.GetX(index);
        public void SetX(int index, ulong value) => _nativeContext.SetX(index, value);

        public V128 GetV(int index) => _nativeContext.GetV(index);
        public void SetV(int index, V128 value) => _nativeContext.SetV(index, value);

        public bool GetPstateFlag(PState flag) => _nativeContext.GetPstateFlag(flag);
        public void SetPstateFlag(PState flag, bool value) => _nativeContext.SetPstateFlag(flag, value);

        public bool GetFPstateFlag(FPState flag) => _nativeContext.GetFPStateFlag(flag);
        public void SetFPstateFlag(FPState flag, bool value) => _nativeContext.SetFPStateFlag(flag, value);

        internal void CheckInterrupt()
        {
            if (_interrupted)
            {
                _interrupted = false;

                _interruptCallback?.Invoke(this);
            }

            _nativeContext.SetCounter(MinCountForCheck);
        }

        public void RequestInterrupt()
        {
            _interrupted = true;
        }

        internal void OnBreak(ulong address, int imm)
        {
            _breakCallback?.Invoke(this, address, imm);
        }

        internal void OnSupervisorCall(ulong address, int imm)
        {
            _supervisorCallback?.Invoke(this, address, imm);
        }

        internal void OnUndefined(ulong address, int opCode)
        {
            _undefinedCallback?.Invoke(this, address, opCode);
        }

        public void StopRunning()
        {
            Running = false;

            _nativeContext.SetCounter(0);
        }

        public void Dispose()
        {
            _nativeContext.Dispose();
        }
    }
}
