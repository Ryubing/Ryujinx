using Ryujinx.Common;
using Ryujinx.Graphics.Gpu;
using Ryujinx.HLE.HOS.Kernel;
using Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvMap;
using Ryujinx.HLE.HOS.Services.SurfaceFlinger;
using Ryujinx.HLE.HOS.Services.SurfaceFlinger.Types;
using Ryujinx.HLE.HOS.Services.Vi.Types;
using Ryujinx.Memory;
using System.Runtime.CompilerServices;

namespace Ryujinx.HLE.HOS.Services
{
    class ViServer : ServerBase
    {
        private const int TotalFramebuffers = 16;

        private SurfaceFlinger.SurfaceFlinger _surfaceFlinger;

        private readonly uint _fbWidth;
        private readonly uint _fbHeight;
        private readonly PixelFormat _fbFormat;
        private readonly int _fbUsage;
        private readonly uint _fbCount;
        private uint _fbSlotsRequested;

        private ulong _pid;
        private ulong _fbsBaseAddress;
        private int _bufferNvMapId;
        private SharedBufferMap _bufferMap;
        private long _sharedLayerId;

        public ViServer(KernelContext context, string name) : base(context, name)
        {
            _fbWidth = 1280;
            _fbHeight = 720;
            _fbFormat = PixelFormat.Rgba8888;
            _fbUsage = 0x100 | 0x200 | 0x800;
            _fbCount = 4;
        }

        protected override ulong CalculateRequiredHeapSize()
        {
            return GetSharedBufferSize();
        }

        protected override void CustomInit(KernelContext context, ulong pid, ulong heapAddress)
        {
            _pid = pid;
            _fbsBaseAddress = heapAddress;

            context.Device.Gpu.RegisterProcess(pid, KernelStatic.GetCurrentProcess().CpuMemory);

            ulong bufferSize = CalculateFramebufferSize();
            ulong totalSize = bufferSize * TotalFramebuffers;

            KernelStatic.GetCurrentProcess().CpuMemory.Fill(heapAddress, totalSize, 0xff);

            int mapId = NvMapDeviceFile.CreateMap(pid, heapAddress, (uint)totalSize);

            _bufferNvMapId = mapId;
            _bufferMap.Count = TotalFramebuffers;

            ulong offset = 0;

            for (int i = 0; i < TotalFramebuffers; i++)
            {
                _bufferMap.SharedBuffers[i].Offset = offset;
                _bufferMap.SharedBuffers[i].Size = bufferSize;
                _bufferMap.SharedBuffers[i].Width = _fbWidth;
                _bufferMap.SharedBuffers[i].Height = _fbHeight;

                offset += bufferSize;
            }

            _surfaceFlinger = context.Device.System.SurfaceFlinger;
            _surfaceFlinger.CreateLayer(out _sharedLayerId, pid);
        }

        public void OpenSharedLayer(long layerId)
        {
            _surfaceFlinger.OpenLayer(_pid, layerId, out _);
        }

        public void CloseSharedLayer(long layerId)
        {
            _surfaceFlinger.CloseLayer(layerId);
        }

        public void ConnectSharedLayer(long layerId)
        {
            IGraphicBufferProducer producer = _surfaceFlinger.GetProducerByLayerId(layerId);

            producer.Connect(null, NativeWindowApi.NVN, false, out IGraphicBufferProducer.QueueBufferOutput output);

            GraphicBuffer graphicBuffer = new GraphicBuffer();

            int gobHeightLog2 = 4;
            int blockHeight = 8 * (1 << gobHeightLog2);
            uint widthAlignedBytes = BitUtils.AlignUp(_fbWidth * 4, 64u);
            uint widthAligned = widthAlignedBytes / 4;
            uint heightAligned = BitUtils.AlignUp(_fbHeight, (uint)blockHeight);
            uint totalSize = widthAlignedBytes * heightAligned;

            graphicBuffer.Header.Magic = 0x47424652;
            graphicBuffer.Header.Width = (int)_fbWidth;
            graphicBuffer.Header.Height = (int)_fbHeight;
            graphicBuffer.Header.Stride = (int)widthAligned;
            graphicBuffer.Header.Format = _fbFormat;
            graphicBuffer.Header.Usage = _fbUsage;
            graphicBuffer.Header.IntsCount = (Unsafe.SizeOf<GraphicBuffer>() - Unsafe.SizeOf<GraphicBufferHeader>()) / sizeof(int);
            graphicBuffer.Buffer.NvMapId = _bufferNvMapId;
            graphicBuffer.Buffer.Magic = unchecked((int)0xDAFFCAFF);
            graphicBuffer.Buffer.Pid = 42;
            graphicBuffer.Buffer.Usage = _fbUsage;
            graphicBuffer.Buffer.PixelFormat = (int)_fbFormat;
            graphicBuffer.Buffer.ExternalPixelFormat = (int)_fbFormat;
            graphicBuffer.Buffer.Stride = (int)widthAligned;
            graphicBuffer.Buffer.FrameBufferSize = (int)totalSize;
            graphicBuffer.Buffer.PlanesCount = 1;
            graphicBuffer.Buffer.Surfaces[0].Width = _fbWidth;
            graphicBuffer.Buffer.Surfaces[0].Height = _fbHeight;
            graphicBuffer.Buffer.Surfaces[0].ColorFormat = ColorFormat.A8B8G8R8;
            graphicBuffer.Buffer.Surfaces[0].Layout = 3; // Block linear
            graphicBuffer.Buffer.Surfaces[0].Pitch = (int)widthAlignedBytes;
            graphicBuffer.Buffer.Surfaces[0].Kind = 0xfe; // Generic 16Bx2
            graphicBuffer.Buffer.Surfaces[0].BlockHeightLog2 = gobHeightLog2;
            graphicBuffer.Buffer.Surfaces[0].Size = (int)totalSize;

            for (int slot = 0; slot < _fbCount; slot++)
            {
                graphicBuffer.Buffer.Surfaces[0].Offset = slot * (int)totalSize;

                producer.SetPreallocatedBuffer(slot, new AndroidStrongPointer<GraphicBuffer>(graphicBuffer));
            }

            _fbSlotsRequested = 0;
        }

        public void DisconnectSharedLayer(long layerId)
        {
            IGraphicBufferProducer producer = _surfaceFlinger.GetProducerByLayerId(layerId);

            producer.Disconnect(NativeWindowApi.NVN);
        }

        public int DequeueFrameBuffer(long layerId, out AndroidFence fence)
        {
            IGraphicBufferProducer producer = _surfaceFlinger.GetProducerByLayerId(layerId);

            Status status = producer.DequeueBuffer(out int slot, out fence, false, _fbWidth, _fbHeight, _fbFormat, (uint)_fbUsage);

            if (status == Status.Success)
            {
                if ((_fbSlotsRequested & (1u << slot)) == 0)
                {
                    status = producer.RequestBuffer(slot, out _);

                    if (status != Status.Success)
                    {
                        producer.CancelBuffer(slot, ref fence);
                    }

                    _fbSlotsRequested |= 1u << slot;
                }
            }

            return slot;
        }

        public void QueueFrameBuffer(long layerId, int slot, Rect crop, NativeWindowTransform transform, int swapInterval, AndroidFence fence)
        {
            IGraphicBufferProducer producer = _surfaceFlinger.GetProducerByLayerId(layerId);

            IGraphicBufferProducer.QueueBufferInput input = new();

            input.Crop = crop;
            input.Transform = transform;
            input.SwapInterval = swapInterval;
            input.Fence = fence;

            Status status = producer.QueueBuffer(slot, ref input, out _);
        }

        public void CancelFrameBuffer(long layerId, int slot)
        {
            IGraphicBufferProducer producer = _surfaceFlinger.GetProducerByLayerId(layerId);
            AndroidFence fence = default;

            producer.CancelBuffer(slot, ref fence);
        }

        public int GetFrameBufferMapIndex(int index)
        {
            return (uint)index < _fbCount ? index : 0;
        }

        public int GetSharedBufferNvMapId()
        {
            return _bufferNvMapId;
        }

        public ulong GetSharedBufferSize()
        {
            return CalculateFramebufferSize() * TotalFramebuffers;
        }

        public long GetSharedLayerId()
        {
            return _sharedLayerId;
        }

        private ulong CalculateFramebufferSize()
        {
            // Each GOB dimension is 512 bytes x 8 lines.
            // Assume 16 GOBs, for a total of 16 x 8 = 128 lines.
            return BitUtils.AlignUp(_fbWidth * 4, 512u) * BitUtils.AlignUp(_fbHeight, 128u);
        }

        public SharedBufferMap GetSharedBufferMap()
        {
            return _bufferMap;
        }

        public int GetApplicationLastPresentedFrameHandle(GpuContext gpuContext)
        {
            TextureData texture = gpuContext.Window.GetLastPresentedData();
            IVirtualMemoryManagerTracked selfAs = KernelStatic.GetProcessByPid(_pid).CpuMemory;
            int fbIndex = (int)_fbCount; // Place it after all our frame buffers.

            selfAs.Write(_fbsBaseAddress + _bufferMap.SharedBuffers[fbIndex].Offset, texture.Data);

            return fbIndex;
        }
    }
}
