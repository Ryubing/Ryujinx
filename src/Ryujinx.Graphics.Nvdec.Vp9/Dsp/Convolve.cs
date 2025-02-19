using Ryujinx.Common.Memory;
using Ryujinx.Graphics.Nvdec.Vp9.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using static Ryujinx.Graphics.Nvdec.Vp9.Dsp.Filter;

namespace Ryujinx.Graphics.Nvdec.Vp9.Dsp
{
    internal static class Convolve
    {
        private const bool UseIntrinsics = true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<int> MultiplyAddAdjacent(
            Vector128<short> vsrc0,
            Vector128<short> vsrc1,
            Vector128<short> vsrc2,
            Vector128<short> vsrc3,
            Vector128<short> vfilter,
            Vector128<int> zero)
        {
            // < sumN, sumN, sumN, sumN >
            Vector128<int> sum0 = Sse2.MultiplyAddAdjacent(vsrc0, vfilter);
            Vector128<int> sum1 = Sse2.MultiplyAddAdjacent(vsrc1, vfilter);
            Vector128<int> sum2 = Sse2.MultiplyAddAdjacent(vsrc2, vfilter);
            Vector128<int> sum3 = Sse2.MultiplyAddAdjacent(vsrc3, vfilter);

            // < 0, 0, sumN, sumN >
            sum0 = Ssse3.HorizontalAdd(sum0, zero);
            sum1 = Ssse3.HorizontalAdd(sum1, zero);
            sum2 = Ssse3.HorizontalAdd(sum2, zero);
            sum3 = Ssse3.HorizontalAdd(sum3, zero);

            // < 0, 0, 0, sumN >
            sum0 = Ssse3.HorizontalAdd(sum0, zero);
            sum1 = Ssse3.HorizontalAdd(sum1, zero);
            sum2 = Ssse3.HorizontalAdd(sum2, zero);
            sum3 = Ssse3.HorizontalAdd(sum3, zero);

            // < 0, 0, sum1, sum0 >
            Vector128<int> sum01 = Sse2.UnpackLow(sum0, sum1);

            // < 0, 0, sum3, sum2 >
            Vector128<int> sum23 = Sse2.UnpackLow(sum2, sum3);

            // < sum3, sum2, sum1, sum0 >
            return Sse.MoveLowToHigh(sum01.AsSingle(), sum23.AsSingle()).AsInt32();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<int> RoundShift(Vector128<int> value, Vector128<int> const64)
        {
            return Sse2.ShiftRightArithmetic(Sse2.Add(value, const64), FilterBits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<byte> PackUnsignedSaturate(Vector128<int> value, Vector128<int> zero)
        {
            return Sse2.PackUnsignedSaturate(Sse41.PackUnsignedSaturate(value, zero).AsInt16(), zero.AsInt16());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void ConvolveHorizSse41(
            byte* src,
            int srcStride,
            byte* dst,
            int dstStride,
            Array8<short>[] xFilters,
            int x0Q4,
            int w,
            int h)
        {
            Vector128<int> zero = Vector128<int>.Zero;
            Vector128<int> const64 = Vector128.Create(64);

            src -= (SubpelTaps / 2) - 1;

            fixed (Array8<short>* xFilter = xFilters)
            {
                Vector128<short> vfilter = Sse2.LoadVector128((short*)xFilter + ((uint)(x0Q4 & SubpelMask) * 8));

                for (ulong y = 0; y < (uint)h; ++y)
                {
                    ulong srcOffset = (uint)x0Q4 >> SubpelBits;
                    for (ulong x = 0; x < (uint)w; x += 4)
                    {
                        Vector128<short> vsrc0 = Sse41.ConvertToVector128Int16(&src[srcOffset + x]);
                        Vector128<short> vsrc1 = Sse41.ConvertToVector128Int16(&src[srcOffset + x + 1]);
                        Vector128<short> vsrc2 = Sse41.ConvertToVector128Int16(&src[srcOffset + x + 2]);
                        Vector128<short> vsrc3 = Sse41.ConvertToVector128Int16(&src[srcOffset + x + 3]);

                        Vector128<int> sum0123 = MultiplyAddAdjacent(vsrc0, vsrc1, vsrc2, vsrc3, vfilter, zero);

                        Sse.StoreScalar((float*)&dst[x],
                            PackUnsignedSaturate(RoundShift(sum0123, const64), zero).AsSingle());
                    }

                    src += srcStride;
                    dst += dstStride;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void ConvolveHoriz(
            byte* src,
            int srcStride,
            byte* dst,
            int dstStride,
            Array8<short>[] xFilters,
            int x0Q4,
            int xStepQ4,
            int w,
            int h)
        {
            if (Sse41.IsSupported && UseIntrinsics && xStepQ4 == 1 << SubpelBits)
            {
                ConvolveHorizSse41(src, srcStride, dst, dstStride, xFilters, x0Q4, w, h);
                return;
            }

            src -= (SubpelTaps / 2) - 1;

            for (int y = 0; y < h; ++y)
            {
                int xQ4 = x0Q4;
                for (int x = 0; x < w; ++x)
                {
                    byte* srcX = &src[xQ4 >> SubpelBits];
                    ref Array8<short> xFilter = ref xFilters[xQ4 & SubpelMask];
                    int sum = 0;
                    for (int k = 0; k < SubpelTaps; ++k)
                    {
                        sum += srcX[k] * xFilter[k];
                    }

                    dst[x] = BitUtils.ClipPixel(BitUtils.RoundPowerOfTwo(sum, FilterBits));
                    xQ4 += xStepQ4;
                }

                src += srcStride;
                dst += dstStride;
            }
        }

        private static unsafe void ConvolveAvgHoriz(
            byte* src,
            int srcStride,
            byte* dst,
            int dstStride,
            Array8<short>[] xFilters,
            int x0Q4,
            int xStepQ4,
            int w,
            int h)
        {
            src -= (SubpelTaps / 2) - 1;

            for (int y = 0; y < h; ++y)
            {
                int xQ4 = x0Q4;
                for (int x = 0; x < w; ++x)
                {
                    byte* srcX = &src[xQ4 >> SubpelBits];
                    ref Array8<short> xFilter = ref xFilters[xQ4 & SubpelMask];
                    int sum = 0;
                    for (int k = 0; k < SubpelTaps; ++k)
                    {
                        sum += srcX[k] * xFilter[k];
                    }

                    dst[x] = (byte)BitUtils.RoundPowerOfTwo(
                        dst[x] + BitUtils.ClipPixel(BitUtils.RoundPowerOfTwo(sum, FilterBits)), 1);
                    xQ4 += xStepQ4;
                }

                src += srcStride;
                dst += dstStride;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void ConvolveVertAvx2(
            byte* src,
            int srcStride,
            byte* dst,
            int dstStride,
            Array8<short>[] yFilters,
            int y0Q4,
            int w,
            int h)
        {
            Vector128<int> zero = Vector128<int>.Zero;
            Vector128<int> const64 = Vector128.Create(64);
            Vector256<int> indices = Vector256.Create(
                0,
                srcStride,
                srcStride * 2,
                srcStride * 3,
                srcStride * 4,
                srcStride * 5,
                srcStride * 6,
                srcStride * 7);

            src -= srcStride * ((SubpelTaps / 2) - 1);

            fixed (Array8<short>* yFilter = yFilters)
            {
                Vector128<short> vfilter = Sse2.LoadVector128((short*)yFilter + ((uint)(y0Q4 & SubpelMask) * 8));

                ulong srcBaseY = (uint)y0Q4 >> SubpelBits;
                for (ulong y = 0; y < (uint)h; ++y)
                {
                    ulong srcOffset = (srcBaseY + y) * (uint)srcStride;
                    for (ulong x = 0; x < (uint)w; x += 4)
                    {
                        Vector256<int> vsrc = Avx2.GatherVector256((uint*)&src[srcOffset + x], indices, 1).AsInt32();

                        Vector128<int> vsrcL = vsrc.GetLower();
                        Vector128<int> vsrcH = vsrc.GetUpper();

                        Vector128<byte> vsrcUnpck11 = Sse2.UnpackLow(vsrcL.AsByte(), vsrcH.AsByte());
                        Vector128<byte> vsrcUnpck12 = Sse2.UnpackHigh(vsrcL.AsByte(), vsrcH.AsByte());

                        Vector128<byte> vsrcUnpck21 = Sse2.UnpackLow(vsrcUnpck11, vsrcUnpck12);
                        Vector128<byte> vsrcUnpck22 = Sse2.UnpackHigh(vsrcUnpck11, vsrcUnpck12);

                        Vector128<byte> vsrc01 = Sse2.UnpackLow(vsrcUnpck21, vsrcUnpck22);
                        Vector128<byte> vsrc23 = Sse2.UnpackHigh(vsrcUnpck21, vsrcUnpck22);

                        Vector128<byte> vsrc11 = Sse.MoveHighToLow(vsrc01.AsSingle(), vsrc01.AsSingle()).AsByte();
                        Vector128<byte> vsrc33 = Sse.MoveHighToLow(vsrc23.AsSingle(), vsrc23.AsSingle()).AsByte();

                        Vector128<short> vsrc0 = Sse41.ConvertToVector128Int16(vsrc01);
                        Vector128<short> vsrc1 = Sse41.ConvertToVector128Int16(vsrc11);
                        Vector128<short> vsrc2 = Sse41.ConvertToVector128Int16(vsrc23);
                        Vector128<short> vsrc3 = Sse41.ConvertToVector128Int16(vsrc33);

                        Vector128<int> sum0123 = MultiplyAddAdjacent(vsrc0, vsrc1, vsrc2, vsrc3, vfilter, zero);

                        Sse.StoreScalar((float*)&dst[x],
                            PackUnsignedSaturate(RoundShift(sum0123, const64), zero).AsSingle());
                    }

                    dst += dstStride;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void ConvolveVert(
            byte* src,
            int srcStride,
            byte* dst,
            int dstStride,
            Array8<short>[] yFilters,
            int y0Q4,
            int yStepQ4,
            int w,
            int h)
        {
            if (Avx2.IsSupported && UseIntrinsics && yStepQ4 == 1 << SubpelBits)
            {
                ConvolveVertAvx2(src, srcStride, dst, dstStride, yFilters, y0Q4, w, h);
                return;
            }

            src -= srcStride * ((SubpelTaps / 2) - 1);

            for (int x = 0; x < w; ++x)
            {
                int yQ4 = y0Q4;
                for (int y = 0; y < h; ++y)
                {
                    byte* srcY = &src[(yQ4 >> SubpelBits) * srcStride];
                    ref Array8<short> yFilter = ref yFilters[yQ4 & SubpelMask];
                    int sum = 0;
                    for (int k = 0; k < SubpelTaps; ++k)
                    {
                        sum += srcY[k * srcStride] * yFilter[k];
                    }

                    dst[y * dstStride] = BitUtils.ClipPixel(BitUtils.RoundPowerOfTwo(sum, FilterBits));
                    yQ4 += yStepQ4;
                }

                ++src;
                ++dst;
            }
        }

        private static unsafe void ConvolveAvgVert(
            byte* src,
            int srcStride,
            byte* dst,
            int dstStride,
            Array8<short>[] yFilters,
            int y0Q4,
            int yStepQ4,
            int w,
            int h)
        {
            src -= srcStride * ((SubpelTaps / 2) - 1);

            for (int x = 0; x < w; ++x)
            {
                int yQ4 = y0Q4;
                for (int y = 0; y < h; ++y)
                {
                    byte* srcY = &src[(yQ4 >> SubpelBits) * srcStride];
                    ref Array8<short> yFilter = ref yFilters[yQ4 & SubpelMask];
                    int sum = 0;
                    for (int k = 0; k < SubpelTaps; ++k)
                    {
                        sum += srcY[k * srcStride] * yFilter[k];
                    }

                    dst[y * dstStride] = (byte)BitUtils.RoundPowerOfTwo(
                        dst[y * dstStride] + BitUtils.ClipPixel(BitUtils.RoundPowerOfTwo(sum, FilterBits)), 1);
                    yQ4 += yStepQ4;
                }

                ++src;
                ++dst;
            }
        }

        public static unsafe void Convolve8Horiz(
            byte* src,
            int srcStride,
            byte* dst,
            int dstStride,
            Array8<short>[] filter,
            int x0Q4,
            int xStepQ4,
            int y0Q4,
            int yStepQ4,
            int w,
            int h)
        {
            ConvolveHoriz(src, srcStride, dst, dstStride, filter, x0Q4, xStepQ4, w, h);
        }

        public static unsafe void Convolve8AvgHoriz(
            byte* src,
            int srcStride,
            byte* dst,
            int dstStride,
            Array8<short>[] filter,
            int x0Q4,
            int xStepQ4,
            int y0Q4,
            int yStepQ4,
            int w,
            int h)
        {
            ConvolveAvgHoriz(src, srcStride, dst, dstStride, filter, x0Q4, xStepQ4, w, h);
        }

        public static unsafe void Convolve8Vert(
            byte* src,
            int srcStride,
            byte* dst,
            int dstStride,
            Array8<short>[] filter,
            int x0Q4,
            int xStepQ4,
            int y0Q4,
            int yStepQ4,
            int w,
            int h)
        {
            ConvolveVert(src, srcStride, dst, dstStride, filter, y0Q4, yStepQ4, w, h);
        }

        public static unsafe void Convolve8AvgVert(
            byte* src,
            int srcStride,
            byte* dst,
            int dstStride,
            Array8<short>[] filter,
            int x0Q4,
            int xStepQ4,
            int y0Q4,
            int yStepQ4,
            int w,
            int h)
        {
            ConvolveAvgVert(src, srcStride, dst, dstStride, filter, y0Q4, yStepQ4, w, h);
        }

        [SkipLocalsInit]
        public static unsafe void Convolve8(
            byte* src,
            int srcStride,
            byte* dst,
            int dstStride,
            Array8<short>[] filter,
            int x0Q4,
            int xStepQ4,
            int y0Q4,
            int yStepQ4,
            int w,
            int h)
        {
            // Note: Fixed size intermediate buffer, temp, places limits on parameters.
            // 2d filtering proceeds in 2 steps:
            //   (1) Interpolate horizontally into an intermediate buffer, temp.
            //   (2) Interpolate temp vertically to derive the sub-pixel result.
            // Deriving the maximum number of rows in the temp buffer (135):
            // --Smallest scaling factor is x1/2 ==> yStepQ4 = 32 (Normative).
            // --Largest block size is 64x64 pixels.
            // --64 rows in the downscaled frame span a distance of (64 - 1) * 32 in the
            //   original frame (in 1/16th pixel units).
            // --Must round-up because block may be located at sub-pixel position.
            // --Require an additional SubpelTaps rows for the 8-tap filter tails.
            // --((64 - 1) * 32 + 15) >> 4 + 8 = 135.
            // When calling in frame scaling function, the smallest scaling factor is x1/4
            // ==> yStepQ4 = 64. Since w and h are at most 16, the temp buffer is still
            // big enough.
            byte* temp = stackalloc byte[64 * 135];
            int intermediateHeight = ((((h - 1) * yStepQ4) + y0Q4) >> SubpelBits) + SubpelTaps;

            Debug.Assert(w <= 64);
            Debug.Assert(h <= 64);
            Debug.Assert(yStepQ4 <= 32 || (yStepQ4 <= 64 && h <= 32));
            Debug.Assert(xStepQ4 <= 64);

            ConvolveHoriz(src - (srcStride * ((SubpelTaps / 2) - 1)), srcStride, temp, 64, filter, x0Q4, xStepQ4, w,
                intermediateHeight);
            ConvolveVert(temp + (64 * ((SubpelTaps / 2) - 1)), 64, dst, dstStride, filter, y0Q4, yStepQ4, w, h);
        }

        public static unsafe void Convolve8Avg(
            byte* src,
            int srcStride,
            byte* dst,
            int dstStride,
            Array8<short>[] filter,
            int x0Q4,
            int xStepQ4,
            int y0Q4,
            int yStepQ4,
            int w,
            int h)
        {
            // Fixed size intermediate buffer places limits on parameters.
            byte* temp = stackalloc byte[64 * 64];
            Debug.Assert(w <= 64);
            Debug.Assert(h <= 64);

            Convolve8(src, srcStride, temp, 64, filter, x0Q4, xStepQ4, y0Q4, yStepQ4, w, h);
            ConvolveAvg(temp, 64, dst, dstStride, null, 0, 0, 0, 0, w, h);
        }

        public static unsafe void ConvolveCopy(
            byte* src,
            int srcStride,
            byte* dst,
            int dstStride,
            Array8<short>[] filter,
            int x0Q4,
            int xStepQ4,
            int y0Q4,
            int yStepQ4,
            int w,
            int h)
        {
            int r;

            for (r = h; r > 0; --r)
            {
                MemoryUtil.Copy(dst, src, w);
                src += srcStride;
                dst += dstStride;
            }
        }

        public static unsafe void ConvolveAvg(
            byte* src,
            int srcStride,
            byte* dst,
            int dstStride,
            Array8<short>[] filter,
            int x0Q4,
            int xStepQ4,
            int y0Q4,
            int yStepQ4,
            int w,
            int h)
        {
            for (int y = 0; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    dst[x] = (byte)BitUtils.RoundPowerOfTwo(dst[x] + src[x], 1);
                }

                src += srcStride;
                dst += dstStride;
            }
        }

        public static unsafe void ScaledHoriz(
            byte* src,
            int srcStride,
            byte* dst,
            int dstStride,
            Array8<short>[] filter,
            int x0Q4,
            int xStepQ4,
            int y0Q4,
            int yStepQ4,
            int w,
            int h)
        {
            Convolve8Horiz(src, srcStride, dst, dstStride, filter, x0Q4, xStepQ4, y0Q4, yStepQ4, w, h);
        }

        public static unsafe void ScaledVert(
            byte* src,
            int srcStride,
            byte* dst,
            int dstStride,
            Array8<short>[] filter,
            int x0Q4,
            int xStepQ4,
            int y0Q4,
            int yStepQ4,
            int w,
            int h)
        {
            Convolve8Vert(src, srcStride, dst, dstStride, filter, x0Q4, xStepQ4, y0Q4, yStepQ4, w, h);
        }

        public static unsafe void Scaled2D(
            byte* src,
            int srcStride,
            byte* dst,
            int dstStride,
            Array8<short>[] filter,
            int x0Q4,
            int xStepQ4,
            int y0Q4,
            int yStepQ4,
            int w,
            int h)
        {
            Convolve8(src, srcStride, dst, dstStride, filter, x0Q4, xStepQ4, y0Q4, yStepQ4, w, h);
        }

        public static unsafe void ScaledAvgHoriz(
            byte* src,
            int srcStride,
            byte* dst,
            int dstStride,
            Array8<short>[] filter,
            int x0Q4,
            int xStepQ4,
            int y0Q4,
            int yStepQ4,
            int w,
            int h)
        {
            Convolve8AvgHoriz(src, srcStride, dst, dstStride, filter, x0Q4, xStepQ4, y0Q4, yStepQ4, w, h);
        }

        public static unsafe void ScaledAvgVert(
            byte* src,
            int srcStride,
            byte* dst,
            int dstStride,
            Array8<short>[] filter,
            int x0Q4,
            int xStepQ4,
            int y0Q4,
            int yStepQ4,
            int w,
            int h)
        {
            Convolve8AvgVert(src, srcStride, dst, dstStride, filter, x0Q4, xStepQ4, y0Q4, yStepQ4, w, h);
        }

        public static unsafe void ScaledAvg2D(
            byte* src,
            int srcStride,
            byte* dst,
            int dstStride,
            Array8<short>[] filter,
            int x0Q4,
            int xStepQ4,
            int y0Q4,
            int yStepQ4,
            int w,
            int h)
        {
            Convolve8Avg(src, srcStride, dst, dstStride, filter, x0Q4, xStepQ4, y0Q4, yStepQ4, w, h);
        }

        private static unsafe void HighbdConvolveHoriz(
            ushort* src,
            int srcStride,
            ushort* dst,
            int dstStride,
            Array8<short>[] xFilters,
            int x0Q4,
            int xStepQ4,
            int w,
            int h,
            int bd)
        {
            src -= (SubpelTaps / 2) - 1;

            for (int y = 0; y < h; ++y)
            {
                int xQ4 = x0Q4;
                for (int x = 0; x < w; ++x)
                {
                    ushort* srcX = &src[xQ4 >> SubpelBits];
                    ref Array8<short> xFilter = ref xFilters[xQ4 & SubpelMask];
                    int sum = 0;
                    for (int k = 0; k < SubpelTaps; ++k)
                    {
                        sum += srcX[k] * xFilter[k];
                    }

                    dst[x] = BitUtils.ClipPixelHighbd(BitUtils.RoundPowerOfTwo(sum, FilterBits), bd);
                    xQ4 += xStepQ4;
                }

                src += srcStride;
                dst += dstStride;
            }
        }

        private static unsafe void HighbdConvolveAvgHoriz(
            ushort* src,
            int srcStride,
            ushort* dst,
            int dstStride,
            Array8<short>[] xFilters,
            int x0Q4,
            int xStepQ4,
            int w,
            int h,
            int bd)
        {
            src -= (SubpelTaps / 2) - 1;

            for (int y = 0; y < h; ++y)
            {
                int xQ4 = x0Q4;
                for (int x = 0; x < w; ++x)
                {
                    ushort* srcX = &src[xQ4 >> SubpelBits];
                    ref Array8<short> xFilter = ref xFilters[xQ4 & SubpelMask];
                    int sum = 0;
                    for (int k = 0; k < SubpelTaps; ++k)
                    {
                        sum += srcX[k] * xFilter[k];
                    }

                    dst[x] = (ushort)BitUtils.RoundPowerOfTwo(
                        dst[x] + BitUtils.ClipPixelHighbd(BitUtils.RoundPowerOfTwo(sum, FilterBits), bd), 1);
                    xQ4 += xStepQ4;
                }

                src += srcStride;
                dst += dstStride;
            }
        }

        private static unsafe void HighbdConvolveVert(
            ushort* src,
            int srcStride,
            ushort* dst,
            int dstStride,
            Array8<short>[] yFilters,
            int y0Q4,
            int yStepQ4,
            int w,
            int h,
            int bd)
        {
            src -= srcStride * ((SubpelTaps / 2) - 1);

            for (int x = 0; x < w; ++x)
            {
                int yQ4 = y0Q4;
                for (int y = 0; y < h; ++y)
                {
                    ushort* srcY = &src[(yQ4 >> SubpelBits) * srcStride];
                    ref Array8<short> yFilter = ref yFilters[yQ4 & SubpelMask];
                    int sum = 0;
                    for (int k = 0; k < SubpelTaps; ++k)
                    {
                        sum += srcY[k * srcStride] * yFilter[k];
                    }

                    dst[y * dstStride] = BitUtils.ClipPixelHighbd(BitUtils.RoundPowerOfTwo(sum, FilterBits), bd);
                    yQ4 += yStepQ4;
                }

                ++src;
                ++dst;
            }
        }

        private static unsafe void HighConvolveAvgVert(
            ushort* src,
            int srcStride,
            ushort* dst,
            int dstStride,
            Array8<short>[] yFilters,
            int y0Q4,
            int yStepQ4,
            int w,
            int h,
            int bd)
        {
            src -= srcStride * ((SubpelTaps / 2) - 1);

            for (int x = 0; x < w; ++x)
            {
                int yQ4 = y0Q4;
                for (int y = 0; y < h; ++y)
                {
                    ushort* srcY = &src[(yQ4 >> SubpelBits) * srcStride];
                    ref Array8<short> yFilter = ref yFilters[yQ4 & SubpelMask];
                    int sum = 0;
                    for (int k = 0; k < SubpelTaps; ++k)
                    {
                        sum += srcY[k * srcStride] * yFilter[k];
                    }

                    dst[y * dstStride] = (ushort)BitUtils.RoundPowerOfTwo(
                        dst[y * dstStride] + BitUtils.ClipPixelHighbd(BitUtils.RoundPowerOfTwo(sum, FilterBits), bd),
                        1);
                    yQ4 += yStepQ4;
                }

                ++src;
                ++dst;
            }
        }

        private static unsafe void HighbdConvolve(
            ushort* src,
            int srcStride,
            ushort* dst,
            int dstStride,
            Array8<short>[] filter,
            int x0Q4,
            int xStepQ4,
            int y0Q4,
            int yStepQ4,
            int w,
            int h,
            int bd)
        {
            // Note: Fixed size intermediate buffer, temp, places limits on parameters.
            // 2d filtering proceeds in 2 steps:
            //   (1) Interpolate horizontally into an intermediate buffer, temp.
            //   (2) Interpolate temp vertically to derive the sub-pixel result.
            // Deriving the maximum number of rows in the temp buffer (135):
            // --Smallest scaling factor is x1/2 ==> yStepQ4 = 32 (Normative).
            // --Largest block size is 64x64 pixels.
            // --64 rows in the downscaled frame span a distance of (64 - 1) * 32 in the
            //   original frame (in 1/16th pixel units).
            // --Must round-up because block may be located at sub-pixel position.
            // --Require an additional SubpelTaps rows for the 8-tap filter tails.
            // --((64 - 1) * 32 + 15) >> 4 + 8 = 135.
            ushort* temp = stackalloc ushort[64 * 135];
            int intermediateHeight = ((((h - 1) * yStepQ4) + y0Q4) >> SubpelBits) + SubpelTaps;

            Debug.Assert(w <= 64);
            Debug.Assert(h <= 64);
            Debug.Assert(yStepQ4 <= 32);
            Debug.Assert(xStepQ4 <= 32);

            HighbdConvolveHoriz(src - (srcStride * ((SubpelTaps / 2) - 1)), srcStride, temp, 64, filter, x0Q4, xStepQ4,
                w, intermediateHeight, bd);
            HighbdConvolveVert(temp + (64 * ((SubpelTaps / 2) - 1)), 64, dst, dstStride, filter, y0Q4, yStepQ4, w, h,
                bd);
        }

        public static unsafe void HighbdConvolve8Horiz(
            ushort* src,
            int srcStride,
            ushort* dst,
            int dstStride,
            Array8<short>[] filter,
            int x0Q4,
            int xStepQ4,
            int y0Q4,
            int yStepQ4,
            int w,
            int h,
            int bd)
        {
            HighbdConvolveHoriz(src, srcStride, dst, dstStride, filter, x0Q4, xStepQ4, w, h, bd);
        }

        public static unsafe void HighbdConvolve8AvgHoriz(
            ushort* src,
            int srcStride,
            ushort* dst,
            int dstStride,
            Array8<short>[] filter,
            int x0Q4,
            int xStepQ4,
            int y0Q4,
            int yStepQ4,
            int w,
            int h,
            int bd)
        {
            HighbdConvolveAvgHoriz(src, srcStride, dst, dstStride, filter, x0Q4, xStepQ4, w, h, bd);
        }

        public static unsafe void HighbdConvolve8Vert(
            ushort* src,
            int srcStride,
            ushort* dst,
            int dstStride,
            Array8<short>[] filter,
            int x0Q4,
            int xStepQ4,
            int y0Q4,
            int yStepQ4,
            int w,
            int h,
            int bd)
        {
            HighbdConvolveVert(src, srcStride, dst, dstStride, filter, y0Q4, yStepQ4, w, h, bd);
        }

        public static unsafe void HighbdConvolve8AvgVert(
            ushort* src,
            int srcStride,
            ushort* dst,
            int dstStride,
            Array8<short>[] filter,
            int x0Q4,
            int xStepQ4,
            int y0Q4,
            int yStepQ4,
            int w,
            int h,
            int bd)
        {
            HighConvolveAvgVert(src, srcStride, dst, dstStride, filter, y0Q4, yStepQ4, w, h, bd);
        }

        public static unsafe void HighbdConvolve8(
            ushort* src,
            int srcStride,
            ushort* dst,
            int dstStride,
            Array8<short>[] filter,
            int x0Q4,
            int xStepQ4,
            int y0Q4,
            int yStepQ4,
            int w,
            int h,
            int bd)
        {
            HighbdConvolve(src, srcStride, dst, dstStride, filter, x0Q4, xStepQ4, y0Q4, yStepQ4, w, h, bd);
        }

        public static unsafe void HighbdConvolve8Avg(
            ushort* src,
            int srcStride,
            ushort* dst,
            int dstStride,
            Array8<short>[] filter,
            int x0Q4,
            int xStepQ4,
            int y0Q4,
            int yStepQ4,
            int w,
            int h,
            int bd)
        {
            // Fixed size intermediate buffer places limits on parameters.
            ushort* temp = stackalloc ushort[64 * 64];
            Debug.Assert(w <= 64);
            Debug.Assert(h <= 64);

            HighbdConvolve8(src, srcStride, temp, 64, filter, x0Q4, xStepQ4, y0Q4, yStepQ4, w, h, bd);
            HighbdConvolveAvg(temp, 64, dst, dstStride, null, 0, 0, 0, 0, w, h, bd);
        }

        public static unsafe void HighbdConvolveCopy(
            ushort* src,
            int srcStride,
            ushort* dst,
            int dstStride,
            Array8<short>[] filter,
            int x0Q4,
            int xStepQ4,
            int y0Q4,
            int yStepQ4,
            int w,
            int h,
            int bd)
        {
            int r;

            for (r = h; r > 0; --r)
            {
                MemoryUtil.Copy(dst, src, w);
                src += srcStride;
                dst += dstStride;
            }
        }

        public static unsafe void HighbdConvolveAvg(
            ushort* src,
            int srcStride,
            ushort* dst,
            int dstStride,
            Array8<short>[] filter,
            int x0Q4,
            int xStepQ4,
            int y0Q4,
            int yStepQ4,
            int w,
            int h,
            int bd)
        {
            for (int y = 0; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    dst[x] = (ushort)BitUtils.RoundPowerOfTwo(dst[x] + src[x], 1);
                }

                src += srcStride;
                dst += dstStride;
            }
        }
    }
}