using Ryujinx.Common.Memory;
using Ryujinx.Graphics.Video;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Nvdec.Vp9.Types
{
    internal struct Surface : ISurface
    {
        public ArrayPtr<byte> YBuffer;
        public ArrayPtr<byte> UBuffer;
        public ArrayPtr<byte> VBuffer;

        public readonly unsafe Plane YPlane => new((nint)YBuffer.ToPointer(), YBuffer.Length);
        public readonly unsafe Plane UPlane => new((nint)UBuffer.ToPointer(), UBuffer.Length);
        public readonly unsafe Plane VPlane => new((nint)VBuffer.ToPointer(), VBuffer.Length);

        public readonly FrameField Field => FrameField.Progressive;

        public int Width { get; }
        public int Height { get; }
        public int AlignedWidth { get; }
        public int AlignedHeight { get; }
        public int Stride { get; }
        public int UvWidth { get; }
        public int UvHeight { get; }
        public int UvAlignedWidth { get; }
        public int UvAlignedHeight { get; }
        public int UvStride { get; }

        public bool HighBd { get; }

        private readonly nint _pointer;

        public Surface(int width, int height)
        {
            HighBd = false;

            const int Border = 32;
            const int SsX = 1;
            const int SsY = 1;

            int alignedWidth = (width + 7) & ~7;
            int alignedHeight = (height + 7) & ~7;
            int yStride = ((alignedWidth + 2 * Border) + 31) & ~31;
            int yplaneSize = (alignedHeight + 2 * Border) * yStride;
            int uvWidth = alignedWidth >> SsX;
            int uvHeight = alignedHeight >> SsY;
            int uvStride = yStride >> SsX;
            int uvBorderW = Border >> SsX;
            int uvBorderH = Border >> SsY;
            int uvplaneSize = (uvHeight + 2 * uvBorderH) * uvStride;

            int frameSize = (HighBd ? 2 : 1) * (yplaneSize + 2 * uvplaneSize);

            nint pointer = Marshal.AllocHGlobal(frameSize);
            _pointer = pointer;
            Width = width;
            Height = height;
            AlignedWidth = alignedWidth;
            AlignedHeight = alignedHeight;
            Stride = yStride;
            UvWidth = (width + SsX) >> SsX;
            UvHeight = (height + SsY) >> SsY;
            UvAlignedWidth = uvWidth;
            UvAlignedHeight = uvHeight;
            UvStride = uvStride;

            ArrayPtr<byte> NewPlane(int start, int size, int planeBorder)
            {
                return new ArrayPtr<byte>(pointer + start + planeBorder, size - planeBorder);
            }

            YBuffer = NewPlane(0, yplaneSize, (Border * yStride) + Border);
            UBuffer = NewPlane(yplaneSize, uvplaneSize, (uvBorderH * uvStride) + uvBorderW);
            VBuffer = NewPlane(yplaneSize + uvplaneSize, uvplaneSize, (uvBorderH * uvStride) + uvBorderW);
        }

        public readonly void Dispose()
        {
            Marshal.FreeHGlobal(_pointer);
        }
    }
}
