using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.HOS.Kernel.Threading;
using Ryujinx.Horizon.Common;
using System;

namespace Ryujinx.HLE.HOS.Services.Ldn.Lp2p
{
    class ISfServiceMonitor : IpcService
    {
        private readonly KEvent _stateChangeEvent;
        private readonly KEvent _jointEvent;
        private int _stateChangeEventHandle = 0;
        private int _jointEventHandle = 0;

        public ISfServiceMonitor(ServiceCtx context)
        {
            _stateChangeEvent = new KEvent(context.Device.System.KernelContext);
            _jointEvent = new KEvent(context.Device.System.KernelContext);
        }

        [CommandCmif(0)]
        // Initialize()
        public ResultCode Initialize(ServiceCtx context)
        {
            context.ResponseData.Write(0);

            return ResultCode.Success;
        }

        [CommandCmif(256)]
        // AttachNetworkInterfaceStateChangeEvent() -> handle<copy>
        public ResultCode AttachNetworkInterfaceStateChangeEvent(ServiceCtx context)
        {
            if (context.Process.HandleTable.GenerateHandle(_stateChangeEvent.ReadableEvent, out _stateChangeEventHandle) != Result.Success)
            {
                throw new InvalidOperationException("Out of handles!");
            }

            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(_stateChangeEventHandle);

            return ResultCode.Success;
        }

        [CommandCmif(288)]
        // GetGroupInfo(buffer<nn::lp2p::GroupInfo, 0x32>)
        public ResultCode GetGroupInfo(ServiceCtx context)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceLdn);

            return ResultCode.Success;
        }

        [CommandCmif(296)]
        // GetGroupInfo2(buffer<nn::lp2p::GroupInfo, 0x32>, buffer<nn::lp2p::GroupInfo, 0x31>)
        public ResultCode GetGroupInfo2(ServiceCtx context)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceLdn);

            return ResultCode.Success;
        }

        [CommandCmif(312)]
        // GetIpConfig(buffer<unknown<0x100>, 0x1a>)
        public ResultCode GetIpConfig(ServiceCtx context)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceLdn);

            return ResultCode.Success;
        }

        [CommandCmif(328)]
        // AttachNetworkInterfaceStateChangeEvent() -> handle<copy>
        public ResultCode AttachJoinEvent(ServiceCtx context)
        {
            if (context.Process.HandleTable.GenerateHandle(_jointEvent.ReadableEvent, out _jointEventHandle) != Result.Success)
            {
                throw new InvalidOperationException("Out of handles!");
            }

            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(_jointEventHandle);

            return ResultCode.Success;
        }
    }
}
