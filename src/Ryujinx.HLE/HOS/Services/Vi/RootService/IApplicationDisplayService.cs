using Ryujinx.Common;
using Ryujinx.Common.Logging;
using Ryujinx.Common.Memory;
using Ryujinx.HLE.HOS.Applets;
using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.HOS.Services.SurfaceFlinger;
using Ryujinx.HLE.HOS.Services.Vi.RootService.ApplicationDisplayService;
using Ryujinx.HLE.HOS.Services.Vi.RootService.ApplicationDisplayService.Types;
using Ryujinx.HLE.HOS.Services.Vi.Types;
using Ryujinx.HLE.UI;
using Ryujinx.Horizon.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Ryujinx.HLE.HOS.Services.Vi.RootService
{
    class IApplicationDisplayService : IpcService
    {
        private readonly ViServiceType _serviceType;

        private class DisplayState
        {
            public int RetrievedEventsCount;
        }

        private readonly List<DisplayInfo> _displayInfo;
        private readonly Dictionary<ulong, DisplayState> _openDisplays;

        private int _vsyncEventHandle;

        public IApplicationDisplayService(ViServiceType serviceType)
        {
            _serviceType = serviceType;
            _displayInfo = [];
            _openDisplays = new Dictionary<ulong, DisplayState>();

            void AddDisplayInfo(string name, bool layerLimitEnabled, ulong layerLimitMax, ulong width, ulong height)
            {
                DisplayInfo displayInfo = new()
                {
                    Name = new Array64<byte>(),
                    LayerLimitEnabled = layerLimitEnabled,
                    Padding = new Array7<byte>(),
                    LayerLimitMax = layerLimitMax,
                    Width = width,
                    Height = height,
                };

                Encoding.ASCII.GetBytes(name).AsSpan().CopyTo(displayInfo.Name.AsSpan());

                _displayInfo.Add(displayInfo);
            }

            AddDisplayInfo("Default", true, 1, 1920, 1080);
            AddDisplayInfo("External", true, 1, 1920, 1080);
            AddDisplayInfo("Edid", true, 1, 0, 0);
            AddDisplayInfo("Internal", true, 1, 1920, 1080);
            AddDisplayInfo("Null", false, 0, 1920, 1080);
        }

        [CommandCmif(100)]
        // GetRelayService() -> object<nns::hosbinder::IHOSBinderDriver>
        public ResultCode GetRelayService(ServiceCtx context)
        {
            // FIXME: Should be _serviceType != ViServiceType.Application but guests crashes if we do this check.
            if (_serviceType > ViServiceType.System)
            {
                return ResultCode.PermissionDenied;
            }

            MakeObject(context, new HOSBinderDriverServer());

            return ResultCode.Success;
        }

        [CommandCmif(101)]
        // GetSystemDisplayService() -> object<nn::visrv::sf::ISystemDisplayService>
        public ResultCode GetSystemDisplayService(ServiceCtx context)
        {
            // FIXME: Should be _serviceType == ViServiceType.System but guests crashes if we do this check.
            if (_serviceType > ViServiceType.System)
            {
                return ResultCode.PermissionDenied;
            }

            MakeObject(context, new ISystemDisplayService(this));

            return ResultCode.Success;
        }

        [CommandCmif(102)]
        // GetManagerDisplayService() -> object<nn::visrv::sf::IManagerDisplayService>
        public ResultCode GetManagerDisplayService(ServiceCtx context)
        {
            if (_serviceType > ViServiceType.System)
            {
                return ResultCode.PermissionDenied;
            }

            MakeObject(context, new IManagerDisplayService(this));

            return ResultCode.Success;
        }

        [CommandCmif(103)] // 2.0.0+
        // GetIndirectDisplayTransactionService() -> object<nns::hosbinder::IHOSBinderDriver>
        public ResultCode GetIndirectDisplayTransactionService(ServiceCtx context)
        {
            if (_serviceType > ViServiceType.System)
            {
                return ResultCode.PermissionDenied;
            }

            MakeObject(context, new HOSBinderDriverServer());

            return ResultCode.Success;
        }

        [CommandCmif(1000)]
        // ListDisplays() -> (u64 count, buffer<nn::vi::DisplayInfo, 6>)
        public ResultCode ListDisplays(ServiceCtx context)
        {
            ulong displayInfoBuffer = context.Request.ReceiveBuff[0].Position;

            // TODO: Determine when more than one display is needed.
            ulong displayCount = 1;

            for (int i = 0; i < (int)displayCount; i++)
            {
                context.Memory.Write(displayInfoBuffer + (ulong)(i * Unsafe.SizeOf<DisplayInfo>()), _displayInfo[i]);
            }

            context.ResponseData.Write(displayCount);

            return ResultCode.Success;
        }

        [CommandCmif(1010)]
        // OpenDisplay(nn::vi::DisplayName) -> u64 display_id
        public ResultCode OpenDisplay(ServiceCtx context)
        {
            StringBuilder nameBuilder = new();

            for (int index = 0; index < 8 && context.RequestData.BaseStream.Position < context.RequestData.BaseStream.Length; index++)
            {
                byte chr = context.RequestData.ReadByte();

                if (chr >= 0x20 && chr < 0x7f)
                {
                    nameBuilder.Append((char)chr);
                }
            }

            return OpenDisplayImpl(context, nameBuilder.ToString());
        }

        [CommandCmif(1011)]
        // OpenDefaultDisplay() -> u64 display_id
        public ResultCode OpenDefaultDisplay(ServiceCtx context)
        {
            return OpenDisplayImpl(context, "Default");
        }

        private ResultCode OpenDisplayImpl(ServiceCtx context, string name)
        {
            if (name == string.Empty)
            {
                return ResultCode.InvalidValue;
            }

            int displayId = _displayInfo.FindIndex(display => Encoding.ASCII.GetString(display.Name.AsSpan()).Trim('\0') == name);

            if (displayId == -1)
            {
                return ResultCode.InvalidValue;
            }

            if (!_openDisplays.TryAdd((ulong)displayId, new DisplayState()))
            {
                return ResultCode.AlreadyOpened;
            }

            context.ResponseData.Write((ulong)displayId);

            return ResultCode.Success;
        }

        [CommandCmif(1020)]
        // CloseDisplay(u64 display_id)
        public ResultCode CloseDisplay(ServiceCtx context)
        {
            ulong displayId = context.RequestData.ReadUInt64();

            if (!_openDisplays.Remove(displayId))
            {
                return ResultCode.InvalidValue;
            }

            return ResultCode.Success;
        }

        [CommandCmif(1101)]
        // SetDisplayEnabled(u32 enabled_bool, u64 display_id)
        public ResultCode SetDisplayEnabled(ServiceCtx context)
        {
            // NOTE: Stubbed in original service.
            return ResultCode.Success;
        }

        [CommandCmif(1102)]
        // GetDisplayResolution(u64 display_id) -> (u64 width, u64 height)
        public ResultCode GetDisplayResolution(ServiceCtx context)
        {
            // NOTE: Not used in original service.
            // ulong displayId = context.RequestData.ReadUInt64();

            // NOTE: Returns ResultCode.InvalidArguments if width and height pointer are null, doesn't occur in our case.

            // NOTE: Values are hardcoded in original service.
            context.ResponseData.Write(1280UL); // Width
            context.ResponseData.Write(720UL);  // Height

            return ResultCode.Success;
        }

        [CommandCmif(2020)]
        // OpenLayer(nn::vi::DisplayName, u64, nn::applet::AppletResourceUserId, pid) -> (u64, buffer<bytes, 6>)
        public ResultCode OpenLayer(ServiceCtx context)
        {
            // TODO: support multi display.
#pragma warning disable IDE0059 // Remove unnecessary value assignment
            byte[] displayName = context.RequestData.ReadBytes(0x40);
#pragma warning restore IDE0059

            long layerId = context.RequestData.ReadInt64();
#pragma warning disable IDE0059 // Remove unnecessary value assignment
            long userId = context.RequestData.ReadInt64();
#pragma warning restore IDE0059
            ulong parcelPtr = context.Request.ReceiveBuff[0].Position;

            ResultCode result = context.Device.System.SurfaceFlinger.OpenLayer(context.Request.HandleDesc.PId, layerId, out IBinder producer);

            if (result != ResultCode.Success)
            {
                return result;
            }

            context.Device.System.SurfaceFlinger.SetRenderLayer(layerId);

            using Parcel parcel = new(0x28, 0x4);

            parcel.WriteObject(producer, "dispdrv\0");

            ReadOnlySpan<byte> parcelData = parcel.Finish();

            context.Memory.Write(parcelPtr, parcelData);

            context.ResponseData.Write((long)parcelData.Length);

            return ResultCode.Success;
        }

        [CommandCmif(2021)]
        // CloseLayer(u64)
        public ResultCode CloseLayer(ServiceCtx context)
        {
            long layerId = context.RequestData.ReadInt64();

            return context.Device.System.SurfaceFlinger.CloseLayer(layerId);
        }

        [CommandCmif(2030)]
        // CreateStrayLayer(u32, u64) -> (u64, u64, buffer<bytes, 6>)
        public ResultCode CreateStrayLayer(ServiceCtx context)
        {
#pragma warning disable IDE0059 // Remove unnecessary value assignment
            long layerFlags = context.RequestData.ReadInt64();
            long displayId = context.RequestData.ReadInt64();
#pragma warning restore IDE0059

            ulong parcelPtr = context.Request.ReceiveBuff[0].Position;

            // TODO: support multi display.
            IBinder producer = context.Device.System.SurfaceFlinger.CreateLayer(out long layerId, 0, LayerState.Stray);

            context.Device.System.SurfaceFlinger.SetRenderLayer(layerId);

            using Parcel parcel = new(0x28, 0x4);

            parcel.WriteObject(producer, "dispdrv\0");

            ReadOnlySpan<byte> parcelData = parcel.Finish();

            context.Memory.Write(parcelPtr, parcelData);

            context.ResponseData.Write(layerId);
            context.ResponseData.Write((long)parcelData.Length);

            return ResultCode.Success;
        }

        [CommandCmif(2031)]
        // DestroyStrayLayer(u64)
        public ResultCode DestroyStrayLayer(ServiceCtx context)
        {
            long layerId = context.RequestData.ReadInt64();

            return context.Device.System.SurfaceFlinger.DestroyStrayLayer(layerId);
        }

        [CommandCmif(2101)]
        // SetLayerScalingMode(u32, u64)
        public ResultCode SetLayerScalingMode(ServiceCtx context)
        {
            /*
            uint  sourceScalingMode = context.RequestData.ReadUInt32();
            ulong layerId           = context.RequestData.ReadUInt64();
            */
            // NOTE: Original service converts SourceScalingMode to DestinationScalingMode but does nothing with the converted value.

            return ResultCode.Success;
        }

        [CommandCmif(2102)] // 5.0.0+
        // ConvertScalingMode(u32 source_scaling_mode) -> u64 destination_scaling_mode
        public ResultCode ConvertScalingMode(ServiceCtx context)
        {
            SourceScalingMode scalingMode = (SourceScalingMode)context.RequestData.ReadInt32();

            DestinationScalingMode? convertedScalingMode = scalingMode switch
            {
                SourceScalingMode.None => DestinationScalingMode.None,
                SourceScalingMode.Freeze => DestinationScalingMode.Freeze,
                SourceScalingMode.ScaleAndCrop => DestinationScalingMode.ScaleAndCrop,
                SourceScalingMode.ScaleToWindow => DestinationScalingMode.ScaleToWindow,
                SourceScalingMode.PreserveAspectRatio => DestinationScalingMode.PreserveAspectRatio,
                _ => null,
            };

            if (!convertedScalingMode.HasValue)
            {
                // Scaling mode out of the range of valid values.
                return ResultCode.InvalidArguments;
            }

            if (scalingMode != SourceScalingMode.ScaleToWindow && scalingMode != SourceScalingMode.PreserveAspectRatio)
            {
                // Invalid scaling mode specified.
                return ResultCode.InvalidScalingMode;
            }

            context.ResponseData.Write((ulong)convertedScalingMode);

            return ResultCode.Success;
        }

        private ulong GetA8B8G8R8LayerSize(int width, int height, out int pitch, out int alignment)
        {
            const int DefaultAlignment = 0x1000;
            const ulong DefaultSize = 0x20000;

            alignment = DefaultAlignment;
            pitch = BitUtils.AlignUp(BitUtils.DivRoundUp(width * 32, 8), 64);

            int memorySize = pitch * BitUtils.AlignUp(height, 64);
            ulong requiredMemorySize = (ulong)BitUtils.AlignUp(memorySize, alignment);

            return (requiredMemorySize + DefaultSize - 1) / DefaultSize * DefaultSize;
        }

        [CommandCmif(2450)]
        // GetIndirectLayerImageMap(s64 width, s64 height, u64 handle, nn::applet::AppletResourceUserId, pid) -> (s64, s64, buffer<bytes, 0x46>)
        public ResultCode GetIndirectLayerImageMap(ServiceCtx context)
        {
            // The size of the layer buffer should be an aligned multiple of width * height
            // because it was created using GetIndirectLayerImageRequiredMemoryInfo as a guide.

            long layerWidth = context.RequestData.ReadInt64();
            long layerHeight = context.RequestData.ReadInt64();
            long layerHandle = context.RequestData.ReadInt64();
            ulong layerBuffPosition = context.Request.ReceiveBuff[0].Position;
            ulong layerBuffSize = context.Request.ReceiveBuff[0].Size;

            // Get the pitch of the layer that is necessary to render correctly.
            ulong size = GetA8B8G8R8LayerSize((int)layerWidth, (int)layerHeight, out int pitch, out _);

            Debug.Assert(layerBuffSize == size);

            RenderingSurfaceInfo surfaceInfo = new(ColorFormat.A8B8G8R8, (uint)layerWidth, (uint)layerHeight, (uint)pitch, (uint)layerBuffSize);

            // Get the applet associated with the handle.
            object appletObject = context.Device.System.AppletState.IndirectLayerHandles.GetData((int)layerHandle);

            if (appletObject == null)
            {
                Logger.Error?.Print(LogClass.ServiceVi, $"Indirect layer handle {layerHandle} does not match any applet");

                return ResultCode.Success;
            }

            Debug.Assert(appletObject is IApplet);

            IApplet applet = appletObject as IApplet;

            if (!applet.DrawTo(surfaceInfo, context.Memory, layerBuffPosition))
            {
                Logger.Warning?.Print(LogClass.ServiceVi, $"Applet did not draw on indirect layer handle {layerHandle}");

                return ResultCode.Success;
            }

            context.ResponseData.Write(layerWidth);
            context.ResponseData.Write(layerHeight);

            return ResultCode.Success;
        }

        [CommandCmif(2460)]
        // GetIndirectLayerImageRequiredMemoryInfo(u64 width, u64 height) -> (u64 size, u64 alignment)
        public ResultCode GetIndirectLayerImageRequiredMemoryInfo(ServiceCtx context)
        {
            /*
            // Doesn't occur in our case.
            if (sizePtr == null || address_alignmentPtr == null)
            {
                return ResultCode.InvalidArguments;
            }
            */

            int width = (int)context.RequestData.ReadUInt64();
            int height = (int)context.RequestData.ReadUInt64();

            if (height < 0 || width < 0)
            {
                return ResultCode.InvalidLayerSize;
            }
            else
            {
                /*
                // Doesn't occur in our case.
                if (!service_initialized)
                {
                    return ResultCode.InvalidArguments;
                }
                */

                // NOTE: The official service setup a A8B8G8R8 texture with a linear layout and then query its size.
                //       As we don't need this texture on the emulator, we can just simplify this logic and directly
                //       do a linear layout size calculation. (stride * height * bytePerPixel)
                ulong size = GetA8B8G8R8LayerSize(width, height, out _, out int alignment);

                context.ResponseData.Write(size);
                context.ResponseData.Write(alignment);
            }

            return ResultCode.Success;
        }

        [CommandCmif(5202)]
        // GetDisplayVsyncEvent(u64) -> handle<copy>
        public ResultCode GetDisplayVSyncEvent(ServiceCtx context)
        {
            ulong displayId = context.RequestData.ReadUInt64();

            if (!_openDisplays.TryGetValue(displayId, out DisplayState displayState))
            {
                return ResultCode.InvalidValue;
            }

            if (displayState.RetrievedEventsCount > 0)
            {
                return ResultCode.PermissionDenied;
            }

            if (_vsyncEventHandle == 0)
            {
                if (context.Process.HandleTable.GenerateHandle(context.Device.System.VsyncEvent.ReadableEvent, out _vsyncEventHandle) != Result.Success)
                {
                    throw new InvalidOperationException("Out of handles!");
                }
            }

            displayState.RetrievedEventsCount++;
            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(_vsyncEventHandle);

            return ResultCode.Success;
        }
    }
}
