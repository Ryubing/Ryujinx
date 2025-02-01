using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using Ryujinx.Common.Memory.PartialUnmaps;
using System.Runtime.InteropServices;
using static ARMeilleure.IntermediateRepresentation.Operand.Factory;

namespace ARMeilleure.Signal
{
    /// <summary>
    /// Methods to handle signals caused by partial unmaps. See the structs for C# implementations of the methods.
    /// </summary>
    internal static partial class WindowsPartialUnmapHandler
    {
        [LibraryImport("kernel32.dll", SetLastError = true, EntryPoint = "LoadLibraryA")]
        private static partial nint LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpFileName);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        private static partial nint GetProcAddress(nint hModule, [MarshalAs(UnmanagedType.LPStr)] string procName);

        private static nint _getCurrentThreadIdPtr;

        public static nint GetCurrentThreadIdFunc()
        {
            if (_getCurrentThreadIdPtr == nint.Zero)
            {
                nint handle = LoadLibrary("kernel32.dll");

                _getCurrentThreadIdPtr = GetProcAddress(handle, "GetCurrentThreadId");
            }

            return _getCurrentThreadIdPtr;
        }

        public static Operand EmitRetryFromAccessViolation(EmitterContext context)
        {
            nint partialRemapStatePtr = PartialUnmapState.GlobalState;
            nint localCountsPtr = nint.Add(partialRemapStatePtr, PartialUnmapState.LocalCountsOffset);

            // Get the lock first.
            EmitNativeReaderLockAcquire(context, nint.Add(partialRemapStatePtr, PartialUnmapState.PartialUnmapLockOffset));

            nint getCurrentThreadId = GetCurrentThreadIdFunc();
            Operand threadId = context.Call(Const((ulong)getCurrentThreadId), OperandType.I32);
            Operand threadIndex = EmitThreadLocalMapIntGetOrReserve(context, localCountsPtr, threadId, Const(0));

            Operand endLabel = Label();
            Operand retry = context.AllocateLocal(OperandType.I32);
            Operand threadIndexValidLabel = Label();

            context.BranchIfFalse(threadIndexValidLabel, context.ICompareEqual(threadIndex, Const(-1)));

            context.Copy(retry, Const(1)); // Always retry when thread local cannot be allocated.

            context.Branch(endLabel);

            context.MarkLabel(threadIndexValidLabel);

            Operand threadLocalPartialUnmapsPtr = EmitThreadLocalMapIntGetValuePtr(context, localCountsPtr, threadIndex);
            Operand threadLocalPartialUnmaps = context.Load(OperandType.I32, threadLocalPartialUnmapsPtr);
            Operand partialUnmapsCount = context.Load(OperandType.I32, Const((ulong)nint.Add(partialRemapStatePtr, PartialUnmapState.PartialUnmapsCountOffset)));

            context.Copy(retry, context.ICompareNotEqual(threadLocalPartialUnmaps, partialUnmapsCount));

            Operand noRetryLabel = Label();

            context.BranchIfFalse(noRetryLabel, retry);

            // if (retry) {

            context.Store(threadLocalPartialUnmapsPtr, partialUnmapsCount);

            context.Branch(endLabel);

            context.MarkLabel(noRetryLabel);

            // }

            context.MarkLabel(endLabel);

            // Finally, release the lock and return the retry value.
            EmitNativeReaderLockRelease(context, nint.Add(partialRemapStatePtr, PartialUnmapState.PartialUnmapLockOffset));

            return retry;
        }

        public static Operand EmitThreadLocalMapIntGetOrReserve(EmitterContext context, nint threadLocalMapPtr, Operand threadId, Operand initialState)
        {
            Operand idsPtr = Const((ulong)nint.Add(threadLocalMapPtr, ThreadLocalMap<int>.ThreadIdsOffset));

            Operand i = context.AllocateLocal(OperandType.I32);

            context.Copy(i, Const(0));

            // (Loop 1) Check all slots for a matching Thread ID (while also trying to allocate)

            Operand endLabel = Label();

            Operand loopLabel = Label();
            context.MarkLabel(loopLabel);

            Operand offset = context.Multiply(i, Const(sizeof(int)));
            Operand idPtr = context.Add(idsPtr, context.SignExtend32(OperandType.I64, offset));

            // Check that this slot has the thread ID.
            Operand existingId = context.CompareAndSwap(idPtr, threadId, threadId);

            // If it was already the thread ID, then we just need to return i.
            context.BranchIfTrue(endLabel, context.ICompareEqual(existingId, threadId));

            context.Copy(i, context.Add(i, Const(1)));

            context.BranchIfTrue(loopLabel, context.ICompareLess(i, Const(ThreadLocalMap<int>.MapSize)));

            // (Loop 2) Try take a slot that is 0 with our Thread ID.

            context.Copy(i, Const(0)); // Reset i.

            Operand loop2Label = Label();
            context.MarkLabel(loop2Label);

            Operand offset2 = context.Multiply(i, Const(sizeof(int)));
            Operand idPtr2 = context.Add(idsPtr, context.SignExtend32(OperandType.I64, offset2));

            // Try and swap in the thread id on top of 0.
            Operand existingId2 = context.CompareAndSwap(idPtr2, Const(0), threadId);

            Operand idNot0Label = Label();

            // If it was 0, then we need to initialize the struct entry and return i.
            context.BranchIfFalse(idNot0Label, context.ICompareEqual(existingId2, Const(0)));

            Operand structsPtr = Const((ulong)nint.Add(threadLocalMapPtr, ThreadLocalMap<int>.StructsOffset));
            Operand structPtr = context.Add(structsPtr, context.SignExtend32(OperandType.I64, offset2));
            context.Store(structPtr, initialState);

            context.Branch(endLabel);

            context.MarkLabel(idNot0Label);

            context.Copy(i, context.Add(i, Const(1)));

            context.BranchIfTrue(loop2Label, context.ICompareLess(i, Const(ThreadLocalMap<int>.MapSize)));

            context.Copy(i, Const(-1)); // Could not place the thread in the list.

            context.MarkLabel(endLabel);

            return context.Copy(i);
        }

        private static Operand EmitThreadLocalMapIntGetValuePtr(EmitterContext context, nint threadLocalMapPtr, Operand index)
        {
            Operand offset = context.Multiply(index, Const(sizeof(int)));
            Operand structsPtr = Const((ulong)nint.Add(threadLocalMapPtr, ThreadLocalMap<int>.StructsOffset));

            return context.Add(structsPtr, context.SignExtend32(OperandType.I64, offset));
        }

        private static void EmitAtomicAddI32(EmitterContext context, Operand ptr, Operand additive)
        {
            Operand loop = Label();
            context.MarkLabel(loop);

            Operand initial = context.Load(OperandType.I32, ptr);
            Operand newValue = context.Add(initial, additive);

            Operand replaced = context.CompareAndSwap(ptr, initial, newValue);

            context.BranchIfFalse(loop, context.ICompareEqual(initial, replaced));
        }

        private static void EmitNativeReaderLockAcquire(EmitterContext context, nint nativeReaderLockPtr)
        {
            Operand writeLockPtr = Const((ulong)nint.Add(nativeReaderLockPtr, NativeReaderWriterLock.WriteLockOffset));

            // Spin until we can acquire the write lock.
            Operand spinLabel = Label();
            context.MarkLabel(spinLabel);

            // Old value must be 0 to continue (we gained the write lock)
            context.BranchIfTrue(spinLabel, context.CompareAndSwap(writeLockPtr, Const(0), Const(1)));

            // Increment reader count.
            EmitAtomicAddI32(context, Const((ulong)nint.Add(nativeReaderLockPtr, NativeReaderWriterLock.ReaderCountOffset)), Const(1));

            // Release write lock.
            context.CompareAndSwap(writeLockPtr, Const(1), Const(0));
        }

        private static void EmitNativeReaderLockRelease(EmitterContext context, nint nativeReaderLockPtr)
        {
            // Decrement reader count.
            EmitAtomicAddI32(context, Const((ulong)nint.Add(nativeReaderLockPtr, NativeReaderWriterLock.ReaderCountOffset)), Const(-1));
        }
    }
}
