using Ryujinx.Common;
using Ryujinx.Common.Logging;
using Ryujinx.Cpu;
using Ryujinx.HLE.Exceptions;
using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.HOS.Services.Nv.NvDrvServices;
using Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostAsGpu;
using Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostChannel;
using Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostCtrl;
using Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostCtrlGpu;
using Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostDbgGpu;
using Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostProfGpu;
using Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvMap;
using Ryujinx.HLE.HOS.Services.Nv.Types;
using Ryujinx.Memory;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Ryujinx.HLE.HOS.Services.Nv
{
    [Service("nvdrv")]
    [Service("nvdrv:a")]
    [Service("nvdrv:s")]
    [Service("nvdrv:t")]
    class INvDrvServices : IpcService
    {
        private static readonly List<string> _deviceFileDebugRegistry =
        [
            "/dev/nvhost-dbg-gpu",
            "/dev/nvhost-prof-gpu"
        ];

        private static readonly Dictionary<string, Type> _deviceFileRegistry = new()
        {
            { "/dev/nvmap",           typeof(NvMapDeviceFile)         },
            { "/dev/nvhost-ctrl",     typeof(NvHostCtrlDeviceFile)    },
            { "/dev/nvhost-ctrl-gpu", typeof(NvHostCtrlGpuDeviceFile) },
            { "/dev/nvhost-as-gpu",   typeof(NvHostAsGpuDeviceFile)   },
            { "/dev/nvhost-gpu",      typeof(NvHostGpuDeviceFile)     },
            //{ "/dev/nvhost-msenc",    typeof(NvHostChannelDeviceFile) },
            { "/dev/nvhost-nvdec",    typeof(NvHostChannelDeviceFile) },
            //{ "/dev/nvhost-nvjpg",    typeof(NvHostChannelDeviceFile) },
            { "/dev/nvhost-vic",      typeof(NvHostChannelDeviceFile) },
            //{ "/dev/nvhost-display",  typeof(NvHostChannelDeviceFile) },
            { "/dev/nvhost-dbg-gpu",  typeof(NvHostDbgGpuDeviceFile)  },
            { "/dev/nvhost-prof-gpu", typeof(NvHostProfGpuDeviceFile) },
        };

        public static IdDictionary DeviceFileIdRegistry = new();

        private IVirtualMemoryManager _clientMemory;
        private ulong _owner;

        private bool _transferMemInitialized = false;

        // TODO: This should call set:sys::GetDebugModeFlag
        private readonly bool _debugModeEnabled = false;

        public INvDrvServices(ServiceCtx context) : base(context.Device.System.NvDrvServer)
        {
            _owner = 0;
        }

        private NvResult Open(ServiceCtx context, string path, out int fd)
        {
            fd = -1;

            if (!_debugModeEnabled && _deviceFileDebugRegistry.Contains(path))
            {
                return NvResult.NotSupported;
            }

            if (_deviceFileRegistry.TryGetValue(path, out Type deviceFileClass))
            {
                ConstructorInfo constructor = deviceFileClass.GetConstructor([typeof(ServiceCtx), typeof(IVirtualMemoryManager), typeof(ulong)
                ]);

                NvDeviceFile deviceFile = (NvDeviceFile)constructor.Invoke([context, _clientMemory, _owner]);

                deviceFile.Path = path;

                fd = DeviceFileIdRegistry.Add(deviceFile);

                return NvResult.Success;
            }

            Logger.Warning?.Print(LogClass.ServiceNv, $"Cannot find file device \"{path}\"!");

            return NvResult.FileOperationFailed;
        }

        private NvResult GetIoctlArgument(ServiceCtx context, NvIoctl ioctlCommand, out Span<byte> arguments)
        {
            (ulong inputDataPosition, ulong inputDataSize) = context.Request.GetBufferType0x21(0);
#pragma warning disable IDE0059 // Remove unnecessary value assignment
            (ulong outputDataPosition, ulong outputDataSize) = context.Request.GetBufferType0x22(0);
#pragma warning restore IDE0059

            NvIoctl.Direction ioctlDirection = ioctlCommand.DirectionValue;
            uint ioctlSize = ioctlCommand.Size;

            bool isRead = (ioctlDirection & NvIoctl.Direction.Read) != 0;
            bool isWrite = (ioctlDirection & NvIoctl.Direction.Write) != 0;

            if ((isWrite && ioctlSize > outputDataSize) || (isRead && ioctlSize > inputDataSize))
            {
                arguments = null;

                Logger.Warning?.Print(LogClass.ServiceNv, "Ioctl size inconsistency found!");

                return NvResult.InvalidSize;
            }

            if (isRead && isWrite)
            {
                if (outputDataSize < inputDataSize)
                {
                    arguments = null;

                    Logger.Warning?.Print(LogClass.ServiceNv, "Ioctl size inconsistency found!");

                    return NvResult.InvalidSize;
                }

                byte[] outputData = new byte[outputDataSize];

                byte[] temp = new byte[inputDataSize];

                context.Memory.Read(inputDataPosition, temp);

                Buffer.BlockCopy(temp, 0, outputData, 0, temp.Length);

                arguments = new Span<byte>(outputData);
            }
            else if (isWrite)
            {
                byte[] outputData = new byte[outputDataSize];

                arguments = new Span<byte>(outputData);
            }
            else
            {
                byte[] temp = new byte[inputDataSize];

                context.Memory.Read(inputDataPosition, temp);

                arguments = new Span<byte>(temp);
            }

            return NvResult.Success;
        }

        private NvResult GetDeviceFileFromFd(int fd, out NvDeviceFile deviceFile)
        {
            deviceFile = null;

            if (fd < 0)
            {
                return NvResult.InvalidParameter;
            }

            deviceFile = DeviceFileIdRegistry.GetData<NvDeviceFile>(fd);

            if (deviceFile == null)
            {
                Logger.Warning?.Print(LogClass.ServiceNv, $"Invalid file descriptor {fd}");

                return NvResult.NotImplemented;
            }

            if (deviceFile.Owner != _owner)
            {
                return NvResult.AccessDenied;
            }

            return NvResult.Success;
        }

        private NvResult EnsureInitialized()
        {
            if (_owner == 0)
            {
                Logger.Warning?.Print(LogClass.ServiceNv, "INvDrvServices is not initialized!");

                return NvResult.NotInitialized;
            }

            return NvResult.Success;
        }

        private NvResult ConvertInternalErrorCode(NvInternalResult errorCode)
        {
            return errorCode switch
            {
                NvInternalResult.Success => NvResult.Success,
                NvInternalResult.Unknown0x72 => NvResult.AlreadyAllocated,
                NvInternalResult.TimedOut or NvInternalResult.TryAgain or NvInternalResult.Interrupted => NvResult.Timeout,
                NvInternalResult.InvalidAddress => NvResult.InvalidAddress,
                NvInternalResult.NotSupported or NvInternalResult.Unknown0x18 => NvResult.NotSupported,
                NvInternalResult.InvalidState => NvResult.InvalidState,
                NvInternalResult.ReadOnlyAttribute => NvResult.ReadOnlyAttribute,
                NvInternalResult.NoSpaceLeft or NvInternalResult.FileTooBig => NvResult.InvalidSize,
                NvInternalResult.FileTableOverflow or NvInternalResult.BadFileNumber => NvResult.FileOperationFailed,
                NvInternalResult.InvalidInput => NvResult.InvalidValue,
                NvInternalResult.NotADirectory => NvResult.DirectoryOperationFailed,
                NvInternalResult.Busy => NvResult.Busy,
                NvInternalResult.BadAddress => NvResult.InvalidAddress,
                NvInternalResult.AccessDenied or NvInternalResult.OperationNotPermitted => NvResult.AccessDenied,
                NvInternalResult.OutOfMemory => NvResult.InsufficientMemory,
                NvInternalResult.DeviceNotFound => NvResult.ModuleNotPresent,
                NvInternalResult.IoError => NvResult.ResourceError,
                _ => NvResult.IoctlFailed,
            };
        }

        [CommandCmif(0)]
        // Open(buffer<bytes, 5> path) -> (s32 fd, u32 error_code)
        public ResultCode Open(ServiceCtx context)
        {
            NvResult errorCode = EnsureInitialized();
            int fd = -1;

            if (errorCode == NvResult.Success)
            {
                ulong pathPtr = context.Request.SendBuff[0].Position;
                ulong pathSize = context.Request.SendBuff[0].Size;

                string path = MemoryHelper.ReadAsciiString(context.Memory, pathPtr, (long)pathSize);

                errorCode = Open(context, path, out fd);
            }

            context.ResponseData.Write(fd);
            context.ResponseData.Write((uint)errorCode);

            return ResultCode.Success;
        }

        [CommandCmif(1)]
        // Ioctl(s32 fd, u32 ioctl_cmd, buffer<bytes, 0x21> in_args) -> (u32 error_code, buffer<bytes, 0x22> out_args)
        public ResultCode Ioctl(ServiceCtx context)
        {
            NvResult errorCode = EnsureInitialized();

            if (errorCode == NvResult.Success)
            {
                int fd = context.RequestData.ReadInt32();
                NvIoctl ioctlCommand = context.RequestData.ReadStruct<NvIoctl>();

                errorCode = GetIoctlArgument(context, ioctlCommand, out Span<byte> arguments);

                if (errorCode == NvResult.Success)
                {
                    errorCode = GetDeviceFileFromFd(fd, out NvDeviceFile deviceFile);

                    if (errorCode == NvResult.Success)
                    {
                        NvInternalResult internalResult = deviceFile.Ioctl(ioctlCommand, arguments);

                        if (internalResult == NvInternalResult.NotImplemented)
                        {
                            throw new NvIoctlNotImplementedException(context, deviceFile, ioctlCommand);
                        }

                        errorCode = ConvertInternalErrorCode(internalResult);

                        if ((ioctlCommand.DirectionValue & NvIoctl.Direction.Write) != 0)
                        {
                            context.Memory.Write(context.Request.GetBufferType0x22(0).Position, arguments.ToArray());
                        }
                    }
                }
            }

            context.ResponseData.Write((uint)errorCode);

            return ResultCode.Success;
        }

        [CommandCmif(2)]
        // Close(s32 fd) -> u32 error_code
        public ResultCode Close(ServiceCtx context)
        {
            NvResult errorCode = EnsureInitialized();

            if (errorCode == NvResult.Success)
            {
                int fd = context.RequestData.ReadInt32();

                errorCode = GetDeviceFileFromFd(fd, out NvDeviceFile deviceFile);

                if (errorCode == NvResult.Success)
                {
                    deviceFile.Close();

                    DeviceFileIdRegistry.Delete(fd);
                }
            }

            context.ResponseData.Write((uint)errorCode);

            return ResultCode.Success;
        }

        [CommandCmif(3)]
        // Initialize(u32 transfer_memory_size, handle<copy, process> current_process, handle<copy, transfer_memory> transfer_memory) -> u32 error_code
        public ResultCode Initialize(ServiceCtx context)
        {
#pragma warning disable IDE0059 // Remove unnecessary value assignment
            long transferMemSize = context.RequestData.ReadInt64();
#pragma warning restore IDE0059
            int transferMemHandle = context.Request.HandleDesc.ToCopy[1];

            // TODO: When transfer memory will be implemented, this could be removed.
            _transferMemInitialized = true;

            int clientHandle = context.Request.HandleDesc.ToCopy[0];

            _clientMemory = context.Process.HandleTable.GetKProcess(clientHandle).CpuMemory;

            context.Device.System.KernelContext.Syscall.GetProcessId(out _owner, clientHandle);

            context.ResponseData.Write((uint)NvResult.Success);

            // Close the process and transfer memory handles immediately as we don't use them.
            context.Device.System.KernelContext.Syscall.CloseHandle(clientHandle);
            context.Device.System.KernelContext.Syscall.CloseHandle(transferMemHandle);

            return ResultCode.Success;
        }

        [CommandCmif(4)]
        // QueryEvent(s32 fd, u32 event_id) -> (u32, handle<copy, event>)
        public ResultCode QueryEvent(ServiceCtx context)
        {
            NvResult errorCode = EnsureInitialized();

            if (errorCode == NvResult.Success)
            {
                int fd = context.RequestData.ReadInt32();
                uint eventId = context.RequestData.ReadUInt32();

                errorCode = GetDeviceFileFromFd(fd, out NvDeviceFile deviceFile);

                if (errorCode == NvResult.Success)
                {
                    NvInternalResult internalResult = deviceFile.QueryEvent(out int eventHandle, eventId);

                    if (internalResult == NvInternalResult.NotImplemented)
                    {
                        throw new NvQueryEventNotImplementedException(context, deviceFile, eventId);
                    }

                    errorCode = ConvertInternalErrorCode(internalResult);

                    if (errorCode == NvResult.Success)
                    {
                        context.Response.HandleDesc = IpcHandleDesc.MakeCopy(eventHandle);
                    }
                }
            }

            context.ResponseData.Write((uint)errorCode);

            return ResultCode.Success;
        }

        [CommandCmif(5)]
        // MapSharedMemory(s32 fd, u32 argument, handle<copy, shared_memory>) -> u32 error_code
        public ResultCode MapSharedMemory(ServiceCtx context)
        {
            NvResult errorCode = EnsureInitialized();

            if (errorCode == NvResult.Success)
            {
                int fd = context.RequestData.ReadInt32();
                uint argument = context.RequestData.ReadUInt32();
                int sharedMemoryHandle = context.Request.HandleDesc.ToCopy[0];

                errorCode = GetDeviceFileFromFd(fd, out NvDeviceFile deviceFile);

                if (errorCode == NvResult.Success)
                {
                    errorCode = ConvertInternalErrorCode(deviceFile.MapSharedMemory(sharedMemoryHandle, argument));
                }
            }

            context.ResponseData.Write((uint)errorCode);

            return ResultCode.Success;
        }

        [CommandCmif(6)]
        // GetStatus() -> (unknown<0x20>, u32 error_code)
        public ResultCode GetStatus(ServiceCtx context)
        {
            // TODO: When transfer memory will be implemented, check if it's mapped instead.
            if (_transferMemInitialized)
            {
                // TODO: Populate values when more RE will be done.
                NvStatus nvStatus = new()
                {
                    MemoryValue1 = 0, // GetMemStats(transfer_memory + 0x60, 3)
                    MemoryValue2 = 0, // GetMemStats(transfer_memory + 0x60, 5)
                    MemoryValue3 = 0, // transfer_memory + 0x78
                    MemoryValue4 = 0, // transfer_memory + 0x80
                };

                context.ResponseData.WriteStruct(nvStatus);
                context.ResponseData.Write((uint)NvResult.Success);

                Logger.Stub?.PrintStub(LogClass.ServiceNv);
            }
            else
            {
                context.ResponseData.Write((uint)NvResult.NotInitialized);
            }

            return ResultCode.Success;
        }

        [CommandCmif(7)]
        // ForceSetClientPid(u64) -> u32 error_code
        public ResultCode ForceSetClientPid(ServiceCtx context)
        {
            throw new ServiceNotImplementedException(this, context);
        }

        [CommandCmif(8)]
        // SetClientPID(u64, pid) -> u32 error_code
        public ResultCode SetClientPid(ServiceCtx context)
        {
#pragma warning disable IDE0059 // Remove unnecessary value assignment
            long pid = context.RequestData.ReadInt64();
#pragma warning restore IDE0059

            context.ResponseData.Write(0);

            return ResultCode.Success;
        }

        [CommandCmif(9)]
        // DumpGraphicsMemoryInfo()
        public ResultCode DumpGraphicsMemoryInfo(ServiceCtx context)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceNv);

            return ResultCode.Success;
        }

        [CommandCmif(10)] // 3.0.0+
        // InitializeDevtools(u32, handle<copy>) -> u32 error_code;
        public ResultCode InitializeDevtools(ServiceCtx context)
        {
            throw new ServiceNotImplementedException(this, context);
        }

        [CommandCmif(11)] // 3.0.0+
        // Ioctl2(s32 fd, u32 ioctl_cmd, buffer<bytes, 0x21> in_args, buffer<bytes, 0x21> inline_in_buffer) -> (u32 error_code, buffer<bytes, 0x22> out_args)
        public ResultCode Ioctl2(ServiceCtx context)
        {
            NvResult errorCode = EnsureInitialized();

            if (errorCode == NvResult.Success)
            {
                int fd = context.RequestData.ReadInt32();
                NvIoctl ioctlCommand = context.RequestData.ReadStruct<NvIoctl>();

                (ulong inlineInBufferPosition, ulong inlineInBufferSize) = context.Request.GetBufferType0x21(1);

                errorCode = GetIoctlArgument(context, ioctlCommand, out Span<byte> arguments);

                byte[] temp = new byte[inlineInBufferSize];

                context.Memory.Read(inlineInBufferPosition, temp);

                Span<byte> inlineInBuffer = new(temp);

                if (errorCode == NvResult.Success)
                {
                    errorCode = GetDeviceFileFromFd(fd, out NvDeviceFile deviceFile);

                    if (errorCode == NvResult.Success)
                    {
                        NvInternalResult internalResult = deviceFile.Ioctl2(ioctlCommand, arguments, inlineInBuffer);

                        if (internalResult == NvInternalResult.NotImplemented)
                        {
                            throw new NvIoctlNotImplementedException(context, deviceFile, ioctlCommand);
                        }

                        errorCode = ConvertInternalErrorCode(internalResult);

                        if ((ioctlCommand.DirectionValue & NvIoctl.Direction.Write) != 0)
                        {
                            context.Memory.Write(context.Request.GetBufferType0x22(0).Position, arguments.ToArray());
                        }
                    }
                }
            }

            context.ResponseData.Write((uint)errorCode);

            return ResultCode.Success;
        }

        [CommandCmif(12)] // 3.0.0+
        // Ioctl3(s32 fd, u32 ioctl_cmd, buffer<bytes, 0x21> in_args) -> (u32 error_code, buffer<bytes, 0x22> out_args,  buffer<bytes, 0x22> inline_out_buffer)
        public ResultCode Ioctl3(ServiceCtx context)
        {
            NvResult errorCode = EnsureInitialized();

            if (errorCode == NvResult.Success)
            {
                int fd = context.RequestData.ReadInt32();
                NvIoctl ioctlCommand = context.RequestData.ReadStruct<NvIoctl>();

                (ulong inlineOutBufferPosition, ulong inlineOutBufferSize) = context.Request.GetBufferType0x22(1);

                errorCode = GetIoctlArgument(context, ioctlCommand, out Span<byte> arguments);

                byte[] temp = new byte[inlineOutBufferSize];

                context.Memory.Read(inlineOutBufferPosition, temp);

                Span<byte> inlineOutBuffer = new(temp);

                if (errorCode == NvResult.Success)
                {
                    errorCode = GetDeviceFileFromFd(fd, out NvDeviceFile deviceFile);

                    if (errorCode == NvResult.Success)
                    {
                        NvInternalResult internalResult = deviceFile.Ioctl3(ioctlCommand, arguments, inlineOutBuffer);

                        if (internalResult == NvInternalResult.NotImplemented)
                        {
                            throw new NvIoctlNotImplementedException(context, deviceFile, ioctlCommand);
                        }

                        errorCode = ConvertInternalErrorCode(internalResult);

                        if ((ioctlCommand.DirectionValue & NvIoctl.Direction.Write) != 0)
                        {
                            context.Memory.Write(context.Request.GetBufferType0x22(0).Position, arguments.ToArray());
                            context.Memory.Write(inlineOutBufferPosition, inlineOutBuffer.ToArray());
                        }
                    }
                }
            }

            context.ResponseData.Write((uint)errorCode);

            return ResultCode.Success;
        }

        [CommandCmif(13)] // 3.0.0+
        // FinishInitialize(unknown<8>)
        public ResultCode FinishInitialize(ServiceCtx context)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceNv);

            return ResultCode.Success;
        }

        public static void Destroy()
        {
            NvHostChannelDeviceFile.Destroy();

            foreach (object entry in DeviceFileIdRegistry.Values)
            {
                NvDeviceFile deviceFile = (NvDeviceFile)entry;

                deviceFile.Close();
            }

            DeviceFileIdRegistry.Clear();
        }
    }
}
