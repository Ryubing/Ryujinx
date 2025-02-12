using Ryujinx.Common.Logging;

namespace Ryujinx.HLE.HOS.Services.Vi.RootService.ApplicationDisplayService
{
    class IManagerDisplayService : IpcService
    {
#pragma warning disable IDE0052 // Remove unread private member
        private readonly IApplicationDisplayService _applicationDisplayService;
#pragma warning restore IDE0052

        public IManagerDisplayService(IApplicationDisplayService applicationDisplayService)
        {
            _applicationDisplayService = applicationDisplayService;
        }

        [CommandCmif(1102)]
        // GetDisplayResolution(u64 display_id) -> (u64 width, u64 height)
        public ResultCode GetDisplayResolution(ServiceCtx context)
        {
            ulong displayId = context.RequestData.ReadUInt64();

            (ulong width, ulong height) = AndroidSurfaceComposerClient.GetDisplayInfo(context, displayId);

            context.ResponseData.Write(width);
            context.ResponseData.Write(height);

            return ResultCode.Success;
        }

        [CommandCmif(2010)]
        // CreateManagedLayer(u32, u64, nn::applet::AppletResourceUserId) -> u64
        public ResultCode CreateManagedLayer(ServiceCtx context)
        {
#pragma warning disable IDE0059 // Remove unnecessary value assignment
            long layerFlags = context.RequestData.ReadInt64();
            long displayId = context.RequestData.ReadInt64();
#pragma warning restore IDE0059
            long appletResourceUserId = context.RequestData.ReadInt64();

            ulong pid = context.Device.System.AppletState.AppletResourceUserIds.GetData<ulong>((int)appletResourceUserId);

            context.Device.System.SurfaceFlinger.CreateLayer(out long layerId, pid);

            context.ResponseData.Write(layerId);

            return ResultCode.Success;
        }

        [CommandCmif(2011)]
        // DestroyManagedLayer(u64)
        public ResultCode DestroyManagedLayer(ServiceCtx context)
        {
            long layerId = context.RequestData.ReadInt64();

            return context.Device.System.SurfaceFlinger.DestroyManagedLayer(layerId);
        }

        [CommandCmif(2012)] // 7.0.0+
        // CreateStrayLayer(u32, u64) -> (u64, u64, buffer<bytes, 6>)
        public ResultCode CreateStrayLayer(ServiceCtx context)
        {
            return _applicationDisplayService.CreateStrayLayer(context);
        }

        [CommandCmif(6000)]
        // AddToLayerStack(u32, u64)
        public ResultCode AddToLayerStack(ServiceCtx context)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceVi);

            return ResultCode.Success;
        }

        [CommandCmif(6002)]
        // SetLayerVisibility(b8, u64)
        public ResultCode SetLayerVisibility(ServiceCtx context)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceVi);

            return ResultCode.Success;
        }
    }
}
