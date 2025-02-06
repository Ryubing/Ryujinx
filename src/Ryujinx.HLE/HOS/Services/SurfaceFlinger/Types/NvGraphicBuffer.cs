using Ryujinx.Common.Memory;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.SurfaceFlinger
{
    [StructLayout(LayoutKind.Explicit, Size = 0x144, Pack = 1)]
    struct NvGraphicBuffer
    {
        [FieldOffset(0x4)]
        public int NvMapId;

        [FieldOffset(0xC)]
        public int Magic;

        [FieldOffset(0x10)]
        public int Pid;

        [FieldOffset(0x14)]
        public int Type;

        [FieldOffset(0x18)]
        public int Usage;

        [FieldOffset(0x1C)]
        public int PixelFormat;

        [FieldOffset(0x20)]
        public int ExternalPixelFormat;

        [FieldOffset(0x24)]
        public int Stride;

        [FieldOffset(0x28)]
        public int FrameBufferSize;

        [FieldOffset(0x2C)]
        public int PlanesCount;

        [FieldOffset(0x34)]
        public Array3<NvGraphicBufferSurface> Surfaces;
    }
}
