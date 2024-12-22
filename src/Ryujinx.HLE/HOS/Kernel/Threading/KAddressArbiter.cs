using Ryujinx.HLE.HOS.Kernel.Common;
using Ryujinx.HLE.HOS.Kernel.Process;
using Ryujinx.Horizon.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Ryujinx.HLE.HOS.Kernel.Threading
{
    class KAddressArbiter
    {
        private const int HasListenersMask = 0x40000000;

        private readonly KernelContext _context;

        private readonly List<KThread> _condVarThreads;
        private readonly List<KThread> _arbiterThreads;

        public KAddressArbiter(KernelContext context)
        {
            _context = context;

            _condVarThreads = new List<KThread>();
            _arbiterThreads = new List<KThread>();
        }

        public Result ArbitrateLock(int ownerHandle, ulong mutexAddress, int requesterHandle)
        {
            KThread currentThread = KernelStatic.GetCurrentThread();

            _context.CriticalSection.Enter();

            if (currentThread.TerminationRequested)
            {
                _context.CriticalSection.Leave();

                return KernelResult.ThreadTerminating;
            }

            currentThread.SignaledObj = null;
            currentThread.ObjSyncResult = Result.Success;

            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            if (!KernelTransfer.UserToKernel(out int mutexValue, mutexAddress))
            {
                _context.CriticalSection.Leave();

                return KernelResult.InvalidMemState;
            }

            if (mutexValue != (ownerHandle | HasListenersMask))
            {
                _context.CriticalSection.Leave();

                return Result.Success;
            }

            KThread mutexOwner = currentProcess.HandleTable.GetObject<KThread>(ownerHandle);

            if (mutexOwner == null)
            {
                _context.CriticalSection.Leave();

                return KernelResult.InvalidHandle;
            }

            currentThread.MutexAddress = mutexAddress;
            currentThread.ThreadHandleForUserMutex = requesterHandle;

            mutexOwner.AddMutexWaiter(currentThread);

            currentThread.Reschedule(ThreadSchedState.Paused);

            _context.CriticalSection.Leave();
            _context.CriticalSection.Enter();

            currentThread.MutexOwner?.RemoveMutexWaiter(currentThread);

            _context.CriticalSection.Leave();

            return currentThread.ObjSyncResult;
        }

        public Result ArbitrateUnlock(ulong mutexAddress)
        {
            _context.CriticalSection.Enter();

            KThread currentThread = KernelStatic.GetCurrentThread();

            (int mutexValue, KThread newOwnerThread) = MutexUnlock(currentThread, mutexAddress);

            Result result = Result.Success;

            if (!KernelTransfer.KernelToUser(mutexAddress, mutexValue))
            {
                result = KernelResult.InvalidMemState;
            }

            if (result != Result.Success && newOwnerThread != null)
            {
                newOwnerThread.SignaledObj = null;
                newOwnerThread.ObjSyncResult = result;
            }

            _context.CriticalSection.Leave();

            return result;
        }

        public Result WaitProcessWideKeyAtomic(ulong mutexAddress, ulong condVarAddress, int threadHandle, long timeout)
        {
            _context.CriticalSection.Enter();

            KThread currentThread = KernelStatic.GetCurrentThread();

            currentThread.SignaledObj = null;
            currentThread.ObjSyncResult = KernelResult.TimedOut;

            if (currentThread.TerminationRequested)
            {
                _context.CriticalSection.Leave();

                return KernelResult.ThreadTerminating;
            }

            (int mutexValue, _) = MutexUnlock(currentThread, mutexAddress);

            KernelTransfer.KernelToUser(condVarAddress, 1);

            if (!KernelTransfer.KernelToUser(mutexAddress, mutexValue))
            {
                _context.CriticalSection.Leave();

                return KernelResult.InvalidMemState;
            }

            currentThread.MutexAddress = mutexAddress;
            currentThread.ThreadHandleForUserMutex = threadHandle;
            currentThread.CondVarAddress = condVarAddress;

            _condVarThreads.Add(currentThread);

            if (timeout != 0)
            {
                currentThread.Reschedule(ThreadSchedState.Paused);

                if (timeout > 0)
                {
                    _context.TimeManager.ScheduleFutureInvocation(currentThread, timeout);
                }
            }

            _context.CriticalSection.Leave();

            if (timeout > 0)
            {
                _context.TimeManager.UnscheduleFutureInvocation(currentThread);
            }

            _context.CriticalSection.Enter();

            currentThread.MutexOwner?.RemoveMutexWaiter(currentThread);

            _condVarThreads.Remove(currentThread);

            _context.CriticalSection.Leave();

            return currentThread.ObjSyncResult;
        }

        private static (int, KThread) MutexUnlock(KThread currentThread, ulong mutexAddress)
        {
            KThread newOwnerThread = currentThread.RelinquishMutex(mutexAddress, out int count);

            int mutexValue = 0;

            if (newOwnerThread != null)
            {
                mutexValue = newOwnerThread.ThreadHandleForUserMutex;

                if (count >= 2)
                {
                    mutexValue |= HasListenersMask;
                }

                newOwnerThread.SignaledObj = null;
                newOwnerThread.ObjSyncResult = Result.Success;

                newOwnerThread.ReleaseAndResume();
            }

            return (mutexValue, newOwnerThread);
        }

        public void SignalProcessWideKey(ulong address, int count)
        {
            _context.CriticalSection.Enter();

            WakeThreads(_condVarThreads, count, TryAcquireMutex, x => x.CondVarAddress == address);

            if (!_condVarThreads.Any(x => x.CondVarAddress == address))
            {
                KernelTransfer.KernelToUser(address, 0);
            }

            _context.CriticalSection.Leave();
        }

        private static void TryAcquireMutex(KThread requester)
        {
            ulong address = requester.MutexAddress;

            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            if (!currentProcess.CpuMemory.IsMapped(address))
            {
                // Invalid address.
                requester.SignaledObj = null;
                requester.ObjSyncResult = KernelResult.InvalidMemState;

                return;
            }

            ref int mutexRef = ref currentProcess.CpuMemory.GetRef<int>(address);

            int mutexValue, newMutexValue;

            do
            {
                mutexValue = mutexRef;

                if (mutexValue != 0)
                {
                    // Update value to indicate there is a mutex waiter now.
                    newMutexValue = mutexValue | HasListenersMask;
                }
                else
                {
                    // No thread owning the mutex, assign to requesting thread.
                    newMutexValue = requester.ThreadHandleForUserMutex;
                }
            }
            while (Interlocked.CompareExchange(ref mutexRef, newMutexValue, mutexValue) != mutexValue);

            if (mutexValue == 0)
            {
                // We now own the mutex.
                requester.SignaledObj = null;
                requester.ObjSyncResult = Result.Success;

                requester.ReleaseAndResume();

                return;
            }

            mutexValue &= ~HasListenersMask;

            KThread mutexOwner = currentProcess.HandleTable.GetObject<KThread>(mutexValue);

            if (mutexOwner != null)
            {
                // Mutex already belongs to another thread, wait for it.
                mutexOwner.AddMutexWaiter(requester);
            }
            else
            {
                // Invalid mutex owner.
                requester.SignaledObj = null;
                requester.ObjSyncResult = KernelResult.InvalidHandle;

                requester.ReleaseAndResume();
            }
        }

        public Result WaitForAddressIfEqual(ulong address, int value, long timeout)
        {
            KThread currentThread = KernelStatic.GetCurrentThread();

            _context.CriticalSection.Enter();

            if (currentThread.TerminationRequested)
            {
                _context.CriticalSection.Leave();

                return KernelResult.ThreadTerminating;
            }

            currentThread.SignaledObj = null;
            currentThread.ObjSyncResult = KernelResult.TimedOut;

            if (!KernelTransfer.UserToKernel(out int currentValue, address))
            {
                _context.CriticalSection.Leave();

                return KernelResult.InvalidMemState;
            }

            if (currentValue == value)
            {
                if (timeout == 0)
                {
                    _context.CriticalSection.Leave();

                    return KernelResult.TimedOut;
                }

                currentThread.MutexAddress = address;
                currentThread.WaitingInArbitration = true;

                _arbiterThreads.Add(currentThread);

                currentThread.Reschedule(ThreadSchedState.Paused);

                if (timeout > 0)
                {
                    _context.TimeManager.ScheduleFutureInvocation(currentThread, timeout);
                }

                _context.CriticalSection.Leave();

                if (timeout > 0)
                {
                    _context.TimeManager.UnscheduleFutureInvocation(currentThread);
                }

                _context.CriticalSection.Enter();

                if (currentThread.WaitingInArbitration)
                {
                    _arbiterThreads.Remove(currentThread);

                    currentThread.WaitingInArbitration = false;
                }

                _context.CriticalSection.Leave();

                return currentThread.ObjSyncResult;
            }

            _context.CriticalSection.Leave();

            return KernelResult.InvalidState;
        }

        public Result WaitForAddressIfLessThan(ulong address, int value, bool shouldDecrement, long timeout)
        {
            KThread currentThread = KernelStatic.GetCurrentThread();

            _context.CriticalSection.Enter();

            if (currentThread.TerminationRequested)
            {
                _context.CriticalSection.Leave();

                return KernelResult.ThreadTerminating;
            }

            currentThread.SignaledObj = null;
            currentThread.ObjSyncResult = KernelResult.TimedOut;

            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            if (!KernelTransfer.UserToKernel(out int currentValue, address))
            {
                _context.CriticalSection.Leave();

                return KernelResult.InvalidMemState;
            }

            if (shouldDecrement)
            {
                currentValue = Interlocked.Decrement(ref currentProcess.CpuMemory.GetRef<int>(address)) + 1;
            }

            if (currentValue < value)
            {
                if (timeout == 0)
                {
                    _context.CriticalSection.Leave();

                    return KernelResult.TimedOut;
                }

                currentThread.MutexAddress = address;
                currentThread.WaitingInArbitration = true;

                _arbiterThreads.Add(currentThread);

                currentThread.Reschedule(ThreadSchedState.Paused);

                if (timeout > 0)
                {
                    _context.TimeManager.ScheduleFutureInvocation(currentThread, timeout);
                }

                _context.CriticalSection.Leave();

                if (timeout > 0)
                {
                    _context.TimeManager.UnscheduleFutureInvocation(currentThread);
                }

                _context.CriticalSection.Enter();

                if (currentThread.WaitingInArbitration)
                {
                    _arbiterThreads.Remove(currentThread);

                    currentThread.WaitingInArbitration = false;
                }

                _context.CriticalSection.Leave();

                return currentThread.ObjSyncResult;
            }

            _context.CriticalSection.Leave();

            return KernelResult.InvalidState;
        }

        public Result Signal(ulong address, int count)
        {
            _context.CriticalSection.Enter();

            WakeArbiterThreads(address, count);

            _context.CriticalSection.Leave();

            return Result.Success;
        }

        public Result SignalAndIncrementIfEqual(ulong address, int value, int count)
        {
            _context.CriticalSection.Enter();

            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            if (!currentProcess.CpuMemory.IsMapped(address))
            {
                _context.CriticalSection.Leave();

                return KernelResult.InvalidMemState;
            }

            ref int valueRef = ref currentProcess.CpuMemory.GetRef<int>(address);

            int currentValue;

            do
            {
                currentValue = valueRef;

                if (currentValue != value)
                {
                    _context.CriticalSection.Leave();

                    return KernelResult.InvalidState;
                }
            }
            while (Interlocked.CompareExchange(ref valueRef, currentValue + 1, currentValue) != currentValue);

            WakeArbiterThreads(address, count);

            _context.CriticalSection.Leave();

            return Result.Success;
        }

        public Result SignalAndModifyIfEqual(ulong address, int value, int count)
        {
            _context.CriticalSection.Enter();

            int addend;

            // The value is decremented if the number of threads waiting is less
            // or equal to the Count of threads to be signaled, or Count is zero
            // or negative. It is incremented if there are no threads waiting.
            int waitingCount = 0;

            foreach (KThread thread in _arbiterThreads.Where(x => x.MutexAddress == address))
            {
                if (++waitingCount >= count)
                {
                    break;
                }
            }

            if (waitingCount > 0)
            {
                if (count <= 0)
                {
                    addend = -2;
                }
                else if (waitingCount < count)
                {
                    addend = -1;
                }
                else
                {
                    addend = 0;
                }
            }
            else
            {
                addend = 1;
            }

            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            if (!currentProcess.CpuMemory.IsMapped(address))
            {
                _context.CriticalSection.Leave();

                return KernelResult.InvalidMemState;
            }

            ref int valueRef = ref currentProcess.CpuMemory.GetRef<int>(address);

            int currentValue;

            do
            {
                currentValue = valueRef;

                if (currentValue != value)
                {
                    _context.CriticalSection.Leave();

                    return KernelResult.InvalidState;
                }
            }
            while (Interlocked.CompareExchange(ref valueRef, currentValue + addend, currentValue) != currentValue);

            WakeArbiterThreads(address, count);

            _context.CriticalSection.Leave();

            return Result.Success;
        }

        private void WakeArbiterThreads(ulong address, int count)
        {
            static void RemoveArbiterThread(KThread thread)
            {
                thread.SignaledObj = null;
                thread.ObjSyncResult = Result.Success;

                thread.ReleaseAndResume();

                thread.WaitingInArbitration = false;
            }

            WakeThreads(_arbiterThreads, count, RemoveArbiterThread, x => x.MutexAddress == address);
        }

        private static void WakeThreads(
            List<KThread> threads,
            int count,
            Action<KThread> removeCallback,
            Func<KThread, bool> predicate)
        {
            var candidates = threads.Where(predicate).OrderBy(x => x.DynamicPriority);
            var toSignal = (count > 0 ? candidates.Take(count) : candidates).ToArray();

            foreach (KThread thread in toSignal)
            {
                removeCallback(thread);
                threads.Remove(thread);
            }
        }
    }
}
