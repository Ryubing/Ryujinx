using ARMeilleure.Memory;
using ARMeilleure.State;
using ARMeilleure.Translation;
using System;
using System.Runtime.InteropServices;

namespace ARMeilleure.Instructions
{
    static class NativeInterface
    {
        private class ThreadContext
        {
            public ExecutionContext Context { get; }
            public IMemoryManager Memory { get; }
            public Translator Translator { get; }

            public ThreadContext(ExecutionContext context, IMemoryManager memory, Translator translator)
            {
                Context = context;
                Memory = memory;
                Translator = translator;
            }
        }

        [ThreadStatic]
        private static ThreadContext Context;

        public static void RegisterThread(ExecutionContext context, IMemoryManager memory, Translator translator)
        {
            Context = new ThreadContext(context, memory, translator);
        }

        public static void UnregisterThread()
        {
            Context = null;
        }

        [UnmanagedCallersOnly]
        public static void Break(ulong address, int imm)
        {
            Statistics.PauseTimer();

            GetContext().OnBreak(address, imm);

            Statistics.ResumeTimer();
        }

        [UnmanagedCallersOnly]
        public static void SupervisorCall(ulong address, int imm)
        {
            Statistics.PauseTimer();

            GetContext().OnSupervisorCall(address, imm);

            Statistics.ResumeTimer();
        }

        [UnmanagedCallersOnly]
        public static void Undefined(ulong address, int opCode)
        {
            Statistics.PauseTimer();

            GetContext().OnUndefined(address, opCode);

            Statistics.ResumeTimer();
        }

        #region "System registers"
        [UnmanagedCallersOnly]
        public static ulong GetCtrEl0()
        {
            return GetContext().CtrEl0;
        }

        [UnmanagedCallersOnly]
        public static ulong GetDczidEl0()
        {
            return GetContext().DczidEl0;
        }

        [UnmanagedCallersOnly]
        public static ulong GetCntfrqEl0()
        {
            return GetContext().CntfrqEl0;
        }

        [UnmanagedCallersOnly]
        public static ulong GetCntpctEl0()
        {
            return GetContext().CntpctEl0;
        }

        [UnmanagedCallersOnly]
        public static ulong GetCntvctEl0()
        {
            return GetContext().CntvctEl0;
        }
        #endregion

        #region "Read"
        [UnmanagedCallersOnly]
        public static byte ReadByte(ulong address)
        {
            return GetMemoryManager().ReadGuest<byte>(address);
        }

        [UnmanagedCallersOnly]
        public static ushort ReadUInt16(ulong address)
        {
            return GetMemoryManager().ReadGuest<ushort>(address);
        }

        [UnmanagedCallersOnly]
        public static uint ReadUInt32(ulong address)
        {
            return GetMemoryManager().ReadGuest<uint>(address);
        }

        [UnmanagedCallersOnly]
        public static ulong ReadUInt64(ulong address)
        {
            return GetMemoryManager().ReadGuest<ulong>(address);
        }

        [UnmanagedCallersOnly]
        public static V128 ReadVector128(ulong address)
        {
            return GetMemoryManager().ReadGuest<V128>(address);
        }
        #endregion

        #region "Write"
        [UnmanagedCallersOnly]
        public static void WriteByte(ulong address, byte value)
        {
            GetMemoryManager().WriteGuest(address, value);
        }

        [UnmanagedCallersOnly]
        public static void WriteUInt16(ulong address, ushort value)
        {
            GetMemoryManager().WriteGuest(address, value);
        }

        [UnmanagedCallersOnly]
        public static void WriteUInt32(ulong address, uint value)
        {
            GetMemoryManager().WriteGuest(address, value);
        }

        [UnmanagedCallersOnly]
        public static void WriteUInt64(ulong address, ulong value)
        {
            GetMemoryManager().WriteGuest(address, value);
        }

        [UnmanagedCallersOnly]
        public static void WriteVector128(ulong address, V128 value)
        {
            GetMemoryManager().WriteGuest(address, value);
        }
        #endregion

        [UnmanagedCallersOnly]
        public static void EnqueueForRejit(ulong address)
        {
            Context.Translator.EnqueueForRejit(address, GetContext().ExecutionMode);
        }

        [UnmanagedCallersOnly]
        public static void SignalMemoryTracking(ulong address, ulong size, byte write)
        {
            GetMemoryManager().SignalMemoryTracking(address, size, write == 1);
        }

        [UnmanagedCallersOnly]
        public static void ThrowInvalidMemoryAccess(ulong address)
        {
            throw new InvalidAccessException(address);
        }

        [UnmanagedCallersOnly]
        public static ulong GetFunctionAddress(ulong address)
        {
            TranslatedFunction function = Context.Translator.GetOrTranslate(address, GetContext().ExecutionMode);

            return (ulong)function.FuncPointer.ToInt64();
        }

        [UnmanagedCallersOnly]
        public static void InvalidateCacheLine(ulong address)
        {
            Context.Translator.InvalidateJitCacheRegion(address, InstEmit.DczSizeInBytes);
        }

        [UnmanagedCallersOnly]
        public static byte CheckSynchronization()
        {
            Statistics.PauseTimer();

            ExecutionContext context = GetContext();

            context.CheckInterrupt();

            Statistics.ResumeTimer();

            return (byte)(context.Running ? 1 : 0);
        }

        public static ExecutionContext GetContext()
        {
            return Context.Context;
        }

        public static IMemoryManager GetMemoryManager()
        {
            return Context.Memory;
        }
    }
}
