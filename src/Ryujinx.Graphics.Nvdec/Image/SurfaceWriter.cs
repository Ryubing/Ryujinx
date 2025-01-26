using Ryujinx.Common;
using Ryujinx.Graphics.Device;
using Ryujinx.Graphics.Texture;
using Ryujinx.Graphics.Video;
using Ryujinx.Memory;
using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using static Ryujinx.Graphics.Nvdec.Image.SurfaceCommon;
using static Ryujinx.Graphics.Nvdec.MemoryExtensions;

namespace Ryujinx.Graphics.Nvdec.Image
{
    static class SurfaceWriter
    {
        public static void Write(DeviceMemoryManager mm, ISurface surface, uint lumaOffset, uint chromaOffset)
        {
            int lumaSize = GetBlockLinearSize(surface.Width, surface.Height, 1);

            using WritableRegion luma = mm.GetWritableRegion(ExtendOffset(lumaOffset), lumaSize);

            WriteLuma(
                luma.Memory.Span,
                surface.YPlane.AsSpan(),
                surface.Stride,
                surface.Width,
                surface.Height);

            int chromaSize = GetBlockLinearSize(surface.UvWidth, surface.UvHeight, 2);

            using WritableRegion chroma = mm.GetWritableRegion(ExtendOffset(chromaOffset), chromaSize);

            WriteChroma(
                chroma.Memory.Span,
                surface.UPlane.AsSpan(),
                surface.VPlane.AsSpan(),
                surface.UvStride,
                surface.UvWidth,
                surface.UvHeight);
        }

        public static void WriteInterlaced(
            DeviceMemoryManager mm,
            ISurface surface,
            uint lumaTopOffset,
            uint chromaTopOffset,
            uint lumaBottomOffset,
            uint chromaBottomOffset)
        {
            int lumaSize = GetBlockLinearSize(surface.Width, surface.Height / 2, 1);

            using WritableRegion lumaTop = mm.GetWritableRegion(ExtendOffset(lumaTopOffset), lumaSize);
            using WritableRegion lumaBottom = mm.GetWritableRegion(ExtendOffset(lumaBottomOffset), lumaSize);

            WriteLuma(
                lumaTop.Memory.Span,
                surface.YPlane.AsSpan(),
                surface.Stride * 2,
                surface.Width,
                surface.Height / 2);

            WriteLuma(
                lumaBottom.Memory.Span,
                surface.YPlane.AsSpan()[surface.Stride..],
                surface.Stride * 2,
                surface.Width,
                surface.Height / 2);

            int chromaSize = GetBlockLinearSize(surface.UvWidth, surface.UvHeight / 2, 2);

            using WritableRegion chromaTop = mm.GetWritableRegion(ExtendOffset(chromaTopOffset), chromaSize);
            using WritableRegion chromaBottom = mm.GetWritableRegion(ExtendOffset(chromaBottomOffset), chromaSize);

            WriteChroma(
                chromaTop.Memory.Span,
                surface.UPlane.AsSpan(),
                surface.VPlane.AsSpan(),
                surface.UvStride * 2,
                surface.UvWidth,
                surface.UvHeight / 2);

            WriteChroma(
                chromaBottom.Memory.Span,
                surface.UPlane.AsSpan()[surface.UvStride..],
                surface.VPlane.AsSpan()[surface.UvStride..],
                surface.UvStride * 2,
                surface.UvWidth,
                surface.UvHeight / 2);
        }

        private static void WriteLuma(Span<byte> dst, ReadOnlySpan<byte> src, int srcStride, int width, int height)
        {
            LayoutConverter.ConvertLinearToBlockLinear(dst, width, height, srcStride, 1, 2, src);
        }

        private unsafe static void WriteChroma(
            Span<byte> dst,
            ReadOnlySpan<byte> srcU,
            ReadOnlySpan<byte> srcV,
            int srcStride,
            int width,
            int height)
        {
            OffsetCalculator calc = new(width, height, 0, false, 2, 2);

            if (Sse2.IsSupported)
            {
                int strideTrunc64 = BitUtils.AlignDown(width * 2, 64);

                int inStrideGap = srcStride - width;

                fixed (byte* outputPtr = dst, srcUPtr = srcU, srcVPtr = srcV)
                {
                    byte* inUPtr = srcUPtr;
                    byte* inVPtr = srcVPtr;

                    for (int y = 0; y < height; y++)
                    {
                        calc.SetY(y);

                        for (int x = 0; x < strideTrunc64; x += 64, inUPtr += 32, inVPtr += 32)
                        {
                            byte* offset = outputPtr + calc.GetOffsetWithLineOffset64(x);
                            byte* offset2 = offset + 0x20;
                            byte* offset3 = offset + 0x100;
                            byte* offset4 = offset + 0x120;

                            Vector128<byte> value = *(Vector128<byte>*)inUPtr;
                            Vector128<byte> value2 = *(Vector128<byte>*)inVPtr;
                            Vector128<byte> value3 = *(Vector128<byte>*)(inUPtr + 16);
                            Vector128<byte> value4 = *(Vector128<byte>*)(inVPtr + 16);

                            Vector128<byte> uv0 = Sse2.UnpackLow(value, value2);
                            Vector128<byte> uv1 = Sse2.UnpackHigh(value, value2);
                            Vector128<byte> uv2 = Sse2.UnpackLow(value3, value4);
                            Vector128<byte> uv3 = Sse2.UnpackHigh(value3, value4);

                            *(Vector128<byte>*)offset = uv0;
                            *(Vector128<byte>*)offset2 = uv1;
                            *(Vector128<byte>*)offset3 = uv2;
                            *(Vector128<byte>*)offset4 = uv3;
                        }

                        for (int x = strideTrunc64 / 2; x < width; x++, inUPtr++, inVPtr++)
                        {
                            byte* offset = outputPtr + calc.GetOffset(x);

                            *offset = *inUPtr;
                            *(offset + 1) = *inVPtr;
                        }

                        inUPtr += inStrideGap;
                        inVPtr += inStrideGap;
                    }
                }
            }
            else
            {
                for (int y = 0; y < height; y++)
                {
                    int srcBaseOffset = y * srcStride;

                    calc.SetY(y);

                    for (int x = 0; x < width; x++)
                    {
                        int dstOffset = calc.GetOffset(x);

                        dst[dstOffset + 0] = srcU[srcBaseOffset + x];
                        dst[dstOffset + 1] = srcV[srcBaseOffset + x];
                    }
                }
            }
        }
    }
}
