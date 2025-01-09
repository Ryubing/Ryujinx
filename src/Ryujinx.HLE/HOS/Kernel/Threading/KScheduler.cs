using Ryujinx.Common;
using Ryujinx.HLE.HOS.Kernel.Process;
using System;
using System.Numerics;
using System.Threading;

namespace Ryujinx.HLE.HOS.Kernel.Threading
{
    partial class KScheduler : IDisposable
    {
        public const int PrioritiesCount = 64;
        public static int CpuCoresCount;

        private const int RoundRobinTimeQuantumMs = 10;

        private static int[] _srcCoresHighestPrioThreads;

        private readonly KernelContext _context;
        private readonly int _coreId;

        private struct SchedulingState
        {
            public volatile bool NeedsScheduling;
            public volatile KThread SelectedThread;
        }

        private SchedulingState _state;

        private KThread _previousThread;
        private KThread _currentThread;

        private int _coreIdleLock;
        private bool _idleSignalled = true;
        private bool _idleActive = true;
        private long _idleTimeRunning;

        public KThread PreviousThread => _previousThread;
        public KThread CurrentThread => _currentThread;
        public long LastContextSwitchTime { get; private set; }
        public long TotalIdleTimeTicks => _idleTimeRunning;

        public KScheduler(KernelContext context, int coreId)
        {
            _context = context;
            _coreId = coreId;

            _currentThread = null;

            if (_srcCoresHighestPrioThreads == null)
            {
                _srcCoresHighestPrioThreads = new int[CpuCoresCount];
            }
        }

        private static int PreemptionPriorities(int index)
        {
            return index == CpuCoresCount - 1 ? 63 : 59;
        }

        public static ulong SelectThreads(KernelContext context)
        {
            if (context.ThreadReselectionRequested)
            {
                return SelectThreadsImpl(context);
            }
            else
            {
                return 0UL;
            }
        }

        private static ulong SelectThreadsImpl(KernelContext context)
        {
            context.ThreadReselectionRequested = false;

            ulong scheduledCoresMask = 0UL;

            for (int core = 0; core < CpuCoresCount; core++)
            {
                KThread thread = context.PriorityQueue.ScheduledThreadsFirstOrDefault(core);

                if (thread != null &&
                    thread.Owner != null &&
                    thread.Owner.PinnedThreads[core] != null &&
                    thread.Owner.PinnedThreads[core] != thread)
                {
                    KThread candidate = thread.Owner.PinnedThreads[core];

                    if (candidate.KernelWaitersCount == 0 && !KProcess.IsExceptionUserThread(candidate))
                    {
                        if (candidate.SchedFlags == ThreadSchedState.Running)
                        {
                            thread = candidate;
                        }
                        else
                        {
                            thread = null;
                        }
                    }
                }

                scheduledCoresMask |= context.Schedulers[core].SelectThread(thread);
            }

            for (int core = 0; core < CpuCoresCount; core++)
            {
                // If the core is not idle (there's already a thread running on it),
                // then we don't need to attempt load balancing.
                if (context.PriorityQueue.HasScheduledThreads(core))
                {
                    continue;
                }

                Array.Fill(_srcCoresHighestPrioThreads, 0);

                int srcCoresHighestPrioThreadsCount = 0;

                KThread dst = null;

                // Select candidate threads that could run on this core.
                // Give preference to threads that are not yet selected.
                foreach (KThread suggested in context.PriorityQueue.SuggestedThreads(core))
                {
                    if (suggested.ActiveCore < 0 || suggested != context.Schedulers[suggested.ActiveCore]._state.SelectedThread)
                    {
                        dst = suggested;
                        break;
                    }

                    _srcCoresHighestPrioThreads[srcCoresHighestPrioThreadsCount++] = suggested.ActiveCore;
                }

                // Not yet selected candidate found.
                if (dst != null)
                {
                    // Priorities < 2 are used for the kernel message dispatching
                    // threads, we should skip load balancing entirely.
                    if (dst.DynamicPriority >= 2)
                    {
                        context.PriorityQueue.TransferToCore(dst.DynamicPriority, core, dst);

                        scheduledCoresMask |= context.Schedulers[core].SelectThread(dst);
                    }

                    continue;
                }

                // All candidates are already selected, choose the best one
                // (the first one that doesn't make the source core idle if moved).
                for (int index = 0; index < srcCoresHighestPrioThreadsCount; index++)
                {
                    int srcCore = _srcCoresHighestPrioThreads[index];

                    KThread src = context.PriorityQueue.ScheduledThreadsElementAtOrDefault(srcCore, 1);

                    if (src != null)
                    {
                        // Run the second thread on the queue on the source core,
                        // move the first one to the current core.
                        KThread origSelectedCoreSrc = context.Schedulers[srcCore]._state.SelectedThread;

                        scheduledCoresMask |= context.Schedulers[srcCore].SelectThread(src);

                        context.PriorityQueue.TransferToCore(origSelectedCoreSrc.DynamicPriority, core, origSelectedCoreSrc);

                        scheduledCoresMask |= context.Schedulers[core].SelectThread(origSelectedCoreSrc);
                    }
                }
            }

            return scheduledCoresMask;
        }

        private ulong SelectThread(KThread nextThread)
        {
            KThread previousThread = _state.SelectedThread;

            if (previousThread != nextThread)
            {
                if (previousThread != null)
                {
                    previousThread.LastScheduledTime = PerformanceCounter.ElapsedTicks;
                }

                _state.SelectedThread = nextThread;
                _state.NeedsScheduling = true;
                return 1UL << _coreId;
            }
            else
            {
                return 0UL;
            }
        }

        public static void EnableScheduling(KernelContext context, ulong scheduledCoresMask)
        {
            KScheduler currentScheduler = context.Schedulers[KernelStatic.GetCurrentThread().CurrentCore];

            // Note that "RescheduleCurrentCore" will block, so "RescheduleOtherCores" must be done first.
            currentScheduler.RescheduleOtherCores(scheduledCoresMask);
            currentScheduler.RescheduleCurrentCore();
        }

        public static void EnableSchedulingFromForeignThread(KernelContext context, ulong scheduledCoresMask)
        {
            RescheduleOtherCores(context, scheduledCoresMask);
        }

        private void RescheduleCurrentCore()
        {
            if (_state.NeedsScheduling)
            {
                Schedule();
            }
        }

        private void RescheduleOtherCores(ulong scheduledCoresMask)
        {
            RescheduleOtherCores(_context, scheduledCoresMask & ~(1UL << _coreId));
        }

        private static void RescheduleOtherCores(KernelContext context, ulong scheduledCoresMask)
        {
            while (scheduledCoresMask != 0)
            {
                int coreToSignal = BitOperations.TrailingZeroCount(scheduledCoresMask);

                KThread threadToSignal = context.Schedulers[coreToSignal]._currentThread;

                // Request the thread running on that core to stop and reschedule, if we have one.
                threadToSignal?.Context.RequestInterrupt();

                // If the core is idle, ensure that the idle thread is awaken.
                context.Schedulers[coreToSignal].NotifyIdleThread();

                scheduledCoresMask &= ~(1UL << coreToSignal);
            }
        }

        private void ActivateIdleThread()
        {
            while (Interlocked.CompareExchange(ref _coreIdleLock, 1, 0) != 0)
            {
                Thread.SpinWait(1);
            }

            Thread.MemoryBarrier();

            // Signals that idle thread is now active on this core.
            _idleActive = true;

            TryLeaveIdle();

            Interlocked.Exchange(ref _coreIdleLock, 0);
        }

        private void NotifyIdleThread()
        {
            while (Interlocked.CompareExchange(ref _coreIdleLock, 1, 0) != 0)
            {
                Thread.SpinWait(1);
            }

            Thread.MemoryBarrier();

            // Signals that the idle core may be able to exit idle.
            _idleSignalled = true;

            TryLeaveIdle();

            Interlocked.Exchange(ref _coreIdleLock, 0);
        }

        public void TryLeaveIdle()
        {
            if (_idleSignalled && _idleActive)
            {
                _state.NeedsScheduling = false;
                Thread.MemoryBarrier();
                KThread nextThread = PickNextThread(null, _state.SelectedThread);

                if (nextThread != null)
                {
                    _idleActive = false;
                    nextThread.SchedulerWaitEvent.Set();
                }

                _idleSignalled = false;
            }
        }

        public void Schedule()
        {
            _state.NeedsScheduling = false;
            Thread.MemoryBarrier();
            KThread currentThread = KernelStatic.GetCurrentThread();
            KThread selectedThread = _state.SelectedThread;

            // If the thread is already scheduled and running on the core, we have nothing to do.
            if (currentThread == selectedThread)
            {
                return;
            }

            currentThread.SchedulerWaitEvent.Reset();
            currentThread.ThreadContext.Unlock();

            // Wake all the threads that might be waiting until this thread context is unlocked.
            for (int core = 0; core < CpuCoresCount; core++)
            {
                _context.Schedulers[core].NotifyIdleThread();
            }

            KThread nextThread = PickNextThread(KernelStatic.GetCurrentThread(), selectedThread);

            if (currentThread.Context.Running)
            {
                // Wait until this thread is scheduled again, and allow the next thread to run.

                if (nextThread == null)
                {
                    ActivateIdleThread();
                    currentThread.SchedulerWaitEvent.WaitOne();
                }
                else
                {
                    WaitHandle.SignalAndWait(nextThread.SchedulerWaitEvent, currentThread.SchedulerWaitEvent);
                }
            }
            else
            {
                // Allow the next thread to run.

                if (nextThread == null)
                {
                    ActivateIdleThread();
                }
                else
                {
                    nextThread.SchedulerWaitEvent.Set();
                }

                // We don't need to wait since the thread is exiting, however we need to
                // make sure this thread will never call the scheduler again, since it is
                // no longer assigned to a core.
                currentThread.MakeUnschedulable();

                // Just to be sure, set the core to a invalid value.
                // This will trigger a exception if it attempts to call schedule again,
                // rather than leaving the scheduler in a invalid state.
                currentThread.CurrentCore = -1;
            }
        }

        private KThread PickNextThread(KThread currentThread, KThread selectedThread)
        {
            while (true)
            {
                if (selectedThread != null)
                {
                    // Try to run the selected thread.
                    // We need to acquire the context lock to be sure the thread is not
                    // already running on another core. If it is, then we return here
                    // and the caller should try again once there is something available for scheduling.
                    // The thread currently running on the core should have been requested to
                    // interrupt so this is not expected to take long.
                    // The idle thread must also be paused if we are scheduling a thread
                    // on the core, as the scheduled thread will handle the next switch.
                    if (selectedThread.ThreadContext.Lock())
                    {
                        SwitchTo(currentThread, selectedThread);

                        if (!_state.NeedsScheduling)
                        {
                            return selectedThread;
                        }

                        selectedThread.ThreadContext.Unlock();
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    // The core is idle now, make sure that the idle thread can run
                    // and switch the core when a thread is available.
                    SwitchTo(currentThread, null);
                    return null;
                }

                _state.NeedsScheduling = false;
                Thread.MemoryBarrier();
                selectedThread = _state.SelectedThread;
            }
        }

        private void SwitchTo(KThread currentThread, KThread nextThread)
        {
            KProcess currentProcess = currentThread?.Owner;

            if (currentThread != nextThread)
            {
                long previousTicks = LastContextSwitchTime;
                long currentTicks = PerformanceCounter.ElapsedTicks;
                long ticksDelta = currentTicks - previousTicks;

                if (currentThread == null)
                {
                    Interlocked.Add(ref _idleTimeRunning, ticksDelta);
                }
                else
                {
                    currentThread.AddCpuTime(ticksDelta);
                }

                currentProcess?.AddCpuTime(ticksDelta);

                LastContextSwitchTime = currentTicks;

                if (currentProcess != null)
                {
                    _previousThread = !currentThread.TerminationRequested && currentThread.ActiveCore == _coreId ? currentThread : null;
                }
                else if (currentThread == null)
                {
                    _previousThread = null;
                }
            }

            if (nextThread != null && nextThread.CurrentCore != _coreId)
            {
                nextThread.CurrentCore = _coreId;
            }

            _currentThread = nextThread;
        }

        public static void PreemptionThreadLoop(KernelContext context)
        {
            while (context.Running)
            {
                context.CriticalSection.Enter();

                for (int core = 0; core < CpuCoresCount; core++)
                {
                    RotateScheduledQueue(context, core, PreemptionPriorities(core));
                }

                context.CriticalSection.Leave();

                Thread.Sleep(RoundRobinTimeQuantumMs);
            }
        }

        private static void RotateScheduledQueue(KernelContext context, int core, int prio)
        {
            KThread selectedThread = context.PriorityQueue.ScheduledThreadsWithDynamicPriorityFirstOrDefault(core, prio);
            KThread nextThread = null;

            // Yield priority queue.
            if (selectedThread != null)
            {
                nextThread = context.PriorityQueue.Reschedule(prio, core, selectedThread);
            }

            static KThread FirstSuitableCandidateOrDefault(KernelContext context, int core, KThread selectedThread, KThread nextThread, Predicate<KThread> predicate)
            {
                foreach (KThread suggested in context.PriorityQueue.SuggestedThreads(core))
                {
                    int suggestedCore = suggested.ActiveCore;
                    if (suggestedCore >= 0)
                    {
                        KThread selectedSuggestedCore = context.PriorityQueue.ScheduledThreadsFirstOrDefault(suggestedCore);

                        if (selectedSuggestedCore == suggested || (selectedSuggestedCore != null && selectedSuggestedCore.DynamicPriority < 2))
                        {
                            continue;
                        }
                    }

                    // If the candidate was scheduled after the current thread, then it's not worth it.
                    if (nextThread == selectedThread ||
                        nextThread == null ||
                        nextThread.LastScheduledTime >= suggested.LastScheduledTime)
                    {
                        if (predicate(suggested))
                        {
                            return suggested;
                        }
                    }
                }

                return null;
            }

            // Select candidate threads that could run on this core.
            // Only take into account threads that are not yet selected.
            KThread dst = FirstSuitableCandidateOrDefault(context, core, selectedThread, nextThread, x => x.DynamicPriority == prio);

            if (dst != null)
            {
                context.PriorityQueue.TransferToCore(prio, core, dst);
            }

            // If the priority of the currently selected thread is lower or same as the preemption priority,
            // then try to migrate a thread with lower priority.
            KThread bestCandidate = context.PriorityQueue.ScheduledThreadsFirstOrDefault(core);

            if (bestCandidate != null && bestCandidate.DynamicPriority >= prio)
            {
                dst = FirstSuitableCandidateOrDefault(context, core, selectedThread, nextThread, x => x.DynamicPriority < bestCandidate.DynamicPriority);

                if (dst != null)
                {
                    context.PriorityQueue.TransferToCore(dst.DynamicPriority, core, dst);
                }
            }

            context.ThreadReselectionRequested = true;
        }

        public static void Yield(KernelContext context)
        {
            KThread currentThread = KernelStatic.GetCurrentThread();

            if (!currentThread.IsSchedulable)
            {
                return;
            }

            context.CriticalSection.Enter();

            if (currentThread.SchedFlags != ThreadSchedState.Running)
            {
                context.CriticalSection.Leave();
                return;
            }

            KThread nextThread = context.PriorityQueue.Reschedule(currentThread.DynamicPriority, currentThread.ActiveCore, currentThread);

            if (nextThread != currentThread)
            {
                context.ThreadReselectionRequested = true;
            }

            context.CriticalSection.Leave();
        }

        public static void YieldWithLoadBalancing(KernelContext context)
        {
            KThread currentThread = KernelStatic.GetCurrentThread();

            if (!currentThread.IsSchedulable)
            {
                return;
            }

            context.CriticalSection.Enter();

            if (currentThread.SchedFlags != ThreadSchedState.Running)
            {
                context.CriticalSection.Leave();
                return;
            }

            int prio = currentThread.DynamicPriority;
            int core = currentThread.ActiveCore;

            // Move current thread to the end of the queue.
            KThread nextThread = context.PriorityQueue.Reschedule(prio, core, currentThread);

            static KThread FirstSuitableCandidateOrDefault(KernelContext context, int core, KThread nextThread, int lessThanOrEqualPriority)
            {
                foreach (KThread suggested in context.PriorityQueue.SuggestedThreads(core))
                {
                    int suggestedCore = suggested.ActiveCore;
                    if (suggestedCore >= 0)
                    {
                        KThread selectedSuggestedCore = context.Schedulers[suggestedCore]._state.SelectedThread;

                        if (selectedSuggestedCore == suggested || (selectedSuggestedCore != null && selectedSuggestedCore.DynamicPriority < 2))
                        {
                            continue;
                        }
                    }

                    // If the candidate was scheduled after the current thread, then it's not worth it,
                    // unless the priority is higher than the current one.
                    if (suggested.LastScheduledTime <= nextThread.LastScheduledTime ||
                        suggested.DynamicPriority < nextThread.DynamicPriority)
                    {
                        if (suggested.DynamicPriority <= lessThanOrEqualPriority)
                        {
                            return suggested;
                        }
                    }
                }

                return null;
            }

            KThread dst = FirstSuitableCandidateOrDefault(context, core, nextThread, prio);

            if (dst != null)
            {
                context.PriorityQueue.TransferToCore(dst.DynamicPriority, core, dst);

                context.ThreadReselectionRequested = true;
            }
            else if (currentThread != nextThread)
            {
                context.ThreadReselectionRequested = true;
            }

            context.CriticalSection.Leave();
        }

        public static void YieldToAnyThread(KernelContext context)
        {
            KThread currentThread = KernelStatic.GetCurrentThread();

            if (!currentThread.IsSchedulable)
            {
                return;
            }

            context.CriticalSection.Enter();

            if (currentThread.SchedFlags != ThreadSchedState.Running)
            {
                context.CriticalSection.Leave();
                return;
            }

            int core = currentThread.ActiveCore;

            context.PriorityQueue.TransferToCore(currentThread.DynamicPriority, -1, currentThread);

            if (!context.PriorityQueue.HasScheduledThreads(core))
            {
                KThread selectedThread = null;

                foreach (KThread suggested in context.PriorityQueue.SuggestedThreads(core))
                {
                    int suggestedCore = suggested.ActiveCore;

                    if (suggestedCore < 0)
                    {
                        continue;
                    }

                    KThread firstCandidate = context.PriorityQueue.ScheduledThreadsFirstOrDefault(suggestedCore);

                    if (firstCandidate == suggested)
                    {
                        continue;
                    }

                    if (firstCandidate == null || firstCandidate.DynamicPriority >= 2)
                    {
                        context.PriorityQueue.TransferToCore(suggested.DynamicPriority, core, suggested);
                    }

                    selectedThread = suggested;
                    break;
                }

                if (currentThread != selectedThread)
                {
                    context.ThreadReselectionRequested = true;
                }
            }
            else
            {
                context.ThreadReselectionRequested = true;
            }

            context.CriticalSection.Leave();
        }

        public void Dispose()
        {
            // No resources to dispose for now.
        }
    }
}
