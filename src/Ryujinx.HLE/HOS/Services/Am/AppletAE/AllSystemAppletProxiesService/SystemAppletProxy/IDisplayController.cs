using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.HOS.Kernel.Memory;
using Ryujinx.Horizon.Common;
using System;

namespace Ryujinx.HLE.HOS.Services.Am.AppletAE.AllSystemAppletProxiesService.SystemAppletProxy
{
    class IDisplayController : IpcService
    {
        private readonly KTransferMemory _transferMem;
        private bool _lastApplicationCaptureBufferAcquired;
        private bool _callerAppletCaptureBufferAcquired;

        public IDisplayController(ServiceCtx context)
        {
            _transferMem = context.Device.System.AppletCaptureBufferTransfer;
        }

        [CommandCmif(8)] // 2.0.0+
        // TakeScreenShotOfOwnLayer(b8, s32)
        public ResultCode TakeScreenShotOfOwnLayer(ServiceCtx context)
        {
            bool unknown1 = context.RequestData.ReadBoolean();
            int unknown2 = context.RequestData.ReadInt32();

            Logger.Stub?.PrintStub(LogClass.ServiceAm, new { unknown1, unknown2 });

            return ResultCode.Success;
        }

        [CommandCmif(11)]
        // ReleaseLastApplicationCaptureBuffer()
        public ResultCode ReleaseLastApplicationCaptureBuffer(ServiceCtx context)
        {
            if (!_lastApplicationCaptureBufferAcquired)
            {
                return ResultCode.BufferNotAcquired;
            }

            _lastApplicationCaptureBufferAcquired = false;

            return ResultCode.Success;
        }

        [CommandCmif(15)]
        // ReleaseCallerAppletCaptureBuffer()
        public ResultCode ReleaseCallerAppletCaptureBuffer(ServiceCtx context)
        {
            if (!_callerAppletCaptureBufferAcquired)
            {
                return ResultCode.BufferNotAcquired;
            }

            _callerAppletCaptureBufferAcquired = false;

            return ResultCode.Success;
        }

        [CommandCmif(16)]
        // AcquireLastApplicationCaptureBufferEx() -> (b8, handle<copy>)
        public ResultCode AcquireLastApplicationCaptureBufferEx(ServiceCtx context)
        {
            if (_lastApplicationCaptureBufferAcquired)
            {
                return ResultCode.BufferAlreadyAcquired;
            }

            if (context.Process.HandleTable.GenerateHandle(_transferMem, out int handle) != Result.Success)
            {
                throw new InvalidOperationException("Out of handles!");
            }

            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(handle);

            _lastApplicationCaptureBufferAcquired = true;

            context.ResponseData.Write(_lastApplicationCaptureBufferAcquired);

            return ResultCode.Success;
        }

        [CommandCmif(18)]
        // AcquireCallerAppletCaptureBufferEx() -> (b8, handle<copy>)
        public ResultCode AcquireCallerAppletCaptureBufferEx(ServiceCtx context)
        {
            if (_callerAppletCaptureBufferAcquired)
            {
                return ResultCode.BufferAlreadyAcquired;
            }

            if (context.Process.HandleTable.GenerateHandle(_transferMem, out int handle) != Result.Success)
            {
                throw new InvalidOperationException("Out of handles!");
            }

            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(handle);

            _callerAppletCaptureBufferAcquired = true;

            context.ResponseData.Write(_callerAppletCaptureBufferAcquired);

            return ResultCode.Success;
        }
        
        [CommandCmif(22)]
        // AcquireLastApplicationCaptureSharedBuffer() -> (b8, u32)
        public ResultCode AcquireLastApplicationCaptureSharedBuffer(ServiceCtx context)
        {
            context.ResponseData.Write(1);
            context.ResponseData.Write(context.Device.System.ViServerS.GetApplicationLastPresentedFrameHandle(context.Device.Gpu));

            return ResultCode.Success;
        }

        [CommandCmif(26)]
        // AcquireCallerAppletCaptureSharedBuffer() -> (b8, u32)
        public ResultCode AcquireCallerAppletCaptureSharedBuffer(ServiceCtx context)
        {
            // TODO: How does the handling for applets differ from the one for applications?
            context.ResponseData.Write(1);
            context.ResponseData.Write(context.Device.System.ViServerS.GetApplicationLastPresentedFrameHandle(context.Device.Gpu));

            return ResultCode.Success;
        }
    }
}
