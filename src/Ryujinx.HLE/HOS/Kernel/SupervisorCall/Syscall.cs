using Ryujinx.Common;
using Ryujinx.Common.Logging;
using Ryujinx.Cpu;
using Ryujinx.HLE.Exceptions;
using Ryujinx.HLE.HOS.Kernel.Common;
using Ryujinx.HLE.HOS.Kernel.Ipc;
using Ryujinx.HLE.HOS.Kernel.Memory;
using Ryujinx.HLE.HOS.Kernel.Process;
using Ryujinx.HLE.HOS.Kernel.Threading;
using Ryujinx.Horizon.Common;
using Ryujinx.Memory;
using System;
using System.Buffers;
using System.Threading;

namespace Ryujinx.HLE.HOS.Kernel.SupervisorCall
{
    [SvcImpl]
    class Syscall : ISyscallApi
    {
        private readonly KernelContext _context;

        public Syscall(KernelContext context)
        {
            _context = context;
        }

        // Process

        [Svc(0x24)]
        public Result GetProcessId(out ulong pid, int handle)
        {
            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            KProcess process = currentProcess.HandleTable.GetKProcess(handle);

            if (process == null)
            {
                KThread thread = currentProcess.HandleTable.GetKThread(handle);

                if (thread != null)
                {
                    process = thread.Owner;
                }

                // TODO: KDebugEvent.
            }

            pid = process?.Pid ?? 0;

            return process != null
                ? Result.Success
                : KernelResult.InvalidHandle;
        }

        public Result CreateProcess(
            out int handle,
            ProcessCreationInfo info,
            ReadOnlySpan<uint> capabilities,
            IProcessContextFactory contextFactory,
            ThreadStart customThreadStart = null)
        {
            handle = 0;

            if ((info.Flags & ~ProcessCreationFlags.All) != 0)
            {
                return KernelResult.InvalidEnumValue;
            }

            // TODO: Address space check.

            if ((info.Flags & ProcessCreationFlags.PoolPartitionMask) > ProcessCreationFlags.PoolPartitionSystemNonSecure)
            {
                return KernelResult.InvalidEnumValue;
            }

            if ((info.CodeAddress & 0x1fffff) != 0)
            {
                return KernelResult.InvalidAddress;
            }

            if (info.CodePagesCount < 0 || info.SystemResourcePagesCount < 0)
            {
                return KernelResult.InvalidSize;
            }

            if (info.Flags.HasFlag(ProcessCreationFlags.EnableAliasRegionExtraSize))
            {
                if ((info.Flags & ProcessCreationFlags.AddressSpaceMask) != ProcessCreationFlags.AddressSpace64Bit ||
                    info.SystemResourcePagesCount <= 0)
                {
                    return KernelResult.InvalidState;
                }

                // TODO: Check that we are in debug mode.
            }

            if (info.Flags.HasFlag(ProcessCreationFlags.OptimizeMemoryAllocation) &&
                !info.Flags.HasFlag(ProcessCreationFlags.IsApplication))
            {
                return KernelResult.InvalidThread;
            }

            KHandleTable handleTable = KernelStatic.GetCurrentProcess().HandleTable;

            KProcess process = new(_context);

            using var _ = new OnScopeExit(process.DecrementReferenceCount);

            KResourceLimit resourceLimit;

            if (info.ResourceLimitHandle != 0)
            {
                resourceLimit = handleTable.GetObject<KResourceLimit>(info.ResourceLimitHandle);

                if (resourceLimit == null)
                {
                    return KernelResult.InvalidHandle;
                }
            }
            else
            {
                resourceLimit = _context.ResourceLimit;
            }

            MemoryRegion memRegion = (info.Flags & ProcessCreationFlags.PoolPartitionMask) switch
            {
                ProcessCreationFlags.PoolPartitionApplication => MemoryRegion.Application,
                ProcessCreationFlags.PoolPartitionApplet => MemoryRegion.Applet,
                ProcessCreationFlags.PoolPartitionSystem => MemoryRegion.Service,
                ProcessCreationFlags.PoolPartitionSystemNonSecure => MemoryRegion.NvServices,
                _ => MemoryRegion.NvServices,
            };

            Result result = process.Initialize(
                info,
                capabilities,
                resourceLimit,
                memRegion,
                contextFactory,
                customThreadStart);

            if (result != Result.Success)
            {
                return result;
            }

            _context.Processes.TryAdd(process.Pid, process);

            return handleTable.GenerateHandle(process, out handle);
        }

        public Result StartProcess(int handle, int priority, int cpuCore, ulong mainThreadStackSize)
        {
            KProcess process = KernelStatic.GetCurrentProcess().HandleTable.GetObject<KProcess>(handle);

            if (process == null)
            {
                return KernelResult.InvalidHandle;
            }

            if ((uint)cpuCore >= KScheduler.CpuCoresCount || !process.IsCpuCoreAllowed(cpuCore))
            {
                return KernelResult.InvalidCpuCore;
            }

            if ((uint)priority >= KScheduler.PrioritiesCount || !process.IsPriorityAllowed(priority))
            {
                return KernelResult.InvalidPriority;
            }

            process.DefaultCpuCore = cpuCore;

            Result result = process.Start(priority, mainThreadStackSize);

            if (result != Result.Success)
            {
                return result;
            }

            process.IncrementReferenceCount();

            return Result.Success;
        }

        [Svc(0x5f)]
        public Result FlushProcessDataCache(int processHandle, ulong address, ulong size)
        {
            // FIXME: This needs to be implemented as ARMv7 doesn't have any way to do cache maintenance operations on EL0.
            // As we don't support (and don't actually need) to flush the cache, this is stubbed.
            return Result.Success;
        }

        // IPC

        [Svc(0x1f)]
        public Result ConnectToNamedPort(out int handle, [PointerSized] ulong namePtr)
        {
            handle = 0;

            if (!KernelTransfer.UserToKernelString(out string name, namePtr, 12))
            {
                return KernelResult.UserCopyFailed;
            }

            return ConnectToNamedPort(out handle, name);
        }

        public Result ConnectToNamedPort(out int handle, string name)
        {
            handle = 0;

            if (name.Length > 11)
            {
                return KernelResult.MaximumExceeded;
            }

            KAutoObject autoObj = KAutoObject.FindNamedObject(_context, name);

            if (autoObj is not KClientPort clientPort)
            {
                return KernelResult.NotFound;
            }

            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            Result result = currentProcess.HandleTable.ReserveHandle(out handle);

            if (result != Result.Success)
            {
                return result;
            }

            result = clientPort.Connect(out KClientSession clientSession);

            if (result != Result.Success)
            {
                currentProcess.HandleTable.CancelHandleReservation(handle);

                return result;
            }

            currentProcess.HandleTable.SetReservedHandleObj(handle, clientSession);

            clientSession.DecrementReferenceCount();

            return result;
        }

        [Svc(0x21)]
        public Result SendSyncRequest(int handle)
        {
            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            KClientSession session = currentProcess.HandleTable.GetObject<KClientSession>(handle);

            if (session == null)
            {
                return KernelResult.InvalidHandle;
            }

            return session.SendSyncRequest();
        }

        [Svc(0x22)]
        public Result SendSyncRequestWithUserBuffer(
            [PointerSized] ulong messagePtr,
            [PointerSized] ulong messageSize,
            int handle)
        {
            if (!PageAligned(messagePtr))
            {
                return KernelResult.InvalidAddress;
            }

            if (!PageAligned(messageSize) || messageSize == 0)
            {
                return KernelResult.InvalidSize;
            }

            if (messagePtr + messageSize <= messagePtr)
            {
                return KernelResult.InvalidMemState;
            }

            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            Result result = currentProcess.MemoryManager.BorrowIpcBuffer(messagePtr, messageSize);

            if (result != Result.Success)
            {
                return result;
            }

            KClientSession session = currentProcess.HandleTable.GetObject<KClientSession>(handle);

            if (session == null)
            {
                result = KernelResult.InvalidHandle;
            }
            else
            {
                result = session.SendSyncRequest(messagePtr, messageSize);
            }

            Result result2 = currentProcess.MemoryManager.UnborrowIpcBuffer(messagePtr, messageSize);

            if (result == Result.Success)
            {
                result = result2;
            }

            return result;
        }

        [Svc(0x23)]
        public Result SendAsyncRequestWithUserBuffer(
            out int doneEventHandle,
            [PointerSized] ulong messagePtr,
            [PointerSized] ulong messageSize,
            int handle)
        {
            doneEventHandle = 0;

            if (!PageAligned(messagePtr))
            {
                return KernelResult.InvalidAddress;
            }

            if (!PageAligned(messageSize) || messageSize == 0)
            {
                return KernelResult.InvalidSize;
            }

            if (messagePtr + messageSize <= messagePtr)
            {
                return KernelResult.InvalidMemState;
            }

            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            Result result = currentProcess.MemoryManager.BorrowIpcBuffer(messagePtr, messageSize);

            if (result != Result.Success)
            {
                return result;
            }

            KResourceLimit resourceLimit = currentProcess.ResourceLimit;

            if (resourceLimit != null && !resourceLimit.Reserve(LimitableResource.Event, 1))
            {
                currentProcess.MemoryManager.UnborrowIpcBuffer(messagePtr, messageSize);

                return KernelResult.ResLimitExceeded;
            }

            KClientSession session = currentProcess.HandleTable.GetObject<KClientSession>(handle);

            if (session == null)
            {
                result = KernelResult.InvalidHandle;
            }
            else
            {
                KEvent doneEvent = new(_context);

                result = currentProcess.HandleTable.GenerateHandle(doneEvent.ReadableEvent, out doneEventHandle);

                if (result == Result.Success)
                {
                    result = session.SendAsyncRequest(doneEvent.WritableEvent, messagePtr, messageSize);

                    if (result != Result.Success)
                    {
                        currentProcess.HandleTable.CloseHandle(doneEventHandle);
                    }
                }
            }

            if (result != Result.Success)
            {
                resourceLimit?.Release(LimitableResource.Event, 1);

                currentProcess.MemoryManager.UnborrowIpcBuffer(messagePtr, messageSize);
            }

            return result;
        }

        [Svc(0x40)]
        public Result CreateSession(
            out int serverSessionHandle,
            out int clientSessionHandle,
            bool isLight,
            [PointerSized] ulong namePtr)
        {
            return CreateSession(out serverSessionHandle, out clientSessionHandle, isLight, null);
        }

        public Result CreateSession(
            out int serverSessionHandle,
            out int clientSessionHandle,
            bool isLight,
            string name)
        {
            serverSessionHandle = 0;
            clientSessionHandle = 0;

            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            KResourceLimit resourceLimit = currentProcess.ResourceLimit;

            if (resourceLimit != null && !resourceLimit.Reserve(LimitableResource.Session, 1))
            {
                return KernelResult.ResLimitExceeded;
            }

            Result result;

            if (isLight)
            {
                KLightSession session = new(_context);

                result = currentProcess.HandleTable.GenerateHandle(session.ServerSession, out serverSessionHandle);

                if (result == Result.Success)
                {
                    result = currentProcess.HandleTable.GenerateHandle(session.ClientSession, out clientSessionHandle);

                    if (result != Result.Success)
                    {
                        currentProcess.HandleTable.CloseHandle(serverSessionHandle);

                        serverSessionHandle = 0;
                    }
                }

                session.ServerSession.DecrementReferenceCount();
                session.ClientSession.DecrementReferenceCount();
            }
            else
            {
                KSession session = new(_context);

                result = currentProcess.HandleTable.GenerateHandle(session.ServerSession, out serverSessionHandle);

                if (result == Result.Success)
                {
                    result = currentProcess.HandleTable.GenerateHandle(session.ClientSession, out clientSessionHandle);

                    if (result != Result.Success)
                    {
                        currentProcess.HandleTable.CloseHandle(serverSessionHandle);

                        serverSessionHandle = 0;
                    }
                }

                session.ServerSession.DecrementReferenceCount();
                session.ClientSession.DecrementReferenceCount();
            }

            return result;
        }

        [Svc(0x41)]
        public Result AcceptSession(out int sessionHandle, int portHandle)
        {
            sessionHandle = 0;

            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            KServerPort serverPort = currentProcess.HandleTable.GetObject<KServerPort>(portHandle);

            if (serverPort == null)
            {
                return KernelResult.InvalidHandle;
            }

            Result result = currentProcess.HandleTable.ReserveHandle(out int handle);

            if (result != Result.Success)
            {
                return result;
            }

            KAutoObject session;

            if (serverPort.IsLight)
            {
                session = serverPort.AcceptIncomingLightConnection();
            }
            else
            {
                session = serverPort.AcceptIncomingConnection();
            }

            if (session != null)
            {
                currentProcess.HandleTable.SetReservedHandleObj(handle, session);

                session.DecrementReferenceCount();

                sessionHandle = handle;

                result = Result.Success;
            }
            else
            {
                currentProcess.HandleTable.CancelHandleReservation(handle);

                result = KernelResult.NotFound;
            }

            return result;
        }

        [Svc(0x43)]
        public Result ReplyAndReceive(
            out int handleIndex,
            [PointerSized] ulong handlesPtr,
            int handlesCount,
            int replyTargetHandle,
            long timeout)
        {
            handleIndex = 0;

            if ((uint)handlesCount > 0x40)
            {
                return KernelResult.MaximumExceeded;
            }

            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            ulong copySize = (ulong)((long)handlesCount * 4);

            if (!currentProcess.MemoryManager.InsideAddrSpace(handlesPtr, copySize))
            {
                return KernelResult.UserCopyFailed;
            }

            if (handlesPtr + copySize < handlesPtr)
            {
                return KernelResult.UserCopyFailed;
            }

            int[] handles = new int[handlesCount];

            if (!KernelTransfer.UserToKernelArray<int>(handlesPtr, handles))
            {
                return KernelResult.UserCopyFailed;
            }

            if (timeout > 0)
            {
                timeout += KTimeManager.DefaultTimeIncrementNanoseconds;
            }

            return ReplyAndReceive(out handleIndex, handles, replyTargetHandle, timeout);
        }

        public Result ReplyAndReceive(out int handleIndex, ReadOnlySpan<int> handles, int replyTargetHandle, long timeout)
        {
            handleIndex = 0;

            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            KSynchronizationObject[] syncObjsArray = ArrayPool<KSynchronizationObject>.Shared.Rent(handles.Length);

            Span<KSynchronizationObject> syncObjs = syncObjsArray.AsSpan(0, handles.Length);

            for (int index = 0; index < handles.Length; index++)
            {
                KSynchronizationObject obj = currentProcess.HandleTable.GetObject<KSynchronizationObject>(handles[index]);

                if (obj == null)
                {
                    return KernelResult.InvalidHandle;
                }

                syncObjs[index] = obj;
            }

            Result result = Result.Success;

            if (replyTargetHandle != 0)
            {
                KServerSession replyTarget = currentProcess.HandleTable.GetObject<KServerSession>(replyTargetHandle);

                if (replyTarget == null)
                {
                    result = KernelResult.InvalidHandle;
                }
                else
                {
                    result = replyTarget.Reply();
                }
            }

            if (result == Result.Success)
            {
                if (timeout > 0)
                {
                    timeout += KTimeManager.DefaultTimeIncrementNanoseconds;
                }

                while ((result = _context.Synchronization.WaitFor(syncObjs, timeout, out handleIndex)) == Result.Success)
                {
                    KServerSession session = currentProcess.HandleTable.GetObject<KServerSession>(handles[handleIndex]);

                    if (session == null)
                    {
                        break;
                    }

                    if ((result = session.Receive()) != KernelResult.NotFound)
                    {
                        break;
                    }
                }
            }

            ArrayPool<KSynchronizationObject>.Shared.Return(syncObjsArray, true);

            return result;
        }

        [Svc(0x44)]
        public Result ReplyAndReceiveWithUserBuffer(
            out int handleIndex,
            [PointerSized] ulong messagePtr,
            [PointerSized] ulong messageSize,
            [PointerSized] ulong handlesPtr,
            int handlesCount,
            int replyTargetHandle,
            long timeout)
        {
            handleIndex = 0;

            if ((uint)handlesCount > 0x40)
            {
                return KernelResult.MaximumExceeded;
            }

            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            ulong copySize = (ulong)((long)handlesCount * 4);

            if (!currentProcess.MemoryManager.InsideAddrSpace(handlesPtr, copySize))
            {
                return KernelResult.UserCopyFailed;
            }

            if (handlesPtr + copySize < handlesPtr)
            {
                return KernelResult.UserCopyFailed;
            }

            Result result = currentProcess.MemoryManager.BorrowIpcBuffer(messagePtr, messageSize);

            if (result != Result.Success)
            {
                return result;
            }

            int[] handles = new int[handlesCount];

            if (!KernelTransfer.UserToKernelArray<int>(handlesPtr, handles))
            {
                currentProcess.MemoryManager.UnborrowIpcBuffer(messagePtr, messageSize);

                return KernelResult.UserCopyFailed;
            }

            KSynchronizationObject[] syncObjs = new KSynchronizationObject[handlesCount];

            for (int index = 0; index < handlesCount; index++)
            {
                KSynchronizationObject obj = currentProcess.HandleTable.GetObject<KSynchronizationObject>(handles[index]);

                if (obj == null)
                {
                    currentProcess.MemoryManager.UnborrowIpcBuffer(messagePtr, messageSize);

                    return KernelResult.InvalidHandle;
                }

                syncObjs[index] = obj;
            }

            if (replyTargetHandle != 0)
            {
                KServerSession replyTarget = currentProcess.HandleTable.GetObject<KServerSession>(replyTargetHandle);

                if (replyTarget == null)
                {
                    result = KernelResult.InvalidHandle;
                }
                else
                {
                    result = replyTarget.Reply(messagePtr, messageSize);
                }
            }

            if (result == Result.Success)
            {
                if (timeout > 0)
                {
                    timeout += KTimeManager.DefaultTimeIncrementNanoseconds;
                }

                while ((result = _context.Synchronization.WaitFor(syncObjs, timeout, out handleIndex)) == Result.Success)
                {
                    KServerSession session = currentProcess.HandleTable.GetObject<KServerSession>(handles[handleIndex]);

                    if (session == null)
                    {
                        break;
                    }

                    if ((result = session.Receive(messagePtr, messageSize)) != KernelResult.NotFound)
                    {
                        break;
                    }
                }
            }

            currentProcess.MemoryManager.UnborrowIpcBuffer(messagePtr, messageSize);

            return result;
        }

        [Svc(0x70)]
        public Result CreatePort(
            out int serverPortHandle,
            out int clientPortHandle,
            int maxSessions,
            bool isLight,
            [PointerSized] ulong namePtr)
        {
            // The kernel doesn't use the name pointer, so we can just pass null as the name.
            return CreatePort(out serverPortHandle, out clientPortHandle, maxSessions, isLight, null);
        }

        public Result CreatePort(
            out int serverPortHandle,
            out int clientPortHandle,
            int maxSessions,
            bool isLight,
            string name)
        {
            serverPortHandle = clientPortHandle = 0;

            if (maxSessions < 1)
            {
                return KernelResult.MaximumExceeded;
            }

            KPort port = new(_context, maxSessions, isLight, name);

            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            Result result = currentProcess.HandleTable.GenerateHandle(port.ClientPort, out clientPortHandle);

            if (result != Result.Success)
            {
                return result;
            }

            result = currentProcess.HandleTable.GenerateHandle(port.ServerPort, out serverPortHandle);

            if (result != Result.Success)
            {
                currentProcess.HandleTable.CloseHandle(clientPortHandle);
            }

            return result;
        }

        [Svc(0x71)]
        public Result ManageNamedPort(out int handle, [PointerSized] ulong namePtr, int maxSessions)
        {
            handle = 0;

            if (!KernelTransfer.UserToKernelString(out string name, namePtr, 12))
            {
                return KernelResult.UserCopyFailed;
            }

            if (name.Length > 11)
            {
                return KernelResult.MaximumExceeded;
            }

            return ManageNamedPort(out handle, name, maxSessions);
        }

        public Result ManageNamedPort(out int handle, string name, int maxSessions)
        {
            handle = 0;

            if (maxSessions < 0)
            {
                return KernelResult.MaximumExceeded;
            }

            if (maxSessions == 0)
            {
                return KAutoObject.RemoveName(_context, name);
            }

            KPort port = new(_context, maxSessions, false, null);

            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            Result result = currentProcess.HandleTable.GenerateHandle(port.ServerPort, out handle);

            if (result != Result.Success)
            {
                return result;
            }

            result = port.ClientPort.SetName(name);

            if (result != Result.Success)
            {
                currentProcess.HandleTable.CloseHandle(handle);
            }

            return result;
        }

        [Svc(0x72)]
        public Result ConnectToPort(out int clientSessionHandle, int clientPortHandle)
        {
            clientSessionHandle = 0;

            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            KClientPort clientPort = currentProcess.HandleTable.GetObject<KClientPort>(clientPortHandle);

            if (clientPort == null)
            {
                return KernelResult.InvalidHandle;
            }

            Result result = currentProcess.HandleTable.ReserveHandle(out int handle);

            if (result != Result.Success)
            {
                return result;
            }

            KAutoObject session;

            if (clientPort.IsLight)
            {
                result = clientPort.ConnectLight(out KLightClientSession clientSession);

                session = clientSession;
            }
            else
            {
                result = clientPort.Connect(out KClientSession clientSession);

                session = clientSession;
            }

            if (result != Result.Success)
            {
                currentProcess.HandleTable.CancelHandleReservation(handle);

                return result;
            }

            currentProcess.HandleTable.SetReservedHandleObj(handle, session);

            session.DecrementReferenceCount();

            clientSessionHandle = handle;

            return result;
        }

        // Memory

        [Svc(1)]
        public Result SetHeapSize([PointerSized] out ulong address, [PointerSized] ulong size)
        {
            if ((size & 0xfffffffe001fffff) != 0)
            {
                address = 0;

                return KernelResult.InvalidSize;
            }

            KProcess process = KernelStatic.GetCurrentProcess();

            return process.MemoryManager.SetHeapSize(size, out address);
        }

        [Svc(2)]
        public Result SetMemoryPermission([PointerSized] ulong address, [PointerSized] ulong size, KMemoryPermission permission)
        {
            if (!PageAligned(address))
            {
                return KernelResult.InvalidAddress;
            }

            if (!PageAligned(size) || size == 0)
            {
                return KernelResult.InvalidSize;
            }

            if (address + size <= address)
            {
                return KernelResult.InvalidMemState;
            }

            if (permission != KMemoryPermission.None && (permission | KMemoryPermission.Write) != KMemoryPermission.ReadAndWrite)
            {
                return KernelResult.InvalidPermission;
            }

            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            if (!currentProcess.MemoryManager.InsideAddrSpace(address, size))
            {
                return KernelResult.InvalidMemState;
            }

            return currentProcess.MemoryManager.SetMemoryPermission(address, size, permission);
        }

        [Svc(3)]
        public Result SetMemoryAttribute(
            [PointerSized] ulong address,
            [PointerSized] ulong size,
            MemoryAttribute attributeMask,
            MemoryAttribute attributeValue)
        {
            if (!PageAligned(address))
            {
                return KernelResult.InvalidAddress;
            }

            if (!PageAligned(size) || size == 0)
            {
                return KernelResult.InvalidSize;
            }

            MemoryAttribute attributes = attributeMask | attributeValue;

            const MemoryAttribute SupportedAttributes = MemoryAttribute.Uncached | MemoryAttribute.PermissionLocked;

            if (attributes != attributeMask ||
               (attributes | SupportedAttributes) != SupportedAttributes)
            {
                return KernelResult.InvalidCombination;
            }

            // The permission locked attribute can't be unset.
            if ((attributeMask & MemoryAttribute.PermissionLocked) != (attributeValue & MemoryAttribute.PermissionLocked))
            {
                return KernelResult.InvalidCombination;
            }

            KProcess process = KernelStatic.GetCurrentProcess();

            if (!process.MemoryManager.InsideAddrSpace(address, size))
            {
                return KernelResult.InvalidMemState;
            }

            Result result = process.MemoryManager.SetMemoryAttribute(
                address,
                size,
                attributeMask,
                attributeValue);

            return result;
        }

        [Svc(4)]
        public Result MapMemory([PointerSized] ulong dst, [PointerSized] ulong src, [PointerSized] ulong size)
        {
            if (!PageAligned(src | dst))
            {
                return KernelResult.InvalidAddress;
            }

            if (!PageAligned(size) || size == 0)
            {
                return KernelResult.InvalidSize;
            }

            if (src + size <= src || dst + size <= dst)
            {
                return KernelResult.InvalidMemState;
            }

            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            if (!currentProcess.MemoryManager.InsideAddrSpace(src, size))
            {
                return KernelResult.InvalidMemState;
            }

            if (currentProcess.MemoryManager.OutsideStackRegion(dst, size) ||
                currentProcess.MemoryManager.InsideHeapRegion(dst, size) ||
                currentProcess.MemoryManager.InsideAliasRegion(dst, size))
            {
                return KernelResult.InvalidMemRange;
            }

            KProcess process = KernelStatic.GetCurrentProcess();

            return process.MemoryManager.Map(dst, src, size);
        }

        [Svc(5)]
        public Result UnmapMemory([PointerSized] ulong dst, [PointerSized] ulong src, [PointerSized] ulong size)
        {
            if (!PageAligned(src | dst))
            {
                return KernelResult.InvalidAddress;
            }

            if (!PageAligned(size) || size == 0)
            {
                return KernelResult.InvalidSize;
            }

            if (src + size <= src || dst + size <= dst)
            {
                return KernelResult.InvalidMemState;
            }

            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            if (!currentProcess.MemoryManager.InsideAddrSpace(src, size))
            {
                return KernelResult.InvalidMemState;
            }

            if (currentProcess.MemoryManager.OutsideStackRegion(dst, size) ||
                currentProcess.MemoryManager.InsideHeapRegion(dst, size) ||
                currentProcess.MemoryManager.InsideAliasRegion(dst, size))
            {
                return KernelResult.InvalidMemRange;
            }

            KProcess process = KernelStatic.GetCurrentProcess();

            return process.MemoryManager.Unmap(dst, src, size);
        }

        [Svc(6)]
        public Result QueryMemory([PointerSized] ulong infoPtr, [PointerSized] out ulong pageInfo, [PointerSized] ulong address)
        {
            Result result = QueryMemory(out MemoryInfo info, out pageInfo, address);

            if (result == Result.Success)
            {
                return KernelTransfer.KernelToUser(infoPtr, info)
                    ? Result.Success
                    : KernelResult.InvalidMemState;
            }

            return result;
        }

        public Result QueryMemory(out MemoryInfo info, out ulong pageInfo, ulong address)
        {
            KProcess process = KernelStatic.GetCurrentProcess();

            KMemoryInfo blockInfo = process.MemoryManager.QueryMemory(address);

            info = new MemoryInfo(
                blockInfo.Address,
                blockInfo.Size,
                blockInfo.State & MemoryState.UserMask,
                blockInfo.Attribute,
                blockInfo.Permission & KMemoryPermission.UserMask,
                blockInfo.IpcRefCount,
                blockInfo.DeviceRefCount);

            pageInfo = 0;

            return Result.Success;
        }

        [Svc(0x13)]
        public Result MapSharedMemory(int handle, [PointerSized] ulong address, [PointerSized] ulong size, KMemoryPermission permission)
        {
            if (!PageAligned(address))
            {
                return KernelResult.InvalidAddress;
            }

            if (!PageAligned(size) || size == 0)
            {
                return KernelResult.InvalidSize;
            }

            if (address + size <= address)
            {
                return KernelResult.InvalidMemState;
            }

            if ((permission | KMemoryPermission.Write) != KMemoryPermission.ReadAndWrite)
            {
                return KernelResult.InvalidPermission;
            }

            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            KSharedMemory sharedMemory = currentProcess.HandleTable.GetObject<KSharedMemory>(handle);

            if (sharedMemory == null)
            {
                return KernelResult.InvalidHandle;
            }

            if (currentProcess.MemoryManager.IsInvalidRegion(address, size) ||
                currentProcess.MemoryManager.InsideHeapRegion(address, size) ||
                currentProcess.MemoryManager.InsideAliasRegion(address, size))
            {
                return KernelResult.InvalidMemRange;
            }

            return sharedMemory.MapIntoProcess(
                currentProcess.MemoryManager,
                address,
                size,
                currentProcess,
                permission);
        }

        [Svc(0x14)]
        public Result UnmapSharedMemory(int handle, [PointerSized] ulong address, [PointerSized] ulong size)
        {
            if (!PageAligned(address))
            {
                return KernelResult.InvalidAddress;
            }

            if (!PageAligned(size) || size == 0)
            {
                return KernelResult.InvalidSize;
            }

            if (address + size <= address)
            {
                return KernelResult.InvalidMemState;
            }

            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            KSharedMemory sharedMemory = currentProcess.HandleTable.GetObject<KSharedMemory>(handle);

            if (sharedMemory == null)
            {
                return KernelResult.InvalidHandle;
            }

            if (currentProcess.MemoryManager.IsInvalidRegion(address, size) ||
                currentProcess.MemoryManager.InsideHeapRegion(address, size) ||
                currentProcess.MemoryManager.InsideAliasRegion(address, size))
            {
                return KernelResult.InvalidMemRange;
            }

            return sharedMemory.UnmapFromProcess(
                currentProcess.MemoryManager,
                address,
                size,
                currentProcess);
        }

        [Svc(0x15)]
        public Result CreateTransferMemory(out int handle, [PointerSized] ulong address, [PointerSized] ulong size, KMemoryPermission permission)
        {
            handle = 0;

            if (!PageAligned(address))
            {
                return KernelResult.InvalidAddress;
            }

            if (!PageAligned(size) || size == 0)
            {
                return KernelResult.InvalidSize;
            }

            if (address + size <= address)
            {
                return KernelResult.InvalidMemState;
            }

            if (permission > KMemoryPermission.ReadAndWrite || permission == KMemoryPermission.Write)
            {
                return KernelResult.InvalidPermission;
            }

            KProcess process = KernelStatic.GetCurrentProcess();

            KResourceLimit resourceLimit = process.ResourceLimit;

            if (resourceLimit != null && !resourceLimit.Reserve(LimitableResource.TransferMemory, 1))
            {
                return KernelResult.ResLimitExceeded;
            }

            void CleanUpForError()
            {
                resourceLimit?.Release(LimitableResource.TransferMemory, 1);
            }

            if (!process.MemoryManager.InsideAddrSpace(address, size))
            {
                CleanUpForError();

                return KernelResult.InvalidMemState;
            }

            KTransferMemory transferMemory = new(_context);

            Result result = transferMemory.Initialize(address, size, permission);

            if (result != Result.Success)
            {
                CleanUpForError();

                return result;
            }

            result = process.HandleTable.GenerateHandle(transferMemory, out handle);

            transferMemory.DecrementReferenceCount();

            return result;
        }

        [Svc(0x51)]
        public Result MapTransferMemory(int handle, [PointerSized] ulong address, [PointerSized] ulong size, KMemoryPermission permission)
        {
            if (!PageAligned(address))
            {
                return KernelResult.InvalidAddress;
            }

            if (!PageAligned(size) || size == 0)
            {
                return KernelResult.InvalidSize;
            }

            if (address + size <= address)
            {
                return KernelResult.InvalidMemState;
            }

            if (permission > KMemoryPermission.ReadAndWrite || permission == KMemoryPermission.Write)
            {
                return KernelResult.InvalidPermission;
            }

            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            KTransferMemory transferMemory = currentProcess.HandleTable.GetObject<KTransferMemory>(handle);

            if (transferMemory == null)
            {
                return KernelResult.InvalidHandle;
            }

            if (currentProcess.MemoryManager.IsInvalidRegion(address, size) ||
                currentProcess.MemoryManager.InsideHeapRegion(address, size) ||
                currentProcess.MemoryManager.InsideAliasRegion(address, size))
            {
                return KernelResult.InvalidMemRange;
            }

            return transferMemory.MapIntoProcess(
                currentProcess.MemoryManager,
                address,
                size,
                currentProcess,
                permission);
        }

        [Svc(0x52)]
        public Result UnmapTransferMemory(int handle, [PointerSized] ulong address, [PointerSized] ulong size)
        {
            if (!PageAligned(address))
            {
                return KernelResult.InvalidAddress;
            }

            if (!PageAligned(size) || size == 0)
            {
                return KernelResult.InvalidSize;
            }

            if (address + size <= address)
            {
                return KernelResult.InvalidMemState;
            }

            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            KTransferMemory transferMemory = currentProcess.HandleTable.GetObject<KTransferMemory>(handle);

            if (transferMemory == null)
            {
                return KernelResult.InvalidHandle;
            }

            if (currentProcess.MemoryManager.IsInvalidRegion(address, size) ||
                currentProcess.MemoryManager.InsideHeapRegion(address, size) ||
                currentProcess.MemoryManager.InsideAliasRegion(address, size))
            {
                return KernelResult.InvalidMemRange;
            }

            return transferMemory.UnmapFromProcess(
                currentProcess.MemoryManager,
                address,
                size,
                currentProcess);
        }

        [Svc(0x2c)]
        public Result MapPhysicalMemory([PointerSized] ulong address, [PointerSized] ulong size)
        {
            if (!PageAligned(address))
            {
                return KernelResult.InvalidAddress;
            }

            if (!PageAligned(size) || size == 0)
            {
                return KernelResult.InvalidSize;
            }

            if (address + size <= address)
            {
                return KernelResult.InvalidMemRange;
            }

            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            if ((currentProcess.PersonalMmHeapPagesCount & 0xfffffffffffff) == 0)
            {
                return KernelResult.InvalidState;
            }

            if (!currentProcess.MemoryManager.InsideAddrSpace(address, size) ||
                 currentProcess.MemoryManager.OutsideAliasRegion(address, size))
            {
                return KernelResult.InvalidMemRange;
            }

            KProcess process = KernelStatic.GetCurrentProcess();

            return process.MemoryManager.MapPhysicalMemory(address, size);
        }

        [Svc(0x2d)]
        public Result UnmapPhysicalMemory([PointerSized] ulong address, [PointerSized] ulong size)
        {
            if (!PageAligned(address))
            {
                return KernelResult.InvalidAddress;
            }

            if (!PageAligned(size) || size == 0)
            {
                return KernelResult.InvalidSize;
            }

            if (address + size <= address)
            {
                return KernelResult.InvalidMemRange;
            }

            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            if ((currentProcess.PersonalMmHeapPagesCount & 0xfffffffffffff) == 0)
            {
                return KernelResult.InvalidState;
            }

            if (!currentProcess.MemoryManager.InsideAddrSpace(address, size) ||
                 currentProcess.MemoryManager.OutsideAliasRegion(address, size))
            {
                return KernelResult.InvalidMemRange;
            }

            KProcess process = KernelStatic.GetCurrentProcess();

            return process.MemoryManager.UnmapPhysicalMemory(address, size);
        }

        [Svc(0x4b)]
        public Result CreateCodeMemory(out int handle, [PointerSized] ulong address, [PointerSized] ulong size)
        {
            handle = 0;

            if (!PageAligned(address))
            {
                return KernelResult.InvalidAddress;
            }

            if (!PageAligned(size) || size == 0)
            {
                return KernelResult.InvalidSize;
            }

            if (size + address <= address)
            {
                return KernelResult.InvalidMemState;
            }

            KCodeMemory codeMemory = new(_context);

            using var _ = new OnScopeExit(codeMemory.DecrementReferenceCount);

            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            if (!currentProcess.MemoryManager.InsideAddrSpace(address, size))
            {
                return KernelResult.InvalidMemState;
            }

            Result result = codeMemory.Initialize(address, size);

            if (result != Result.Success)
            {
                return result;
            }

            return currentProcess.HandleTable.GenerateHandle(codeMemory, out handle);
        }

        [Svc(0x4c)]
        public Result ControlCodeMemory(
            int handle,
            CodeMemoryOperation op,
            ulong address,
            ulong size,
            KMemoryPermission permission)
        {
            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            KCodeMemory codeMemory = currentProcess.HandleTable.GetObject<KCodeMemory>(handle);

            // Newer versions of the kernel also returns an error here if the owner and process
            // where the operation will happen are the same. We do not return an error here
            // for homebrew because some of them requires this to be patched out to work (for JIT).
            if (codeMemory == null || (!currentProcess.AllowCodeMemoryForJit && codeMemory.Owner == currentProcess))
            {
                return KernelResult.InvalidHandle;
            }

            switch (op)
            {
                case CodeMemoryOperation.Map:
                    if (!currentProcess.MemoryManager.CanContain(address, size, MemoryState.CodeWritable))
                    {
                        return KernelResult.InvalidMemRange;
                    }

                    if (permission != KMemoryPermission.ReadAndWrite)
                    {
                        return KernelResult.InvalidPermission;
                    }

                    return codeMemory.Map(address, size, permission);

                case CodeMemoryOperation.MapToOwner:
                    if (!currentProcess.MemoryManager.CanContain(address, size, MemoryState.CodeReadOnly))
                    {
                        return KernelResult.InvalidMemRange;
                    }

                    if (permission != KMemoryPermission.Read && permission != KMemoryPermission.ReadAndExecute)
                    {
                        return KernelResult.InvalidPermission;
                    }

                    return codeMemory.MapToOwner(address, size, permission);

                case CodeMemoryOperation.Unmap:
                    if (!currentProcess.MemoryManager.CanContain(address, size, MemoryState.CodeWritable))
                    {
                        return KernelResult.InvalidMemRange;
                    }

                    if (permission != KMemoryPermission.None)
                    {
                        return KernelResult.InvalidPermission;
                    }

                    return codeMemory.Unmap(address, size);

                case CodeMemoryOperation.UnmapFromOwner:
                    if (!currentProcess.MemoryManager.CanContain(address, size, MemoryState.CodeReadOnly))
                    {
                        return KernelResult.InvalidMemRange;
                    }

                    if (permission != KMemoryPermission.None)
                    {
                        return KernelResult.InvalidPermission;
                    }

                    return codeMemory.UnmapFromOwner(address, size);

                default:
                    return KernelResult.InvalidEnumValue;
            }
        }

        [Svc(0x73)]
        public Result SetProcessMemoryPermission(
            int handle,
            ulong src,
            ulong size,
            KMemoryPermission permission)
        {
            if (!PageAligned(src))
            {
                return KernelResult.InvalidAddress;
            }

            if (!PageAligned(size) || size == 0)
            {
                return KernelResult.InvalidSize;
            }

            if (permission != KMemoryPermission.None &&
                permission != KMemoryPermission.Read &&
                permission != KMemoryPermission.ReadAndWrite &&
                permission != KMemoryPermission.ReadAndExecute)
            {
                return KernelResult.InvalidPermission;
            }

            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            KProcess targetProcess = currentProcess.HandleTable.GetObject<KProcess>(handle);

            if (targetProcess == null)
            {
                return KernelResult.InvalidHandle;
            }

            if (targetProcess.MemoryManager.OutsideAddrSpace(src, size))
            {
                return KernelResult.InvalidMemState;
            }

            return targetProcess.MemoryManager.SetProcessMemoryPermission(src, size, permission);
        }

        [Svc(0x74)]
        public Result MapProcessMemory(
            [PointerSized] ulong dst,
            int handle,
            ulong src,
            [PointerSized] ulong size)
        {
            if (!PageAligned(src) || !PageAligned(dst))
            {
                return KernelResult.InvalidAddress;
            }

            if (!PageAligned(size) || size == 0)
            {
                return KernelResult.InvalidSize;
            }

            if (dst + size <= dst || src + size <= src)
            {
                return KernelResult.InvalidMemRange;
            }

            KProcess dstProcess = KernelStatic.GetCurrentProcess();
            KProcess srcProcess = dstProcess.HandleTable.GetObject<KProcess>(handle);

            if (srcProcess == null)
            {
                return KernelResult.InvalidHandle;
            }

            if (!srcProcess.MemoryManager.InsideAddrSpace(src, size) ||
                !dstProcess.MemoryManager.CanContain(dst, size, MemoryState.ProcessMemory))
            {
                return KernelResult.InvalidMemRange;
            }

            KPageList pageList = new();

            Result result = srcProcess.MemoryManager.GetPagesIfStateEquals(
                src,
                size,
                MemoryState.MapProcessAllowed,
                MemoryState.MapProcessAllowed,
                KMemoryPermission.None,
                KMemoryPermission.None,
                MemoryAttribute.Mask,
                MemoryAttribute.None,
                pageList);

            if (result != Result.Success)
            {
                return result;
            }

            return dstProcess.MemoryManager.MapPages(dst, pageList, MemoryState.ProcessMemory, KMemoryPermission.ReadAndWrite);
        }

        [Svc(0x75)]
        public Result UnmapProcessMemory(
            [PointerSized] ulong dst,
            int handle,
            ulong src,
            [PointerSized] ulong size)
        {
            if (!PageAligned(src) || !PageAligned(dst))
            {
                return KernelResult.InvalidAddress;
            }

            if (!PageAligned(size) || size == 0)
            {
                return KernelResult.InvalidSize;
            }

            if (dst + size <= dst || src + size <= src)
            {
                return KernelResult.InvalidMemRange;
            }

            KProcess dstProcess = KernelStatic.GetCurrentProcess();
            KProcess srcProcess = dstProcess.HandleTable.GetObject<KProcess>(handle);

            if (srcProcess == null)
            {
                return KernelResult.InvalidHandle;
            }

            if (!srcProcess.MemoryManager.InsideAddrSpace(src, size) ||
                !dstProcess.MemoryManager.CanContain(dst, size, MemoryState.ProcessMemory))
            {
                return KernelResult.InvalidMemRange;
            }

            Result result = dstProcess.MemoryManager.UnmapProcessMemory(dst, size, srcProcess.MemoryManager, src);

            if (result != Result.Success)
            {
                return result;
            }

            return Result.Success;
        }

        [Svc(0x77)]
        public Result MapProcessCodeMemory(int handle, ulong dst, ulong src, ulong size)
        {
            if (!PageAligned(dst) || !PageAligned(src))
            {
                return KernelResult.InvalidAddress;
            }

            if (!PageAligned(size) || size == 0)
            {
                return KernelResult.InvalidSize;
            }

            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            KProcess targetProcess = currentProcess.HandleTable.GetObject<KProcess>(handle);

            if (targetProcess == null)
            {
                return KernelResult.InvalidHandle;
            }

            if (targetProcess.MemoryManager.OutsideAddrSpace(dst, size) ||
                targetProcess.MemoryManager.OutsideAddrSpace(src, size) ||
                targetProcess.MemoryManager.InsideAliasRegion(dst, size) ||
                targetProcess.MemoryManager.InsideHeapRegion(dst, size))
            {
                return KernelResult.InvalidMemRange;
            }

            if (size + dst <= dst || size + src <= src)
            {
                return KernelResult.InvalidMemState;
            }

            return targetProcess.MemoryManager.MapProcessCodeMemory(dst, src, size);
        }

        [Svc(0x78)]
        public Result UnmapProcessCodeMemory(int handle, ulong dst, ulong src, ulong size)
        {
            if (!PageAligned(dst) || !PageAligned(src))
            {
                return KernelResult.InvalidAddress;
            }

            if (!PageAligned(size) || size == 0)
            {
                return KernelResult.InvalidSize;
            }

            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            KProcess targetProcess = currentProcess.HandleTable.GetObject<KProcess>(handle);

            if (targetProcess == null)
            {
                return KernelResult.InvalidHandle;
            }

            if (targetProcess.MemoryManager.OutsideAddrSpace(dst, size) ||
                targetProcess.MemoryManager.OutsideAddrSpace(src, size) ||
                targetProcess.MemoryManager.InsideAliasRegion(dst, size) ||
                targetProcess.MemoryManager.InsideHeapRegion(dst, size))
            {
                return KernelResult.InvalidMemRange;
            }

            if (size + dst <= dst || size + src <= src)
            {
                return KernelResult.InvalidMemState;
            }

            return targetProcess.MemoryManager.UnmapProcessCodeMemory(dst, src, size);
        }

        private static bool PageAligned(ulong address)
        {
            return (address & (KPageTableBase.PageSize - 1)) == 0;
        }

        // System

        [Svc(0x7b)]
        public Result TerminateProcess(int handle)
        {
            KProcess process = KernelStatic.GetCurrentProcess();

            process = process.HandleTable.GetObject<KProcess>(handle);

            Result result;

            if (process != null)
            {
                if (process == KernelStatic.GetCurrentProcess())
                {
                    result = Result.Success;
                    process.DecrementToZeroWhileTerminatingCurrent();
                }
                else
                {
                    result = process.Terminate();
                    process.DecrementReferenceCount();
                }
            }
            else
            {
                result = KernelResult.InvalidHandle;
            }

            return result;
        }

        [Svc(7)]
        public void ExitProcess()
        {
            KernelStatic.GetCurrentProcess().TerminateCurrentProcess();
        }

        [Svc(0x11)]
        public Result SignalEvent(int handle)
        {
            KProcess process = KernelStatic.GetCurrentProcess();

            KWritableEvent writableEvent = process.HandleTable.GetObject<KWritableEvent>(handle);

            Result result;

            if (writableEvent != null)
            {
                writableEvent.Signal();

                result = Result.Success;
            }
            else
            {
                result = KernelResult.InvalidHandle;
            }

            return result;
        }

        [Svc(0x12)]
        public Result ClearEvent(int handle)
        {
            Result result;

            KProcess process = KernelStatic.GetCurrentProcess();

            KWritableEvent writableEvent = process.HandleTable.GetObject<KWritableEvent>(handle);

            if (writableEvent == null)
            {
                KReadableEvent readableEvent = process.HandleTable.GetObject<KReadableEvent>(handle);

                result = readableEvent?.Clear() ?? KernelResult.InvalidHandle;
            }
            else
            {
                result = writableEvent.Clear();
            }

            return result;
        }

        [Svc(0x16)]
        public Result CloseHandle(int handle)
        {
            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            return currentProcess.HandleTable.CloseHandle(handle) ? Result.Success : KernelResult.InvalidHandle;
        }

        [Svc(0x17)]
        public Result ResetSignal(int handle)
        {
            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            KReadableEvent readableEvent = currentProcess.HandleTable.GetObject<KReadableEvent>(handle);

            Result result;

            if (readableEvent != null)
            {
                result = readableEvent.ClearIfSignaled();
            }
            else
            {
                KProcess process = currentProcess.HandleTable.GetKProcess(handle);

                if (process != null)
                {
                    result = process.ClearIfNotExited();
                }
                else
                {
                    result = KernelResult.InvalidHandle;
                }
            }

            return result;
        }

        [Svc(0x1e)]
        public ulong GetSystemTick()
        {
            return _context.TickSource.Counter;
        }

        [Svc(0x26)]
        public void Break(ulong reason)
        {
            KThread currentThread = KernelStatic.GetCurrentThread();

            if ((reason & (1UL << 31)) == 0)
            {
                currentThread.PrintGuestStackTrace();
                currentThread.PrintGuestRegisterPrintout();

                // As the process is exiting, this is probably caused by emulation termination.
                if (currentThread.Owner.State == ProcessState.Exiting)
                {
                    return;
                }

                // TODO: Debug events.
                currentThread.Owner.TerminateCurrentProcess();

                throw new GuestBrokeExecutionException();
            }
            else
            {
                Logger.Debug?.Print(LogClass.KernelSvc, "Debugger triggered.");
            }
        }

        [Svc(0x27)]
        public void OutputDebugString([PointerSized] ulong strPtr, [PointerSized] ulong size)
        {
            KProcess process = KernelStatic.GetCurrentProcess();

            string str = MemoryHelper.ReadAsciiString(process.CpuMemory, strPtr, (long)size);

            Logger.Warning?.Print(LogClass.KernelSvc, str);
        }

        [Svc(0x29)]
        public Result GetInfo(out ulong value, InfoType id, int handle, long subId)
        {
            value = 0;

            switch (id)
            {
                case InfoType.CoreMask:
                case InfoType.PriorityMask:
                case InfoType.AliasRegionAddress:
                case InfoType.AliasRegionSize:
                case InfoType.HeapRegionAddress:
                case InfoType.HeapRegionSize:
                case InfoType.TotalMemorySize:
                case InfoType.UsedMemorySize:
                case InfoType.AslrRegionAddress:
                case InfoType.AslrRegionSize:
                case InfoType.StackRegionAddress:
                case InfoType.StackRegionSize:
                case InfoType.SystemResourceSizeTotal:
                case InfoType.SystemResourceSizeUsed:
                case InfoType.ProgramId:
                case InfoType.UserExceptionContextAddress:
                case InfoType.TotalNonSystemMemorySize:
                case InfoType.UsedNonSystemMemorySize:
                case InfoType.IsApplication:
                case InfoType.FreeThreadCount:
                case InfoType.AliasRegionExtraSize:
                    {
                        if (subId != 0)
                        {
                            return KernelResult.InvalidCombination;
                        }

                        KProcess currentProcess = KernelStatic.GetCurrentProcess();

                        KProcess process = currentProcess.HandleTable.GetKProcess(handle);

                        if (process == null)
                        {
                            return KernelResult.InvalidHandle;
                        }

                        switch (id)
                        {
                            case InfoType.CoreMask:
                                value = process.Capabilities.AllowedCpuCoresMask;
                                break;
                            case InfoType.PriorityMask:
                                value = process.Capabilities.AllowedThreadPriosMask;
                                break;

                            case InfoType.AliasRegionAddress:
                                value = process.MemoryManager.AliasRegionStart;
                                break;
                            case InfoType.AliasRegionSize:
                                value = process.MemoryManager.AliasRegionEnd - process.MemoryManager.AliasRegionStart;
                                break;

                            case InfoType.HeapRegionAddress:
                                value = process.MemoryManager.HeapRegionStart;
                                break;
                            case InfoType.HeapRegionSize:
                                value = process.MemoryManager.HeapRegionEnd - process.MemoryManager.HeapRegionStart;
                                break;

                            case InfoType.TotalMemorySize:
                                value = process.GetMemoryCapacity();
                                break;
                            case InfoType.UsedMemorySize:
                                value = process.GetMemoryUsage();
                                break;

                            case InfoType.AslrRegionAddress:
                                value = process.MemoryManager.GetAddrSpaceBaseAddr();
                                break;
                            case InfoType.AslrRegionSize:
                                value = process.MemoryManager.GetAddrSpaceSize();
                                break;

                            case InfoType.StackRegionAddress:
                                value = process.MemoryManager.StackRegionStart;
                                break;
                            case InfoType.StackRegionSize:
                                value = process.MemoryManager.StackRegionEnd - process.MemoryManager.StackRegionStart;
                                break;

                            case InfoType.SystemResourceSizeTotal:
                                value = process.PersonalMmHeapPagesCount * KPageTableBase.PageSize;
                                break;
                            case InfoType.SystemResourceSizeUsed:
                                if (process.PersonalMmHeapPagesCount != 0)
                                {
                                    value = process.MemoryManager.GetMmUsedPages() * KPageTableBase.PageSize;
                                }
                                break;

                            case InfoType.ProgramId:
                                value = process.TitleId;
                                break;

                            case InfoType.UserExceptionContextAddress:
                                value = process.UserExceptionContextAddress;
                                break;

                            case InfoType.TotalNonSystemMemorySize:
                                value = process.GetMemoryCapacityWithoutPersonalMmHeap();
                                break;
                            case InfoType.UsedNonSystemMemorySize:
                                value = process.GetMemoryUsageWithoutPersonalMmHeap();
                                break;

                            case InfoType.IsApplication:
                                value = process.IsApplication ? 1UL : 0UL;
                                break;

                            case InfoType.FreeThreadCount:
                                if (process.ResourceLimit != null)
                                {
                                    value = (ulong)(process.ResourceLimit.GetLimitValue(LimitableResource.Thread) -
                                                    process.ResourceLimit.GetCurrentValue(LimitableResource.Thread));
                                }
                                else
                                {
                                    value = 0;
                                }
                                break;

                            case InfoType.AliasRegionExtraSize:
                                value = process.MemoryManager.AliasRegionExtraSize;
                                break;
                        }
                        break;
                    }

                case InfoType.DebuggerAttached:
                    {
                        if (handle != 0)
                        {
                            return KernelResult.InvalidHandle;
                        }

                        if (subId != 0)
                        {
                            return KernelResult.InvalidCombination;
                        }

                        value = KernelStatic.GetCurrentProcess().Debug ? 1UL : 0UL;
                        break;
                    }

                case InfoType.ResourceLimit:
                    {
                        if (handle != 0)
                        {
                            return KernelResult.InvalidHandle;
                        }

                        if (subId != 0)
                        {
                            return KernelResult.InvalidCombination;
                        }

                        KProcess currentProcess = KernelStatic.GetCurrentProcess();

                        if (currentProcess.ResourceLimit != null)
                        {
                            KHandleTable handleTable = currentProcess.HandleTable;
                            KResourceLimit resourceLimit = currentProcess.ResourceLimit;

                            Result result = handleTable.GenerateHandle(resourceLimit, out int resLimHandle);

                            if (result != Result.Success)
                            {
                                return result;
                            }

                            value = (uint)resLimHandle;
                        }
                        break;
                    }

                case InfoType.IdleTickCount:
                    {
                        if (handle != 0)
                        {
                            return KernelResult.InvalidHandle;
                        }

                        int currentCore = KernelStatic.GetCurrentThread().CurrentCore;

                        if (subId != -1 && subId != currentCore)
                        {
                            return KernelResult.InvalidCombination;
                        }

                        value = (ulong)KTimeManager.ConvertHostTicksToTicks(_context.Schedulers[currentCore].TotalIdleTimeTicks);
                        break;
                    }

                case InfoType.RandomEntropy:
                    {
                        if (handle != 0)
                        {
                            return KernelResult.InvalidHandle;
                        }

                        if ((ulong)subId > 3)
                        {
                            return KernelResult.InvalidCombination;
                        }

                        KProcess currentProcess = KernelStatic.GetCurrentProcess();

                        value = currentProcess.RandomEntropy[subId];
                        break;
                    }

                case InfoType.ThreadTickCount:
                    {
                        if (subId < -1 || subId > 3)
                        {
                            return KernelResult.InvalidCombination;
                        }

                        KThread thread = KernelStatic.GetCurrentProcess().HandleTable.GetKThread(handle);

                        if (thread == null)
                        {
                            return KernelResult.InvalidHandle;
                        }

                        KThread currentThread = KernelStatic.GetCurrentThread();

                        int currentCore = currentThread.CurrentCore;

                        if (subId != -1 && subId != currentCore)
                        {
                            return Result.Success;
                        }

                        KScheduler scheduler = _context.Schedulers[currentCore];

                        long timeDelta = PerformanceCounter.ElapsedTicks - scheduler.LastContextSwitchTime;

                        if (subId != -1)
                        {
                            value = (ulong)KTimeManager.ConvertHostTicksToTicks(timeDelta);
                        }
                        else
                        {
                            long totalTimeRunning = thread.TotalTimeRunning;

                            if (thread == currentThread)
                            {
                                totalTimeRunning += timeDelta;
                            }

                            value = (ulong)KTimeManager.ConvertHostTicksToTicks(totalTimeRunning);
                        }
                        break;
                    }

                case InfoType.IsSvcPermitted:
                    {
                        if (handle != 0)
                        {
                            return KernelResult.InvalidHandle;
                        }

                        if (subId != 0x36)
                        {
                            return KernelResult.InvalidCombination;
                        }

                        value = KernelStatic.GetCurrentProcess().IsSvcPermitted((int)subId) ? 1UL : 0UL;
                        break;
                    }

                case InfoType.MesosphereCurrentProcess:
                    {
                        if (handle != 0)
                        {
                            return KernelResult.InvalidHandle;
                        }

                        if (subId != 0)
                        {
                            return KernelResult.InvalidCombination;
                        }

                        KProcess currentProcess = KernelStatic.GetCurrentProcess();
                        KHandleTable handleTable = currentProcess.HandleTable;

                        Result result = handleTable.GenerateHandle(currentProcess, out int outHandle);

                        if (result != Result.Success)
                        {
                            return result;
                        }

                        value = (uint)outHandle;
                        break;
                    }

                default:
                    return KernelResult.InvalidEnumValue;
            }

            return Result.Success;
        }

        [Svc(0x45)]
        public Result CreateEvent(out int wEventHandle, out int rEventHandle)
        {
            KEvent Event = new(_context);

            KProcess process = KernelStatic.GetCurrentProcess();

            Result result = process.HandleTable.GenerateHandle(Event.WritableEvent, out wEventHandle);

            if (result == Result.Success)
            {
                result = process.HandleTable.GenerateHandle(Event.ReadableEvent, out rEventHandle);

                if (result != Result.Success)
                {
                    process.HandleTable.CloseHandle(wEventHandle);
                }
            }
            else
            {
                rEventHandle = 0;
            }

            return result;
        }

        [Svc(0x65)]
        public Result GetProcessList(out int count, [PointerSized] ulong address, int maxCount)
        {
            count = 0;

            if ((maxCount >> 28) != 0)
            {
                return KernelResult.MaximumExceeded;
            }

            if (maxCount != 0)
            {
                KProcess currentProcess = KernelStatic.GetCurrentProcess();

                ulong copySize = (ulong)maxCount * 8;

                if (address + copySize <= address)
                {
                    return KernelResult.InvalidMemState;
                }

                if (currentProcess.MemoryManager.OutsideAddrSpace(address, copySize))
                {
                    return KernelResult.InvalidMemState;
                }
            }

            int copyCount = 0;

            lock (_context.Processes)
            {
                foreach (KProcess process in _context.Processes.Values)
                {
                    if (copyCount < maxCount)
                    {
                        if (!KernelTransfer.KernelToUser(address + (ulong)copyCount * 8, process.Pid))
                        {
                            return KernelResult.UserCopyFailed;
                        }
                    }

                    copyCount++;
                }
            }

            count = copyCount;

            return Result.Success;
        }

        [Svc(0x6f)]
        public Result GetSystemInfo(out long value, uint id, int handle, long subId)
        {
            value = 0;

            if (id > 2)
            {
                return KernelResult.InvalidEnumValue;
            }

            if (handle != 0)
            {
                return KernelResult.InvalidHandle;
            }

            if (id < 2)
            {
                if ((ulong)subId > 3)
                {
                    return KernelResult.InvalidCombination;
                }

                KMemoryRegionManager region = _context.MemoryManager.MemoryRegions[subId];

                switch (id)
                {
                    // Memory region capacity.
                    case 0:
                        value = (long)region.Size;
                        break;

                    // Memory region free space.
                    case 1:
                        {
                            ulong freePagesCount = region.GetFreePages();

                            value = (long)(freePagesCount * KPageTableBase.PageSize);

                            break;
                        }
                }
            }
            else /* if (Id == 2) */
            {
                if ((ulong)subId > 1)
                {
                    return KernelResult.InvalidCombination;
                }

                switch (subId)
                {
                    case 0:
                        value = _context.PrivilegedProcessLowestId;
                        break;
                    case 1:
                        value = _context.PrivilegedProcessHighestId;
                        break;
                }
            }

            return Result.Success;
        }

        [Svc(0x30)]
        public Result GetResourceLimitLimitValue(out long limitValue, int handle, LimitableResource resource)
        {
            limitValue = 0;

            if (resource >= LimitableResource.Count)
            {
                return KernelResult.InvalidEnumValue;
            }

            KResourceLimit resourceLimit = KernelStatic.GetCurrentProcess().HandleTable.GetObject<KResourceLimit>(handle);

            if (resourceLimit == null)
            {
                return KernelResult.InvalidHandle;
            }

            limitValue = resourceLimit.GetLimitValue(resource);

            return Result.Success;
        }

        [Svc(0x31)]
        public Result GetResourceLimitCurrentValue(out long limitValue, int handle, LimitableResource resource)
        {
            limitValue = 0;

            if (resource >= LimitableResource.Count)
            {
                return KernelResult.InvalidEnumValue;
            }

            KResourceLimit resourceLimit = KernelStatic.GetCurrentProcess().HandleTable.GetObject<KResourceLimit>(handle);

            if (resourceLimit == null)
            {
                return KernelResult.InvalidHandle;
            }

            limitValue = resourceLimit.GetCurrentValue(resource);

            return Result.Success;
        }

        [Svc(0x37)]
        public Result GetResourceLimitPeakValue(out long peak, int handle, LimitableResource resource)
        {
            peak = 0;

            if (resource >= LimitableResource.Count)
            {
                return KernelResult.InvalidEnumValue;
            }

            KResourceLimit resourceLimit = KernelStatic.GetCurrentProcess().HandleTable.GetObject<KResourceLimit>(handle);

            if (resourceLimit == null)
            {
                return KernelResult.InvalidHandle;
            }

            peak = resourceLimit.GetPeakValue(resource);

            return Result.Success;
        }

        [Svc(0x7d)]
        public Result CreateResourceLimit(out int handle)
        {
            KResourceLimit limit = new(_context);

            KProcess process = KernelStatic.GetCurrentProcess();

            return process.HandleTable.GenerateHandle(limit, out handle);
        }

        [Svc(0x7e)]
        public Result SetResourceLimitLimitValue(int handle, LimitableResource resource, long limitValue)
        {
            if (resource >= LimitableResource.Count)
            {
                return KernelResult.InvalidEnumValue;
            }

            KResourceLimit resourceLimit = KernelStatic.GetCurrentProcess().HandleTable.GetObject<KResourceLimit>(handle);

            if (resourceLimit == null)
            {
                return KernelResult.InvalidHandle;
            }

            return resourceLimit.SetLimitValue(resource, limitValue);
        }

        // Thread

        [Svc(8)]
        public Result CreateThread(
            out int handle,
            [PointerSized] ulong entrypoint,
            [PointerSized] ulong argsPtr,
            [PointerSized] ulong stackTop,
            int priority,
            int cpuCore)
        {
            return CreateThread(out handle, entrypoint, argsPtr, stackTop, priority, cpuCore, null);
        }

        public Result CreateThread(
            out int handle,
            ulong entrypoint,
            ulong argsPtr,
            ulong stackTop,
            int priority,
            int cpuCore,
            ThreadStart customThreadStart)
        {
            handle = 0;

            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            if (cpuCore == -2)
            {
                cpuCore = currentProcess.DefaultCpuCore;
            }

            if ((uint)cpuCore >= KScheduler.CpuCoresCount || !currentProcess.IsCpuCoreAllowed(cpuCore))
            {
                return KernelResult.InvalidCpuCore;
            }

            if ((uint)priority >= KScheduler.PrioritiesCount || !currentProcess.IsPriorityAllowed(priority))
            {
                return KernelResult.InvalidPriority;
            }

            long timeout = KTimeManager.ConvertMillisecondsToNanoseconds(100);

            if (currentProcess.ResourceLimit != null &&
               !currentProcess.ResourceLimit.Reserve(LimitableResource.Thread, 1, timeout))
            {
                return KernelResult.ResLimitExceeded;
            }

            KThread thread = new(_context);

            Result result = currentProcess.InitializeThread(
                thread,
                entrypoint,
                argsPtr,
                stackTop,
                priority,
                cpuCore,
                customThreadStart);

            if (result == Result.Success)
            {
                KProcess process = KernelStatic.GetCurrentProcess();

                result = process.HandleTable.GenerateHandle(thread, out handle);
            }
            else
            {
                currentProcess.ResourceLimit?.Release(LimitableResource.Thread, 1);
            }

            thread.DecrementReferenceCount();

            return result;
        }

        [Svc(9)]
        public Result StartThread(int handle)
        {
            KProcess process = KernelStatic.GetCurrentProcess();

            KThread thread = process.HandleTable.GetKThread(handle);

            if (thread != null)
            {
                thread.IncrementReferenceCount();

                Result result = thread.Start();

                if (result == Result.Success)
                {
                    thread.IncrementReferenceCount();
                }

                thread.DecrementReferenceCount();

                return result;
            }
            else
            {
                return KernelResult.InvalidHandle;
            }
        }

        [Svc(0xa)]
        public void ExitThread()
        {
            KThread currentThread = KernelStatic.GetCurrentThread();

            currentThread.Exit();
        }

        [Svc(0xb)]
        public void SleepThread(long timeout)
        {
            if (timeout < 1)
            {
                switch (timeout)
                {
                    case 0:
                        KScheduler.Yield(_context);
                        break;
                    case -1:
                        KScheduler.YieldWithLoadBalancing(_context);
                        break;
                    case -2:
                        KScheduler.YieldToAnyThread(_context);
                        break;
                }
            }
            else
            {
                KernelStatic.GetCurrentThread().Sleep(timeout + KTimeManager.DefaultTimeIncrementNanoseconds);
            }
        }

        [Svc(0xc)]
        public Result GetThreadPriority(out int priority, int handle)
        {
            KProcess process = KernelStatic.GetCurrentProcess();

            KThread thread = process.HandleTable.GetKThread(handle);

            if (thread != null)
            {
                priority = thread.DynamicPriority;

                return Result.Success;
            }
            else
            {
                priority = 0;

                return KernelResult.InvalidHandle;
            }
        }

        [Svc(0xd)]
        public Result SetThreadPriority(int handle, int priority)
        {
            // TODO: NPDM check.

            KProcess process = KernelStatic.GetCurrentProcess();

            KThread thread = process.HandleTable.GetKThread(handle);

            if (thread == null)
            {
                return KernelResult.InvalidHandle;
            }

            thread.SetPriority(priority);

            return Result.Success;
        }

        [Svc(0xe)]
        public Result GetThreadCoreMask(out int preferredCore, out ulong affinityMask, int handle)
        {
            KProcess process = KernelStatic.GetCurrentProcess();

            KThread thread = process.HandleTable.GetKThread(handle);

            if (thread != null)
            {
                preferredCore = thread.PreferredCore;
                affinityMask = thread.AffinityMask;

                return Result.Success;
            }
            else
            {
                preferredCore = 0;
                affinityMask = 0;

                return KernelResult.InvalidHandle;
            }
        }

        [Svc(0xf)]
        public Result SetThreadCoreMask(int handle, int preferredCore, ulong affinityMask)
        {
            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            if (preferredCore == -2)
            {
                preferredCore = currentProcess.DefaultCpuCore;

                affinityMask = 1UL << preferredCore;
            }
            else
            {
                if ((currentProcess.Capabilities.AllowedCpuCoresMask | affinityMask) !=
                     currentProcess.Capabilities.AllowedCpuCoresMask)
                {
                    return KernelResult.InvalidCpuCore;
                }

                if (affinityMask == 0)
                {
                    return KernelResult.InvalidCombination;
                }

                if ((uint)preferredCore > KScheduler.CpuCoresCount - 1)
                {
                    if ((preferredCore | 2) != -1)
                    {
                        return KernelResult.InvalidCpuCore;
                    }
                }
                else if ((affinityMask & (1UL << preferredCore)) == 0)
                {
                    return KernelResult.InvalidCombination;
                }
            }

            KProcess process = KernelStatic.GetCurrentProcess();

            KThread thread = process.HandleTable.GetKThread(handle);

            if (thread == null)
            {
                return KernelResult.InvalidHandle;
            }

            return thread.SetCoreAndAffinityMask(preferredCore, affinityMask);
        }

        [Svc(0x10)]
        public int GetCurrentProcessorNumber()
        {
            return KernelStatic.GetCurrentThread().CurrentCore;
        }

        [Svc(0x25)]
        public Result GetThreadId(out ulong threadUid, int handle)
        {
            KProcess process = KernelStatic.GetCurrentProcess();

            KThread thread = process.HandleTable.GetKThread(handle);

            if (thread != null)
            {
                threadUid = thread.ThreadUid;

                return Result.Success;
            }
            else
            {
                threadUid = 0;

                return KernelResult.InvalidHandle;
            }
        }

        [Svc(0x32)]
        public Result SetThreadActivity(int handle, bool pause)
        {
            KProcess process = KernelStatic.GetCurrentProcess();

            KThread thread = process.HandleTable.GetObject<KThread>(handle);

            if (thread == null)
            {
                return KernelResult.InvalidHandle;
            }

            if (thread.Owner != process)
            {
                return KernelResult.InvalidHandle;
            }

            if (thread == KernelStatic.GetCurrentThread())
            {
                return KernelResult.InvalidThread;
            }

            return thread.SetActivity(pause);
        }

        [Svc(0x33)]
        public Result GetThreadContext3([PointerSized] ulong address, int handle)
        {
            KProcess currentProcess = KernelStatic.GetCurrentProcess();
            KThread currentThread = KernelStatic.GetCurrentThread();

            KThread thread = currentProcess.HandleTable.GetObject<KThread>(handle);

            if (thread == null)
            {
                return KernelResult.InvalidHandle;
            }

            if (thread.Owner != currentProcess)
            {
                return KernelResult.InvalidHandle;
            }

            if (currentThread == thread)
            {
                return KernelResult.InvalidThread;
            }

            Result result = thread.GetThreadContext3(out ThreadContext context);

            if (result == Result.Success)
            {
                return KernelTransfer.KernelToUser(address, context)
                    ? Result.Success
                    : KernelResult.InvalidMemState;
            }

            return result;
        }

        // Thread synchronization

        [Svc(0x18)]
        public Result WaitSynchronization(out int handleIndex, [PointerSized] ulong handlesPtr, int handlesCount, long timeout)
        {
            handleIndex = 0;

            if ((uint)handlesCount > KThread.MaxWaitSyncObjects)
            {
                return KernelResult.MaximumExceeded;
            }

            KThread currentThread = KernelStatic.GetCurrentThread();

            if (handlesCount != 0)
            {
                KProcess currentProcess = KernelStatic.GetCurrentProcess();

                if (currentProcess.MemoryManager.AddrSpaceStart > handlesPtr)
                {
                    return KernelResult.UserCopyFailed;
                }

                long handlesSize = handlesCount * 4;

                if (handlesPtr + (ulong)handlesSize <= handlesPtr)
                {
                    return KernelResult.UserCopyFailed;
                }

                if (handlesPtr + (ulong)handlesSize - 1 > currentProcess.MemoryManager.AddrSpaceEnd - 1)
                {
                    return KernelResult.UserCopyFailed;
                }

                Span<int> handles = new Span<int>(currentThread.WaitSyncHandles)[..handlesCount];

                if (!KernelTransfer.UserToKernelArray(handlesPtr, handles))
                {
                    return KernelResult.UserCopyFailed;
                }

                return WaitSynchronization(out handleIndex, handles, timeout);
            }

            return WaitSynchronization(out handleIndex, ReadOnlySpan<int>.Empty, timeout);
        }

        public Result WaitSynchronization(out int handleIndex, ReadOnlySpan<int> handles, long timeout)
        {
            handleIndex = 0;

            if ((uint)handles.Length > KThread.MaxWaitSyncObjects)
            {
                return KernelResult.MaximumExceeded;
            }

            KThread currentThread = KernelStatic.GetCurrentThread();

            var syncObjs = new Span<KSynchronizationObject>(currentThread.WaitSyncObjects)[..handles.Length];

            if (handles.Length != 0)
            {
                KProcess currentProcess = KernelStatic.GetCurrentProcess();

                int processedHandles = 0;

                for (; processedHandles < handles.Length; processedHandles++)
                {
                    KSynchronizationObject syncObj = currentProcess.HandleTable.GetObject<KSynchronizationObject>(handles[processedHandles]);

                    if (syncObj == null)
                    {
                        break;
                    }

                    syncObjs[processedHandles] = syncObj;

                    syncObj.IncrementReferenceCount();
                }

                if (processedHandles != handles.Length)
                {
                    // One or more handles are invalid.
                    for (int index = 0; index < processedHandles; index++)
                    {
                        currentThread.WaitSyncObjects[index].DecrementReferenceCount();
                    }

                    return KernelResult.InvalidHandle;
                }
            }

            if (timeout > 0)
            {
                timeout += KTimeManager.DefaultTimeIncrementNanoseconds;
            }

            Result result = _context.Synchronization.WaitFor(syncObjs, timeout, out handleIndex);

            if (result == KernelResult.PortRemoteClosed)
            {
                result = Result.Success;
            }

            for (int index = 0; index < handles.Length; index++)
            {
                currentThread.WaitSyncObjects[index].DecrementReferenceCount();
            }

            return result;
        }

        [Svc(0x19)]
        public Result CancelSynchronization(int handle)
        {
            KProcess process = KernelStatic.GetCurrentProcess();

            KThread thread = process.HandleTable.GetKThread(handle);

            if (thread == null)
            {
                return KernelResult.InvalidHandle;
            }

            thread.CancelSynchronization();

            return Result.Success;
        }

        [Svc(0x1a)]
        public Result ArbitrateLock(int ownerHandle, [PointerSized] ulong mutexAddress, int requesterHandle)
        {
            if (IsPointingInsideKernel(mutexAddress))
            {
                return KernelResult.InvalidMemState;
            }

            if (IsAddressNotWordAligned(mutexAddress))
            {
                return KernelResult.InvalidAddress;
            }

            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            return currentProcess.AddressArbiter.ArbitrateLock(ownerHandle, mutexAddress, requesterHandle);
        }

        [Svc(0x1b)]
        public Result ArbitrateUnlock([PointerSized] ulong mutexAddress)
        {
            if (IsPointingInsideKernel(mutexAddress))
            {
                return KernelResult.InvalidMemState;
            }

            if (IsAddressNotWordAligned(mutexAddress))
            {
                return KernelResult.InvalidAddress;
            }

            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            return currentProcess.AddressArbiter.ArbitrateUnlock(mutexAddress);
        }

        [Svc(0x1c)]
        public Result WaitProcessWideKeyAtomic(
            [PointerSized] ulong mutexAddress,
            [PointerSized] ulong condVarAddress,
            int handle,
            long timeout)
        {
            if (IsPointingInsideKernel(mutexAddress))
            {
                return KernelResult.InvalidMemState;
            }

            if (IsAddressNotWordAligned(mutexAddress))
            {
                return KernelResult.InvalidAddress;
            }

            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            if (timeout > 0)
            {
                timeout += KTimeManager.DefaultTimeIncrementNanoseconds;
            }

            return currentProcess.AddressArbiter.WaitProcessWideKeyAtomic(
                mutexAddress,
                condVarAddress,
                handle,
                timeout);
        }

        [Svc(0x1d)]
        public Result SignalProcessWideKey([PointerSized] ulong address, int count)
        {
            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            currentProcess.AddressArbiter.SignalProcessWideKey(address, count);

            return Result.Success;
        }

        [Svc(0x34)]
        public Result WaitForAddress([PointerSized] ulong address, ArbitrationType type, int value, long timeout)
        {
            if (IsPointingInsideKernel(address))
            {
                return KernelResult.InvalidMemState;
            }

            if (IsAddressNotWordAligned(address))
            {
                return KernelResult.InvalidAddress;
            }

            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            if (timeout > 0)
            {
                timeout += KTimeManager.DefaultTimeIncrementNanoseconds;
            }

            return type switch
            {
                ArbitrationType.WaitIfLessThan
                    => currentProcess.AddressArbiter.WaitForAddressIfLessThan(address, value, false, timeout),
                ArbitrationType.DecrementAndWaitIfLessThan
                    => currentProcess.AddressArbiter.WaitForAddressIfLessThan(address, value, true, timeout),
                ArbitrationType.WaitIfEqual
                    => currentProcess.AddressArbiter.WaitForAddressIfEqual(address, value, timeout),
                _ => KernelResult.InvalidEnumValue,
            };
        }

        [Svc(0x35)]
        public Result SignalToAddress([PointerSized] ulong address, SignalType type, int value, int count)
        {
            if (IsPointingInsideKernel(address))
            {
                return KernelResult.InvalidMemState;
            }

            if (IsAddressNotWordAligned(address))
            {
                return KernelResult.InvalidAddress;
            }

            KProcess currentProcess = KernelStatic.GetCurrentProcess();

            return type switch
            {
                SignalType.Signal
                    => currentProcess.AddressArbiter.Signal(address, count),
                SignalType.SignalAndIncrementIfEqual
                    => currentProcess.AddressArbiter.SignalAndIncrementIfEqual(address, value, count),
                SignalType.SignalAndModifyIfEqual
                    => currentProcess.AddressArbiter.SignalAndModifyIfEqual(address, value, count),
                _ => KernelResult.InvalidEnumValue,
            };
        }

        [Svc(0x36)]
        public Result SynchronizePreemptionState()
        {
            KernelStatic.GetCurrentThread().SynchronizePreemptionState();

            return Result.Success;
        }

        // Not actual syscalls, used by HLE services and such.

        public IExternalEvent GetExternalEvent(int handle)
        {
            KWritableEvent writableEvent = KernelStatic.GetCurrentProcess().HandleTable.GetObject<KWritableEvent>(handle);

            if (writableEvent == null)
            {
                return null;
            }

            return new ExternalEvent(writableEvent);
        }

        public IVirtualMemoryManager GetMemoryManagerByProcessHandle(int handle)
        {
            return KernelStatic.GetCurrentProcess().HandleTable.GetKProcess(handle).CpuMemory;
        }

        public ulong GetTransferMemoryAddress(int handle)
        {
            KTransferMemory transferMemory = KernelStatic.GetCurrentProcess().HandleTable.GetObject<KTransferMemory>(handle);

            if (transferMemory == null)
            {
                return 0;
            }

            return transferMemory.Address;
        }

        private static bool IsPointingInsideKernel(ulong address)
        {
            return (address + 0x1000000000) < 0xffffff000;
        }

        private static bool IsAddressNotWordAligned(ulong address)
        {
            return (address & 3) != 0;
        }
    }
}
