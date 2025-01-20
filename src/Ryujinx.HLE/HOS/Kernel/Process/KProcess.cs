using Ryujinx.Common;
using Ryujinx.Common.Logging;
using Ryujinx.Cpu;
using Ryujinx.HLE.Exceptions;
using Ryujinx.HLE.HOS.Kernel.Common;
using Ryujinx.HLE.HOS.Kernel.Memory;
using Ryujinx.HLE.HOS.Kernel.Threading;
using Ryujinx.Horizon.Common;
using Ryujinx.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Ryujinx.HLE.HOS.Kernel.Process
{
    class KProcess : KSynchronizationObject
    {
        public const uint KernelVersionMajor = 10;
        public const uint KernelVersionMinor = 4;
        public const uint KernelVersionRevision = 0;

        public const uint KernelVersionPacked =
            (KernelVersionMajor << 19) |
            (KernelVersionMinor << 15) |
            (KernelVersionRevision << 0);

        public KPageTableBase MemoryManager { get; private set; }

        private readonly SortedDictionary<ulong, KTlsPageInfo> _fullTlsPages;
        private readonly SortedDictionary<ulong, KTlsPageInfo> _freeTlsPages;

        public int DefaultCpuCore { get; set; }

        public bool Debug { get; private set; }

        public KResourceLimit ResourceLimit { get; private set; }

        public ulong PersonalMmHeapPagesCount { get; private set; }

        public ProcessState State { get; private set; }

        private readonly Lock _processLock = new();
        private readonly Lock _threadingLock = new();

        public KAddressArbiter AddressArbiter { get; private set; }

        public ulong[] RandomEntropy { get; private set; }
        public KThread[] PinnedThreads { get; private set; }

        private bool _signaled;

        public string Name { get; private set; }

        private int _threadCount;

        public ProcessCreationFlags Flags { get; private set; }

        private MemoryRegion _memRegion;

        public KProcessCapabilities Capabilities { get; private set; }

        public bool AllowCodeMemoryForJit { get; private set; }

        public ulong TitleId { get; private set; }
        public bool IsApplication { get; private set; }
        public ulong Pid { get; private set; }

        private ulong _entrypoint;
        private ThreadStart _customThreadStart;
        private ulong _imageSize;
        private ulong _mainThreadStackSize;
        private ulong _memoryUsageCapacity;

        public KHandleTable HandleTable { get; private set; }

        public ulong UserExceptionContextAddress { get; private set; }

        private readonly LinkedList<KThread> _threads;

        public bool IsPaused { get; private set; }

        private long _totalTimeRunning;

        public long TotalTimeRunning => _totalTimeRunning;

        private IProcessContextFactory _contextFactory;
        public IProcessContext Context { get; private set; }
        public IVirtualMemoryManager CpuMemory => Context.AddressSpace;

        public HleProcessDebugger Debugger { get; private set; }

        public KProcess(KernelContext context, bool allowCodeMemoryForJit = false) : base(context)
        {
            AddressArbiter = new KAddressArbiter(context);

            _fullTlsPages = new SortedDictionary<ulong, KTlsPageInfo>();
            _freeTlsPages = new SortedDictionary<ulong, KTlsPageInfo>();

            Capabilities = new KProcessCapabilities();

            AllowCodeMemoryForJit = allowCodeMemoryForJit;

            RandomEntropy = new ulong[KScheduler.CpuCoresCount];
            PinnedThreads = new KThread[KScheduler.CpuCoresCount];

            // TODO: Remove once we no longer need to initialize it externally.
            HandleTable = new KHandleTable();

            _threads = new LinkedList<KThread>();

            Debugger = new HleProcessDebugger(this);
        }

        public Result InitializeKip(
            ProcessCreationInfo creationInfo,
            ReadOnlySpan<uint> capabilities,
            KPageList pageList,
            KResourceLimit resourceLimit,
            MemoryRegion memRegion,
            IProcessContextFactory contextFactory,
            ThreadStart customThreadStart = null)
        {
            ResourceLimit = resourceLimit;
            _memRegion = memRegion;
            _contextFactory = contextFactory ?? new ProcessContextFactory();
            _customThreadStart = customThreadStart;

            Pid = KernelContext.NewKipId();

            if (Pid == 0 || Pid >= KernelConstants.InitialProcessId)
            {
                throw new InvalidOperationException($"Invalid KIP Id {Pid}.");
            }

            InitializeMemoryManager(creationInfo.Flags);

            ulong codeAddress = creationInfo.CodeAddress;

            ulong codeSize = (ulong)creationInfo.CodePagesCount * KPageTableBase.PageSize;

            KMemoryBlockSlabManager slabManager = creationInfo.Flags.HasFlag(ProcessCreationFlags.IsApplication)
                ? KernelContext.LargeMemoryBlockSlabManager
                : KernelContext.SmallMemoryBlockSlabManager;

            Result result = MemoryManager.InitializeForProcess(
                creationInfo.Flags,
                !creationInfo.Flags.HasFlag(ProcessCreationFlags.EnableAslr),
                memRegion,
                codeAddress,
                codeSize,
                slabManager);

            if (result != Result.Success)
            {
                return result;
            }

            if (!MemoryManager.CanContain(codeAddress, codeSize, MemoryState.CodeStatic))
            {
                return KernelResult.InvalidMemRange;
            }

            result = MemoryManager.MapPages(codeAddress, pageList, MemoryState.CodeStatic, KMemoryPermission.None);

            if (result != Result.Success)
            {
                return result;
            }

            result = Capabilities.InitializeForKernel(capabilities, MemoryManager);

            if (result != Result.Success)
            {
                return result;
            }

            return ParseProcessInfo(creationInfo);
        }

        public Result Initialize(
            ProcessCreationInfo creationInfo,
            ReadOnlySpan<uint> capabilities,
            KResourceLimit resourceLimit,
            MemoryRegion memRegion,
            IProcessContextFactory contextFactory,
            ThreadStart customThreadStart = null)
        {
            ResourceLimit = resourceLimit;
            _memRegion = memRegion;
            _contextFactory = contextFactory ?? new ProcessContextFactory();
            _customThreadStart = customThreadStart;
            IsApplication = creationInfo.Flags.HasFlag(ProcessCreationFlags.IsApplication);

            ulong personalMmHeapSize = GetPersonalMmHeapSize((ulong)creationInfo.SystemResourcePagesCount, memRegion);

            ulong codePagesCount = (ulong)creationInfo.CodePagesCount;

            ulong neededSizeForProcess = personalMmHeapSize + codePagesCount * KPageTableBase.PageSize;

            if (neededSizeForProcess != 0 && resourceLimit != null)
            {
                if (!resourceLimit.Reserve(LimitableResource.Memory, neededSizeForProcess))
                {
                    return KernelResult.ResLimitExceeded;
                }
            }

            void CleanUpForError()
            {
                if (neededSizeForProcess != 0 && resourceLimit != null)
                {
                    resourceLimit.Release(LimitableResource.Memory, neededSizeForProcess);
                }
            }

            PersonalMmHeapPagesCount = (ulong)creationInfo.SystemResourcePagesCount;

            KMemoryBlockSlabManager slabManager;

            if (PersonalMmHeapPagesCount != 0)
            {
                slabManager = new KMemoryBlockSlabManager(PersonalMmHeapPagesCount * KPageTableBase.PageSize);
            }
            else
            {
                slabManager = creationInfo.Flags.HasFlag(ProcessCreationFlags.IsApplication)
                    ? KernelContext.LargeMemoryBlockSlabManager
                    : KernelContext.SmallMemoryBlockSlabManager;
            }

            Pid = KernelContext.NewProcessId();

            if (Pid == ulong.MaxValue || Pid < KernelConstants.InitialProcessId)
            {
                throw new InvalidOperationException($"Invalid Process Id {Pid}.");
            }

            InitializeMemoryManager(creationInfo.Flags);

            ulong codeAddress = creationInfo.CodeAddress;

            ulong codeSize = codePagesCount * KPageTableBase.PageSize;

            Result result = MemoryManager.InitializeForProcess(
                creationInfo.Flags,
                !creationInfo.Flags.HasFlag(ProcessCreationFlags.EnableAslr),
                memRegion,
                codeAddress,
                codeSize,
                slabManager);

            if (result != Result.Success)
            {
                CleanUpForError();

                return result;
            }

            if (!MemoryManager.CanContain(codeAddress, codeSize, MemoryState.CodeStatic))
            {
                CleanUpForError();

                return KernelResult.InvalidMemRange;
            }

            result = MemoryManager.MapPages(
                codeAddress,
                codePagesCount,
                MemoryState.CodeStatic,
                KMemoryPermission.None);

            if (result != Result.Success)
            {
                CleanUpForError();

                return result;
            }

            result = Capabilities.InitializeForUser(capabilities, MemoryManager, IsApplication);

            if (result != Result.Success)
            {
                CleanUpForError();

                return result;
            }

            result = ParseProcessInfo(creationInfo);

            if (result != Result.Success)
            {
                CleanUpForError();
            }

            return result;
        }

        private Result ParseProcessInfo(ProcessCreationInfo creationInfo)
        {
            // Ensure that the current kernel version is equal or above to the minimum required.
            uint requiredKernelVersionMajor = Capabilities.KernelReleaseVersion >> 19;
            uint requiredKernelVersionMinor = (Capabilities.KernelReleaseVersion >> 15) & 0xf;

            if (KernelContext.EnableVersionChecks)
            {
                if (requiredKernelVersionMajor > KernelVersionMajor)
                {
                    return KernelResult.InvalidCombination;
                }

                if (requiredKernelVersionMajor != KernelVersionMajor && requiredKernelVersionMajor < 3)
                {
                    return KernelResult.InvalidCombination;
                }

                if (requiredKernelVersionMinor > KernelVersionMinor)
                {
                    return KernelResult.InvalidCombination;
                }
            }

            Result result = AllocateThreadLocalStorage(out ulong userExceptionContextAddress);

            if (result != Result.Success)
            {
                return result;
            }

            UserExceptionContextAddress = userExceptionContextAddress;

            MemoryHelper.FillWithZeros(CpuMemory, userExceptionContextAddress, KTlsPageInfo.TlsEntrySize);

            Name = creationInfo.Name;

            State = ProcessState.Created;

            Flags = creationInfo.Flags;
            TitleId = creationInfo.TitleId;
            _entrypoint = creationInfo.CodeAddress;
            _imageSize = (ulong)creationInfo.CodePagesCount * KPageTableBase.PageSize;

            switch (Flags & ProcessCreationFlags.AddressSpaceMask)
            {
                case ProcessCreationFlags.AddressSpace32Bit:
                case ProcessCreationFlags.AddressSpace64BitDeprecated:
                case ProcessCreationFlags.AddressSpace64Bit:
                    _memoryUsageCapacity = MemoryManager.HeapRegionEnd -
                                           MemoryManager.HeapRegionStart;
                    break;

                case ProcessCreationFlags.AddressSpace32BitWithoutAlias:
                    _memoryUsageCapacity = MemoryManager.HeapRegionEnd -
                                           MemoryManager.HeapRegionStart +
                                           MemoryManager.AliasRegionEnd -
                                           MemoryManager.AliasRegionStart;
                    break;
                default:
                    throw new InvalidOperationException($"Invalid MMU flags value 0x{Flags:x2}.");
            }

            GenerateRandomEntropy();

            return Result.Success;
        }

        public Result AllocateThreadLocalStorage(out ulong address)
        {
            KernelContext.CriticalSection.Enter();

            Result result;

            if (_freeTlsPages.Count > 0)
            {
                // If we have free TLS pages available, just use the first one.
                KTlsPageInfo pageInfo = _freeTlsPages.Values.First();

                if (!pageInfo.TryGetFreePage(out address))
                {
                    throw new InvalidOperationException("Unexpected failure getting free TLS page!");
                }

                if (pageInfo.IsFull())
                {
                    _freeTlsPages.Remove(pageInfo.PageVirtualAddress);

                    _fullTlsPages.Add(pageInfo.PageVirtualAddress, pageInfo);
                }

                result = Result.Success;
            }
            else
            {
                // Otherwise, we need to create a new one.
                result = AllocateTlsPage(out KTlsPageInfo pageInfo);

                if (result == Result.Success)
                {
                    if (!pageInfo.TryGetFreePage(out address))
                    {
                        throw new InvalidOperationException("Unexpected failure getting free TLS page!");
                    }

                    _freeTlsPages.Add(pageInfo.PageVirtualAddress, pageInfo);
                }
                else
                {
                    address = 0;
                }
            }

            KernelContext.CriticalSection.Leave();

            return result;
        }

        private Result AllocateTlsPage(out KTlsPageInfo pageInfo)
        {
            pageInfo = default;

            if (!KernelContext.UserSlabHeapPages.TryGetItem(out ulong tlsPagePa))
            {
                return KernelResult.OutOfMemory;
            }

            ulong regionStart = MemoryManager.TlsIoRegionStart;
            ulong regionSize = MemoryManager.TlsIoRegionEnd - regionStart;

            ulong regionPagesCount = regionSize / KPageTableBase.PageSize;

            Result result = MemoryManager.MapPages(
                1,
                KPageTableBase.PageSize,
                tlsPagePa,
                true,
                regionStart,
                regionPagesCount,
                MemoryState.ThreadLocal,
                KMemoryPermission.ReadAndWrite,
                out ulong tlsPageVa);

            if (result != Result.Success)
            {
                KernelContext.UserSlabHeapPages.Free(tlsPagePa);
            }
            else
            {
                pageInfo = new KTlsPageInfo(tlsPageVa, tlsPagePa);

                MemoryHelper.FillWithZeros(CpuMemory, tlsPageVa, KPageTableBase.PageSize);
            }

            return result;
        }

        public Result FreeThreadLocalStorage(ulong tlsSlotAddr)
        {
            ulong tlsPageAddr = BitUtils.AlignDown<ulong>(tlsSlotAddr, KPageTableBase.PageSize);

            KernelContext.CriticalSection.Enter();

            Result result = Result.Success;


            if (_fullTlsPages.TryGetValue(tlsPageAddr, out KTlsPageInfo pageInfo))
            {
                // TLS page was full, free slot and move to free pages tree.
                _fullTlsPages.Remove(tlsPageAddr);

                _freeTlsPages.Add(tlsPageAddr, pageInfo);
            }
            else if (!_freeTlsPages.TryGetValue(tlsPageAddr, out pageInfo))
            {
                result = KernelResult.InvalidAddress;
            }

            if (pageInfo != null)
            {
                pageInfo.FreeTlsSlot(tlsSlotAddr);

                if (pageInfo.IsEmpty())
                {
                    // TLS page is now empty, we should ensure it is removed
                    // from all trees, and free the memory it was using.
                    _freeTlsPages.Remove(tlsPageAddr);

                    KernelContext.CriticalSection.Leave();

                    FreeTlsPage(pageInfo);

                    return Result.Success;
                }
            }

            KernelContext.CriticalSection.Leave();

            return result;
        }

        private Result FreeTlsPage(KTlsPageInfo pageInfo)
        {
            Result result = MemoryManager.UnmapForKernel(pageInfo.PageVirtualAddress, 1, MemoryState.ThreadLocal);

            if (result == Result.Success)
            {
                KernelContext.UserSlabHeapPages.Free(pageInfo.PagePhysicalAddress);
            }

            return result;
        }

        private void GenerateRandomEntropy()
        {
            // TODO.
        }

        public Result Start(int mainThreadPriority, ulong stackSize)
        {
            lock (_processLock)
            {
                if (State > ProcessState.CreatedAttached)
                {
                    return KernelResult.InvalidState;
                }

                if (ResourceLimit != null && !ResourceLimit.Reserve(LimitableResource.Thread, 1))
                {
                    return KernelResult.ResLimitExceeded;
                }

                KResourceLimit threadResourceLimit = ResourceLimit;
                KResourceLimit memoryResourceLimit = null;

                if (_mainThreadStackSize != 0)
                {
                    throw new InvalidOperationException("Trying to start a process with an invalid state!");
                }

                ulong stackSizeRounded = BitUtils.AlignUp<ulong>(stackSize, KPageTableBase.PageSize);

                ulong neededSize = stackSizeRounded + _imageSize;

                // Check if the needed size for the code and the stack will fit on the
                // memory usage capacity of this Process. Also check for possible overflow
                // on the above addition.
                if (neededSize > _memoryUsageCapacity || neededSize < stackSizeRounded)
                {
                    threadResourceLimit?.Release(LimitableResource.Thread, 1);

                    return KernelResult.OutOfMemory;
                }

                if (stackSizeRounded != 0 && ResourceLimit != null)
                {
                    memoryResourceLimit = ResourceLimit;

                    if (!memoryResourceLimit.Reserve(LimitableResource.Memory, stackSizeRounded))
                    {
                        threadResourceLimit?.Release(LimitableResource.Thread, 1);

                        return KernelResult.ResLimitExceeded;
                    }
                }

                Result result;

                KThread mainThread = null;

                ulong stackTop = 0;

                void CleanUpForError()
                {
                    HandleTable.Destroy();

                    mainThread?.DecrementReferenceCount();

                    if (_mainThreadStackSize != 0)
                    {
                        ulong stackBottom = stackTop - _mainThreadStackSize;

                        ulong stackPagesCount = _mainThreadStackSize / KPageTableBase.PageSize;

                        MemoryManager.UnmapForKernel(stackBottom, stackPagesCount, MemoryState.Stack);

                        _mainThreadStackSize = 0;
                    }

                    memoryResourceLimit?.Release(LimitableResource.Memory, stackSizeRounded);
                    threadResourceLimit?.Release(LimitableResource.Thread, 1);
                }

                if (stackSizeRounded != 0)
                {
                    ulong stackPagesCount = stackSizeRounded / KPageTableBase.PageSize;

                    ulong regionStart = MemoryManager.StackRegionStart;
                    ulong regionSize = MemoryManager.StackRegionEnd - regionStart;

                    ulong regionPagesCount = regionSize / KPageTableBase.PageSize;

                    result = MemoryManager.MapPages(
                        stackPagesCount,
                        KPageTableBase.PageSize,
                        0,
                        false,
                        regionStart,
                        regionPagesCount,
                        MemoryState.Stack,
                        KMemoryPermission.ReadAndWrite,
                        out ulong stackBottom);

                    if (result != Result.Success)
                    {
                        CleanUpForError();

                        return result;
                    }

                    _mainThreadStackSize += stackSizeRounded;

                    stackTop = stackBottom + stackSizeRounded;
                }

                ulong heapCapacity = _memoryUsageCapacity - _mainThreadStackSize - _imageSize;

                result = MemoryManager.SetHeapCapacity(heapCapacity);

                if (result != Result.Success)
                {
                    CleanUpForError();

                    return result;
                }

                HandleTable = new KHandleTable();

                result = HandleTable.Initialize(Capabilities.HandleTableSize);

                if (result != Result.Success)
                {
                    CleanUpForError();

                    return result;
                }

                mainThread = new KThread(KernelContext);

                result = mainThread.Initialize(
                    _entrypoint,
                    0,
                    stackTop,
                    mainThreadPriority,
                    DefaultCpuCore,
                    this,
                    ThreadType.User,
                    _customThreadStart);

                if (result != Result.Success)
                {
                    CleanUpForError();

                    return result;
                }

                result = HandleTable.GenerateHandle(mainThread, out int mainThreadHandle);

                if (result != Result.Success)
                {
                    CleanUpForError();

                    return result;
                }

                mainThread.SetEntryArguments(0, mainThreadHandle);

                ProcessState oldState = State;
                ProcessState newState = State != ProcessState.Created
                    ? ProcessState.Attached
                    : ProcessState.Started;

                SetState(newState);

                result = mainThread.Start();

                if (result != Result.Success)
                {
                    SetState(oldState);

                    CleanUpForError();
                }

                if (result == Result.Success)
                {
                    mainThread.IncrementReferenceCount();
                }

                mainThread.DecrementReferenceCount();

                return result;
            }
        }

        private void SetState(ProcessState newState)
        {
            if (State != newState)
            {
                State = newState;
                _signaled = true;

                Signal();
            }
        }

        public Result InitializeThread(
            KThread thread,
            ulong entrypoint,
            ulong argsPtr,
            ulong stackTop,
            int priority,
            int cpuCore,
            ThreadStart customThreadStart = null)
        {
            lock (_processLock)
            {
                return thread.Initialize(entrypoint, argsPtr, stackTop, priority, cpuCore, this, ThreadType.User, customThreadStart);
            }
        }

        public IExecutionContext CreateExecutionContext()
        {
            return Context?.CreateExecutionContext(new ExceptionCallbacks(
                InterruptHandler,
                null,
                KernelContext.SyscallHandler.SvcCall,
                UndefinedInstructionHandler));
        }

        private void InterruptHandler(IExecutionContext context)
        {
            KThread currentThread = KernelStatic.GetCurrentThread();

            if (currentThread.Context.Running &&
                currentThread.Owner != null &&
                currentThread.GetUserDisableCount() != 0 &&
                currentThread.Owner.PinnedThreads[currentThread.CurrentCore] == null)
            {
                KernelContext.CriticalSection.Enter();

                currentThread.Owner.PinThread(currentThread);

                currentThread.SetUserInterruptFlag();

                KernelContext.CriticalSection.Leave();
            }

            if (currentThread.IsSchedulable)
            {
                KernelContext.Schedulers[currentThread.CurrentCore].Schedule();
            }

            currentThread.HandlePostSyscall();
        }

        public void IncrementThreadCount()
        {
            Interlocked.Increment(ref _threadCount);
        }

        public void DecrementThreadCountAndTerminateIfZero()
        {
            if (Interlocked.Decrement(ref _threadCount) == 0)
            {
                Terminate();
            }
        }

        public void DecrementToZeroWhileTerminatingCurrent()
        {
            while (Interlocked.Decrement(ref _threadCount) != 0)
            {
                Destroy();
                TerminateCurrentProcess();
            }

            // Nintendo panic here because if it reaches this point, the current thread should be already dead.
            // As we handle the death of the thread in the post SVC handler and inside the CPU emulator, we don't panic here.
        }

        public ulong GetMemoryCapacity()
        {
            ulong totalCapacity = (ulong)ResourceLimit.GetRemainingValue(LimitableResource.Memory);

            totalCapacity += MemoryManager.GetTotalHeapSize();

            totalCapacity += GetPersonalMmHeapSize();

            totalCapacity += _imageSize + _mainThreadStackSize;

            if (totalCapacity <= _memoryUsageCapacity)
            {
                return totalCapacity;
            }

            return _memoryUsageCapacity;
        }

        public ulong GetMemoryUsage()
        {
            return _imageSize + _mainThreadStackSize + MemoryManager.GetTotalHeapSize() + GetPersonalMmHeapSize();
        }

        public ulong GetMemoryCapacityWithoutPersonalMmHeap()
        {
            return GetMemoryCapacity() - GetPersonalMmHeapSize();
        }

        public ulong GetMemoryUsageWithoutPersonalMmHeap()
        {
            return GetMemoryUsage() - GetPersonalMmHeapSize();
        }

        private ulong GetPersonalMmHeapSize()
        {
            return GetPersonalMmHeapSize(PersonalMmHeapPagesCount, _memRegion);
        }

        private static ulong GetPersonalMmHeapSize(ulong personalMmHeapPagesCount, MemoryRegion memRegion)
        {
            if (memRegion == MemoryRegion.Applet)
            {
                return 0;
            }

            return personalMmHeapPagesCount * KPageTableBase.PageSize;
        }

        public void AddCpuTime(long ticks)
        {
            Interlocked.Add(ref _totalTimeRunning, ticks);
        }

        public void AddThread(KThread thread)
        {
            lock (_threadingLock)
            {
                thread.ProcessListNode = _threads.AddLast(thread);
            }
        }

        public void RemoveThread(KThread thread)
        {
            lock (_threadingLock)
            {
                _threads.Remove(thread.ProcessListNode);
            }
        }

        public bool IsCpuCoreAllowed(int core)
        {
            return (Capabilities.AllowedCpuCoresMask & (1UL << core)) != 0;
        }

        public bool IsPriorityAllowed(int priority)
        {
            return (Capabilities.AllowedThreadPriosMask & (1UL << priority)) != 0;
        }

        public override bool IsSignaled()
        {
            return _signaled;
        }

        public Result Terminate()
        {
            Result result;

            bool shallTerminate = false;

            KernelContext.CriticalSection.Enter();

            lock (_processLock)
            {
                if (State >= ProcessState.Started)
                {
                    if (State == ProcessState.Started ||
                        State == ProcessState.Crashed ||
                        State == ProcessState.Attached ||
                        State == ProcessState.DebugSuspended)
                    {
                        SetState(ProcessState.Exiting);

                        shallTerminate = true;
                    }

                    result = Result.Success;
                }
                else
                {
                    result = KernelResult.InvalidState;
                }
            }

            KernelContext.CriticalSection.Leave();

            if (shallTerminate)
            {
                UnpauseAndTerminateAllThreadsExcept(KernelStatic.GetCurrentThread());

                HandleTable.Destroy();

                SignalExitToDebugTerminated();
                SignalExit();
            }

            return result;
        }

        public void TerminateCurrentProcess()
        {
            bool shallTerminate = false;

            KernelContext.CriticalSection.Enter();

            lock (_processLock)
            {
                if (State >= ProcessState.Started)
                {
                    if (State == ProcessState.Started ||
                        State == ProcessState.Attached ||
                        State == ProcessState.DebugSuspended)
                    {
                        SetState(ProcessState.Exiting);

                        shallTerminate = true;
                    }
                }
            }

            KernelContext.CriticalSection.Leave();

            if (shallTerminate)
            {
                UnpauseAndTerminateAllThreadsExcept(KernelStatic.GetCurrentThread());

                HandleTable.Destroy();

                // NOTE: this is supposed to be called in receiving of the mailbox.
                SignalExitToDebugExited();
                SignalExit();
            }

            KernelStatic.GetCurrentThread().Exit();
        }

        private void UnpauseAndTerminateAllThreadsExcept(KThread currentThread)
        {
            lock (_threadingLock)
            {
                KernelContext.CriticalSection.Enter();

                if (currentThread != null && PinnedThreads[currentThread.CurrentCore] == currentThread)
                {
                    UnpinThread(currentThread);
                }

                foreach (KThread thread in _threads)
                {
                    if (thread != currentThread && (thread.SchedFlags & ThreadSchedState.LowMask) != ThreadSchedState.TerminationPending)
                    {
                        thread.PrepareForTermination();
                    }
                }

                KernelContext.CriticalSection.Leave();
            }

            while (true)
            {
                KThread blockedThread = null;

                lock (_threadingLock)
                {
                    foreach (KThread thread in _threads)
                    {
                        if (thread != currentThread && (thread.SchedFlags & ThreadSchedState.LowMask) != ThreadSchedState.TerminationPending)
                        {
                            thread.IncrementReferenceCount();

                            blockedThread = thread;
                            break;
                        }
                    }
                }

                if (blockedThread == null)
                {
                    break;
                }

                blockedThread.Terminate();
                blockedThread.DecrementReferenceCount();
            }
        }

        private static void SignalExitToDebugTerminated()
        {
            // TODO: Debug events.
        }

        private static void SignalExitToDebugExited()
        {
            // TODO: Debug events.
        }

        private void SignalExit()
        {
            ResourceLimit?.Release(LimitableResource.Memory, GetMemoryUsage());

            KernelContext.CriticalSection.Enter();

            SetState(ProcessState.Exited);

            KernelContext.CriticalSection.Leave();
        }

        public Result ClearIfNotExited()
        {
            Result result;

            KernelContext.CriticalSection.Enter();

            lock (_processLock)
            {
                if (State != ProcessState.Exited && _signaled)
                {
                    _signaled = false;

                    result = Result.Success;
                }
                else
                {
                    result = KernelResult.InvalidState;
                }
            }

            KernelContext.CriticalSection.Leave();

            return result;
        }

        private void InitializeMemoryManager(ProcessCreationFlags flags)
        {
            int addrSpaceBits = (flags & ProcessCreationFlags.AddressSpaceMask) switch
            {
                ProcessCreationFlags.AddressSpace32Bit => 32,
                ProcessCreationFlags.AddressSpace64BitDeprecated => 36,
                ProcessCreationFlags.AddressSpace32BitWithoutAlias => 32,
                ProcessCreationFlags.AddressSpace64Bit => 39,
                _ => 39,
            };

            bool for64Bit = flags.HasFlag(ProcessCreationFlags.Is64Bit);

            Context = _contextFactory.Create(KernelContext, Pid, 1UL << addrSpaceBits, InvalidAccessHandler, for64Bit);

            MemoryManager = new KPageTable(KernelContext, CpuMemory, Context.AddressSpaceSize);
        }

        private bool InvalidAccessHandler(ulong va)
        {
            KernelStatic.GetCurrentThread()?.PrintGuestStackTrace();
            KernelStatic.GetCurrentThread()?.PrintGuestRegisterPrintout();

            Logger.Error?.Print(LogClass.Cpu, $"Invalid memory access at virtual address 0x{va:X16}.");

            return false;
        }

        private void UndefinedInstructionHandler(IExecutionContext context, ulong address, int opCode)
        {
            KernelStatic.GetCurrentThread().PrintGuestStackTrace();
            KernelStatic.GetCurrentThread()?.PrintGuestRegisterPrintout();

            throw new UndefinedInstructionException(address, opCode);
        }

        protected override void Destroy() => Context.Dispose();

        public Result SetActivity(bool pause)
        {
            KernelContext.CriticalSection.Enter();

            if (State != ProcessState.Exiting && State != ProcessState.Exited)
            {
                if (pause)
                {
                    if (IsPaused)
                    {
                        KernelContext.CriticalSection.Leave();

                        return KernelResult.InvalidState;
                    }

                    lock (_threadingLock)
                    {
                        foreach (KThread thread in _threads)
                        {
                            thread.Suspend(ThreadSchedState.ProcessPauseFlag);
                        }
                    }

                    IsPaused = true;
                }
                else
                {
                    if (!IsPaused)
                    {
                        KernelContext.CriticalSection.Leave();

                        return KernelResult.InvalidState;
                    }

                    lock (_threadingLock)
                    {
                        foreach (KThread thread in _threads)
                        {
                            thread.Resume(ThreadSchedState.ProcessPauseFlag);
                        }
                    }

                    IsPaused = false;
                }

                KernelContext.CriticalSection.Leave();

                return Result.Success;
            }

            KernelContext.CriticalSection.Leave();

            return KernelResult.InvalidState;
        }

        public void PinThread(KThread thread)
        {
            if (!thread.TerminationRequested)
            {
                PinnedThreads[thread.CurrentCore] = thread;

                thread.Pin();

                KernelContext.ThreadReselectionRequested = true;
            }
        }

        public void UnpinThread(KThread thread)
        {
            if (!thread.TerminationRequested)
            {
                thread.Unpin();

                PinnedThreads[thread.CurrentCore] = null;

                KernelContext.ThreadReselectionRequested = true;
            }
        }

        public static bool IsExceptionUserThread(KThread thread)
        {
            // TODO
            return false;
        }

        public bool IsSvcPermitted(int svcId)
        {
            return Capabilities.IsSvcPermitted(svcId);
        }
    }
}
