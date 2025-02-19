using Ryujinx.Common.Memory;
using Ryujinx.Graphics.Nvdec.Vp9.Common;
using Ryujinx.Graphics.Video;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Nvdec.Vp9.Types
{
    internal delegate int VpxGetFrameBufferCbFnT(MemoryAllocator allocator, Ptr<InternalFrameBufferList> cbPriv,
        ulong minSize, ref VpxCodecFrameBuffer fb);

    internal struct Surface : ISurface
    {
        public const int Innerborderinpixels = 96;
        public const int InterpExtend = 4;
        public const int EncBorderInPixels = 160;
        public const int DecBorderInPixels = 32;

        public const int Yv12FlagHighbitdepth = 8;

        public ArrayPtr<byte> YBuffer;
        public ArrayPtr<byte> UBuffer;
        public ArrayPtr<byte> VBuffer;

        public readonly unsafe Plane YPlane => new((nint)YBuffer.ToPointer(), YBuffer.Length);
        public readonly unsafe Plane UPlane => new((nint)UBuffer.ToPointer(), UBuffer.Length);
        public readonly unsafe Plane VPlane => new((nint)VBuffer.ToPointer(), VBuffer.Length);

        public FrameField Field => FrameField.Progressive;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int AlignedWidth { get; private set; }
        public int AlignedHeight { get; private set; }
        public int Stride { get; private set; }
        public int UvWidth { get; private set; }
        public int UvHeight { get; private set; }
        public int UvAlignedWidth { get; private set; }
        public int UvAlignedHeight { get; private set; }
        public int UvStride { get; private set; }
        public bool HighBd { get; private set; }

        public int FrameSize { get; private set; }
        public int Border { get; private set; }

        public int YCropWidth => Width;
        public int YCropHeight => Height;
        public int UvCropWidth => UvWidth;
        public int UvCropHeight => UvHeight;

        public ArrayPtr<byte> BufferAlloc;
        public int BufferAllocSz;
        public int SubsamplingX;
        public int SubsamplingY;
        public uint BitDepth;
        public VpxColorSpace ColorSpace;
        public VpxColorRange ColorRange;
        public int RenderWidth;
        public int RenderHeight;

        public int Corrupted;
        public int Flags;

        private readonly nint _pointer;

        public Surface(int width, int height)
        {
            const int Border = 32;
            const int SsX = 1;
            const int SsY = 1;
            const bool Highbd = false;

            int alignedWidth = (width + 7) & ~7;
            int alignedHeight = (height + 7) & ~7;
            int yStride = (alignedWidth + (2 * Border) + 31) & ~31;
            int yplaneSize = (alignedHeight + (2 * Border)) * yStride;
            int uvWidth = alignedWidth >> SsX;
            int uvHeight = alignedHeight >> SsY;
            int uvStride = yStride >> SsX;
            int uvBorderW = Border >> SsX;
            int uvBorderH = Border >> SsY;
            int uvplaneSize = (uvHeight + (2 * uvBorderH)) * uvStride;

            int frameSize = (Highbd ? 2 : 1) * (yplaneSize + (2 * uvplaneSize));

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

            ArrayPtr<byte> NewPlane(int start, int size, int border)
            {
                return new ArrayPtr<byte>(pointer + start + border, size - border);
            }

            YBuffer = NewPlane(0, yplaneSize, (Border * yStride) + Border);
            UBuffer = NewPlane(yplaneSize, uvplaneSize, (uvBorderH * uvStride) + uvBorderW);
            VBuffer = NewPlane(yplaneSize + uvplaneSize, uvplaneSize, (uvBorderH * uvStride) + uvBorderW);
        }

        public unsafe int ReallocFrameBuffer(
            MemoryAllocator allocator,
            int width,
            int height,
            int ssX,
            int ssY,
            bool useHighbitdepth,
            int border,
            int byteAlignment,
            Ptr<VpxCodecFrameBuffer> fb,
            VpxGetFrameBufferCbFnT cb,
            Ptr<InternalFrameBufferList> cbPriv)
        {
            int byteAlign = byteAlignment == 0 ? 1 : byteAlignment; // TODO: Is it safe to ignore the alignment?
            int alignedWidth = (width + 7) & ~7;
            int alignedHeight = (height + 7) & ~7;
            int yStride = (alignedWidth + (2 * border) + 31) & ~31;
            ulong yplaneSize =
                ((ulong)(alignedHeight + (2 * border)) * (ulong)yStride) + (ulong)byteAlignment;
            int uvWidth = alignedWidth >> ssX;
            int uvHeight = alignedHeight >> ssY;
            int uvStride = yStride >> ssX;
            int uvBorderW = border >> ssX;
            int uvBorderH = border >> ssY;
            ulong uvplaneSize =
                ((ulong)(uvHeight + (2 * uvBorderH)) * (ulong)uvStride) + (ulong)byteAlignment;

            ulong frameSize = (ulong)(1 + (useHighbitdepth ? 1 : 0)) * (yplaneSize + (2 * uvplaneSize));

            ArrayPtr<byte> buf = ArrayPtr<byte>.Null;

            // frame_size is stored in buffer_alloc_sz, which is an int. If it won't
            // fit, fail early.
            if (frameSize > int.MaxValue)
            {
                return -1;
            }

            if (cb != null)
            {
                const int AlignAddrExtraSize = 31;
                ulong externalFrameSize = frameSize + AlignAddrExtraSize;

                Debug.Assert(!fb.IsNull);

                // Allocation to hold larger frame, or first allocation.
                if (cb(allocator, cbPriv, externalFrameSize, ref fb.Value) < 0)
                {
                    return -1;
                }

                if (fb.Value.Data.IsNull || (ulong)fb.Value.Data.Length < externalFrameSize)
                {
                    return -1;
                }

                BufferAlloc = fb.Value.Data;
            }
            else if (frameSize > (ulong)BufferAllocSz)
            {
                // Allocation to hold larger frame, or first allocation.
                allocator.Free(BufferAlloc);
                BufferAlloc = ArrayPtr<byte>.Null;

                BufferAlloc = allocator.Allocate<byte>((int)frameSize);
                if (BufferAlloc.IsNull)
                {
                    return -1;
                }

                BufferAllocSz = (int)frameSize;

                // This memset is needed for fixing valgrind error from C loop filter
                // due to access uninitialized memory in frame border. It could be
                // removed if border is totally removed.
                MemoryUtil.Fill(BufferAlloc.ToPointer(), (byte)0, BufferAllocSz);
            }

            /* Only support allocating buffers that have a border that's a multiple
             * of 32. The border restriction is required to get 16-byte alignment of
             * the start of the chroma rows without introducing an arbitrary gap
             * between planes, which would break the semantics of things like
             * vpx_img_set_rect(). */
            if ((border & 0x1f) != 0)
            {
                return -3;
            }

            Width = width;
            Height = height;
            AlignedWidth = alignedWidth;
            AlignedHeight = alignedHeight;
            Stride = yStride;

            UvWidth = (width + ssX) >> ssX;
            UvHeight = (height + ssY) >> ssY;
            UvAlignedWidth = uvWidth;
            UvAlignedHeight = uvHeight;
            UvStride = uvStride;

            Border = border;
            FrameSize = (int)frameSize;
            SubsamplingX = ssX;
            SubsamplingY = ssY;

            buf = BufferAlloc;
            if (useHighbitdepth)
            {
                // Store uint16 addresses when using 16bit framebuffers
                buf = BufferAlloc;
                Flags = Yv12FlagHighbitdepth;
            }
            else
            {
                Flags = 0;
            }

            YBuffer = buf.Slice((border * yStride) + border);
            UBuffer = buf.Slice((int)yplaneSize + (uvBorderH * uvStride) + uvBorderW);
            VBuffer = buf.Slice((int)yplaneSize + (int)uvplaneSize + (uvBorderH * uvStride) + uvBorderW);

            Corrupted = 0; /* assume not corrupted by errors */
            return 0;
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal(_pointer);
        }
    }
}
