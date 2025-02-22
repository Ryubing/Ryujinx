using Ryujinx.Common;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.HOS.Kernel.Threading;
using Ryujinx.HLE.HOS.Services.SurfaceFlinger;
using Ryujinx.HLE.HOS.Services.Vi.RootService.ApplicationDisplayService.Types.Fbshare;
using Ryujinx.Horizon.Common;
using System;

namespace Ryujinx.HLE.HOS.Services.Vi.RootService.ApplicationDisplayService
{
    class ISystemDisplayService : IpcService
    {
#pragma warning disable IDE0052 // Remove unread private member
        private readonly IApplicationDisplayService _applicationDisplayService;
#pragma warning restore IDE0052
        
        private KEvent _sharedFramebufferAcquirableEvent;
        private int _sharedFramebufferAcquirableEventHandle;

        public ISystemDisplayService(IApplicationDisplayService applicationDisplayService)
        {
            _applicationDisplayService = applicationDisplayService;
        }

        [CommandCmif(2205)]
        // SetLayerZ(u64, u64)
        public ResultCode SetLayerZ(ServiceCtx context)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceVi);

            return ResultCode.Success;
        }

        [CommandCmif(2207)]
        // SetLayerVisibility(b8, u64)
        public ResultCode SetLayerVisibility(ServiceCtx context)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceVi);

            return ResultCode.Success;
        }

        [CommandCmif(2312)] // 1.0.0-6.2.0
        // CreateStrayLayer(u32, u64) -> (u64, u64, buffer<bytes, 6>)
        public ResultCode CreateStrayLayer(ServiceCtx context)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceVi);

            return _applicationDisplayService.CreateStrayLayer(context);
        }

        [CommandCmif(3200)]
        // GetDisplayMode(u64) -> nn::vi::DisplayModeInfo
        public ResultCode GetDisplayMode(ServiceCtx context)
        {
            ulong displayId = context.RequestData.ReadUInt64();

            (ulong width, ulong height) = AndroidSurfaceComposerClient.GetDisplayInfo(context, displayId);

            context.ResponseData.Write((uint)width);
            context.ResponseData.Write((uint)height);
            context.ResponseData.Write(60.0f);
            context.ResponseData.Write(0);

            Logger.Stub?.PrintStub(LogClass.ServiceVi);

            return ResultCode.Success;
        }
        
        [CommandCmif(8225)] // 4.0.0+
        // GetSharedBufferMemoryHandleId()
        public ResultCode GetSharedBufferMemoryHandleId(ServiceCtx context)
        {
            context.ResponseData.Write((ulong)context.Device.System.ViServerS.GetSharedBufferNvMapId());
            context.ResponseData.Write(context.Device.System.ViServerS.GetSharedBufferSize());

            (ulong mapAddress, ulong mapSize) = context.Request.GetBufferType0x22();

            context.Memory.Write(mapAddress, context.Device.System.ViServerS.GetSharedBufferMap());

            return ResultCode.Success;
        }

        [CommandCmif(8250)] // 4.0.0+
        // OpenSharedLayer(nn::vi::fbshare::SharedLayerHandle, nn::applet::AppletResourceUserId, pid)
        public ResultCode OpenSharedLayer(ServiceCtx context)
        {
            long sharedLayerHandle = context.RequestData.ReadInt64();
            long appletResourceUserId = context.RequestData.ReadInt64();

            context.Device.System.ViServerS.OpenSharedLayer(sharedLayerHandle);

            return ResultCode.Success;
        }

        [CommandCmif(8251)] // 4.0.0+
        // CloseSharedLayer(nn::vi::fbshare::SharedLayerHandle)
        public ResultCode CloseSharedLayer(ServiceCtx context)
        {
            long sharedLayerHandle = context.RequestData.ReadInt64();

            context.Device.System.ViServerS.CloseSharedLayer(sharedLayerHandle);

            return ResultCode.Success;
        }

        [CommandCmif(8252)] // 4.0.0+
        // ConnectSharedLayer(nn::vi::fbshare::SharedLayerHandle)
        public ResultCode ConnectSharedLayer(ServiceCtx context)
        {
            long sharedLayerHandle = context.RequestData.ReadInt64();

            context.Device.System.ViServerS.ConnectSharedLayer(sharedLayerHandle);

            return ResultCode.Success;
        }

        [CommandCmif(8253)] // 4.0.0+
        // DisconnectSharedLayer(nn::vi::fbshare::SharedLayerHandle)
        public ResultCode DisconnectSharedLayer(ServiceCtx context)
        {
            long sharedLayerHandle = context.RequestData.ReadInt64();

            context.Device.System.ViServerS.DisconnectSharedLayer(sharedLayerHandle);

            return ResultCode.Success;
        }

        [CommandCmif(8254)] // 4.0.0+
        // AcquireSharedFrameBuffer(nn::vi::fbshare::SharedLayerHandle) -> (nn::vi::native::NativeSync, nn::vi::fbshare::SharedLayerTextureIndexList, u64)
        public ResultCode AcquireSharedFrameBuffer(ServiceCtx context)
        {
            long sharedLayerHandle = context.RequestData.ReadInt64();

            int slot = context.Device.System.ViServerS.DequeueFrameBuffer(sharedLayerHandle, out AndroidFence fence);

            var indexList = new SharedLayerTextureIndexList();

            for (int i = 0; i < indexList.Indices.Length; i++)
            {
                indexList.Indices[i] = context.Device.System.ViServerS.GetFrameBufferMapIndex(i);
            }

            context.ResponseData.WriteStruct(fence);
            context.ResponseData.WriteStruct(indexList);
            context.ResponseData.Write(0); // Padding
            context.ResponseData.Write((ulong)slot);

            return ResultCode.Success;
        }

        [CommandCmif(8255)] // 4.0.0+
        // PresentSharedFrameBuffer(nn::vi::native::NativeSync, nn::vi::CropRegion, u32, u32, nn::vi::fbshare::SharedLayerHandle, u64)
        public ResultCode PresentSharedFrameBuffer(ServiceCtx context)
        {
            AndroidFence nativeSync = context.RequestData.ReadStruct<AndroidFence>();
            Rect cropRegion = context.RequestData.ReadStruct<Rect>();

            NativeWindowTransform transform = (NativeWindowTransform)context.RequestData.ReadUInt32();
            int swapInterval = context.RequestData.ReadInt32();
            int padding = context.RequestData.ReadInt32();

            long sharedLayerHandle = context.RequestData.ReadInt64();
            ulong slot = context.RequestData.ReadUInt64();

            context.Device.System.ViServerS.QueueFrameBuffer(sharedLayerHandle, (int)slot, cropRegion, transform, swapInterval, nativeSync);

            return ResultCode.Success;
        }

        [CommandCmif(8256)] // 4.0.0+
        // GetSharedFrameBufferAcquirableEvent(nn::vi::fbshare::SharedLayerHandle) -> handle<copy>
        public ResultCode GetSharedFrameBufferAcquirableEvent(ServiceCtx context)
        {
            if (_sharedFramebufferAcquirableEventHandle == 0)
            {
                _sharedFramebufferAcquirableEvent = new KEvent(context.Device.System.KernelContext);
                _sharedFramebufferAcquirableEvent.WritableEvent.Signal();

                if (context.Process.HandleTable.GenerateHandle(_sharedFramebufferAcquirableEvent.ReadableEvent, out _sharedFramebufferAcquirableEventHandle) != Result.Success)
                {
                    throw new InvalidOperationException("Out of handles!");
                }
            }

            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(_sharedFramebufferAcquirableEventHandle);

            return ResultCode.Success;
        }

        [CommandCmif(8258)] // 5.0.0+
        // CancelSharedFrameBuffer(nn::vi::fbshare::SharedLayerHandle, u64)
        public ResultCode CancelSharedFrameBuffer(ServiceCtx context)
        {
            long sharedLayerHandle = context.RequestData.ReadInt64();
            ulong slot = context.RequestData.ReadUInt64();

            context.Device.System.ViServerS.CancelFrameBuffer(sharedLayerHandle, (int)slot);

            return ResultCode.Success;
        }
    }
}
