using Ryujinx.Graphics.Nvdec.Vp9.Common;

namespace Ryujinx.Graphics.Nvdec.Vp9.Dsp
{
    internal static class IntraPred
    {
        private static unsafe ref byte Dst(byte* dst, int stride, int x, int y)
        {
            return ref dst[x + (y * stride)];
        }

        private static unsafe ref ushort Dst(ushort* dst, int stride, int x, int y)
        {
            return ref dst[x + (y * stride)];
        }

        private static byte Avg3(byte a, byte b, byte c)
        {
            return (byte)((a + (2 * b) + c + 2) >> 2);
        }

        private static ushort Avg3(ushort a, ushort b, ushort c)
        {
            return (ushort)((a + (2 * b) + c + 2) >> 2);
        }

        private static byte Avg2(byte a, byte b)
        {
            return (byte)((a + b + 1) >> 1);
        }

        private static ushort Avg2(ushort a, ushort b)
        {
            return (ushort)((a + b + 1) >> 1);
        }

        public static unsafe void D207Predictor8x8(byte* dst, int stride, byte* above, byte* left)
        {
            D207Predictor(dst, stride, 8, above, left);
        }

        public static unsafe void D207Predictor16x16(byte* dst, int stride, byte* above, byte* left)
        {
            D207Predictor(dst, stride, 16, above, left);
        }

        public static unsafe void D207Predictor32x32(byte* dst, int stride, byte* above, byte* left)
        {
            D207Predictor(dst, stride, 32, above, left);
        }

        private static unsafe void D207Predictor(byte* dst, int stride, int bs, byte* above, byte* left)
        {
            // First column
            for (int r = 0; r < bs - 1; ++r)
            {
                dst[r * stride] = Avg2(left[r], left[r + 1]);
            }

            dst[(bs - 1) * stride] = left[bs - 1];
            dst++;

            // Second column
            for (int r = 0; r < bs - 2; ++r)
            {
                dst[r * stride] = Avg3(left[r], left[r + 1], left[r + 2]);
            }

            dst[(bs - 2) * stride] = Avg3(left[bs - 2], left[bs - 1], left[bs - 1]);
            dst[(bs - 1) * stride] = left[bs - 1];
            dst++;

            // Rest of last row
            for (int c = 0; c < bs - 2; ++c)
            {
                dst[((bs - 1) * stride) + c] = left[bs - 1];
            }

            for (int r = bs - 2; r >= 0; --r)
            {
                for (int c = 0; c < bs - 2; ++c)
                {
                    dst[(r * stride) + c] = dst[((r + 1) * stride) + c - 2];
                }
            }
        }

        public static unsafe void D63Predictor8x8(byte* dst, int stride, byte* above, byte* left)
        {
            D63Predictor(dst, stride, 8, above, left);
        }

        public static unsafe void D63Predictor16x16(byte* dst, int stride, byte* above, byte* left)
        {
            D63Predictor(dst, stride, 16, above, left);
        }

        public static unsafe void D63Predictor32x32(byte* dst, int stride, byte* above, byte* left)
        {
            D63Predictor(dst, stride, 32, above, left);
        }

        private static unsafe void D63Predictor(byte* dst, int stride, int bs, byte* above, byte* left)
        {
            for (int c = 0; c < bs; ++c)
            {
                dst[c] = Avg2(above[c], above[c + 1]);
                dst[stride + c] = Avg3(above[c], above[c + 1], above[c + 2]);
            }

            for (int r = 2, size = bs - 2; r < bs; r += 2, --size)
            {
                MemoryUtil.Copy(dst + ((r + 0) * stride), dst + (r >> 1), size);
                MemoryUtil.Fill(dst + ((r + 0) * stride) + size, above[bs - 1], bs - size);
                MemoryUtil.Copy(dst + ((r + 1) * stride), dst + stride + (r >> 1), size);
                MemoryUtil.Fill(dst + ((r + 1) * stride) + size, above[bs - 1], bs - size);
            }
        }

        public static unsafe void D45Predictor8x8(byte* dst, int stride, byte* above, byte* left)
        {
            D45Predictor(dst, stride, 8, above, left);
        }

        public static unsafe void D45Predictor16x16(byte* dst, int stride, byte* above, byte* left)
        {
            D45Predictor(dst, stride, 16, above, left);
        }

        public static unsafe void D45Predictor32x32(byte* dst, int stride, byte* above, byte* left)
        {
            D45Predictor(dst, stride, 32, above, left);
        }

        private static unsafe void D45Predictor(byte* dst, int stride, int bs, byte* above, byte* left)
        {
            byte aboveRight = above[bs - 1];
            byte* dstRow0 = dst;

            for (int x = 0; x < bs - 1; ++x)
            {
                dst[x] = Avg3(above[x], above[x + 1], above[x + 2]);
            }

            dst[bs - 1] = aboveRight;
            dst += stride;
            for (int x = 1, size = bs - 2; x < bs; ++x, --size)
            {
                MemoryUtil.Copy(dst, dstRow0 + x, size);
                MemoryUtil.Fill(dst + size, aboveRight, x + 1);
                dst += stride;
            }
        }

        public static unsafe void D117Predictor8x8(byte* dst, int stride, byte* above, byte* left)
        {
            D117Predictor(dst, stride, 8, above, left);
        }

        public static unsafe void D117Predictor16x16(byte* dst, int stride, byte* above, byte* left)
        {
            D117Predictor(dst, stride, 16, above, left);
        }

        public static unsafe void D117Predictor32x32(byte* dst, int stride, byte* above, byte* left)
        {
            D117Predictor(dst, stride, 32, above, left);
        }

        private static unsafe void D117Predictor(byte* dst, int stride, int bs, byte* above, byte* left)
        {
            // First row
            for (int c = 0; c < bs; c++)
            {
                dst[c] = Avg2(above[c - 1], above[c]);
            }

            dst += stride;

            // Second row
            dst[0] = Avg3(left[0], above[-1], above[0]);
            for (int c = 1; c < bs; c++)
            {
                dst[c] = Avg3(above[c - 2], above[c - 1], above[c]);
            }

            dst += stride;

            // The rest of first col
            dst[0] = Avg3(above[-1], left[0], left[1]);
            for (int r = 3; r < bs; ++r)
            {
                dst[(r - 2) * stride] = Avg3(left[r - 3], left[r - 2], left[r - 1]);
            }

            // The rest of the block
            for (int r = 2; r < bs; ++r)
            {
                for (int c = 1; c < bs; c++)
                {
                    dst[c] = dst[(-2 * stride) + c - 1];
                }

                dst += stride;
            }
        }

        public static unsafe void D135Predictor8x8(byte* dst, int stride, byte* above, byte* left)
        {
            D135Predictor(dst, stride, 8, above, left);
        }

        public static unsafe void D135Predictor16x16(byte* dst, int stride, byte* above, byte* left)
        {
            D135Predictor(dst, stride, 16, above, left);
        }

        public static unsafe void D135Predictor32x32(byte* dst, int stride, byte* above, byte* left)
        {
            D135Predictor(dst, stride, 32, above, left);
        }

        private static unsafe void D135Predictor(byte* dst, int stride, int bs, byte* above, byte* left)
        {
            byte* border = stackalloc byte[32 + 32 - 1]; // outer border from bottom-left to top-right

            // Dst(dst, stride, bs, bs - 2)[0], i.e., border starting at bottom-left
            for (int i = 0; i < bs - 2; ++i)
            {
                border[i] = Avg3(left[bs - 3 - i], left[bs - 2 - i], left[bs - 1 - i]);
            }

            border[bs - 2] = Avg3(above[-1], left[0], left[1]);
            border[bs - 1] = Avg3(left[0], above[-1], above[0]);
            border[bs - 0] = Avg3(above[-1], above[0], above[1]);
            // dst[0][2, size), i.e., remaining top border ascending
            for (int i = 0; i < bs - 2; ++i)
            {
                border[bs + 1 + i] = Avg3(above[i], above[i + 1], above[i + 2]);
            }

            for (int i = 0; i < bs; ++i)
            {
                MemoryUtil.Copy(dst + (i * stride), border + bs - 1 - i, bs);
            }
        }

        public static unsafe void D153Predictor8x8(byte* dst, int stride, byte* above, byte* left)
        {
            D153Predictor(dst, stride, 8, above, left);
        }

        public static unsafe void D153Predictor16x16(byte* dst, int stride, byte* above, byte* left)
        {
            D153Predictor(dst, stride, 16, above, left);
        }

        public static unsafe void D153Predictor32x32(byte* dst, int stride, byte* above, byte* left)
        {
            D153Predictor(dst, stride, 32, above, left);
        }

        private static unsafe void D153Predictor(byte* dst, int stride, int bs, byte* above, byte* left)
        {
            dst[0] = Avg2(above[-1], left[0]);
            for (int r = 1; r < bs; r++)
            {
                dst[r * stride] = Avg2(left[r - 1], left[r]);
            }

            dst++;

            dst[0] = Avg3(left[0], above[-1], above[0]);
            dst[stride] = Avg3(above[-1], left[0], left[1]);
            for (int r = 2; r < bs; r++)
            {
                dst[r * stride] = Avg3(left[r - 2], left[r - 1], left[r]);
            }

            dst++;

            for (int c = 0; c < bs - 2; c++)
            {
                dst[c] = Avg3(above[c - 1], above[c], above[c + 1]);
            }

            dst += stride;

            for (int r = 1; r < bs; ++r)
            {
                for (int c = 0; c < bs - 2; c++)
                {
                    dst[c] = dst[-stride + c - 2];
                }

                dst += stride;
            }
        }

        public static unsafe void VPredictor4x4(byte* dst, int stride, byte* above, byte* left)
        {
            VPredictor(dst, stride, 4, above, left);
        }

        public static unsafe void VPredictor8x8(byte* dst, int stride, byte* above, byte* left)
        {
            VPredictor(dst, stride, 8, above, left);
        }

        public static unsafe void VPredictor16x16(byte* dst, int stride, byte* above, byte* left)
        {
            VPredictor(dst, stride, 16, above, left);
        }

        public static unsafe void VPredictor32x32(byte* dst, int stride, byte* above, byte* left)
        {
            VPredictor(dst, stride, 32, above, left);
        }

        private static unsafe void VPredictor(byte* dst, int stride, int bs, byte* above, byte* left)
        {
            for (int r = 0; r < bs; r++)
            {
                MemoryUtil.Copy(dst, above, bs);
                dst += stride;
            }
        }

        public static unsafe void HPredictor4x4(byte* dst, int stride, byte* above, byte* left)
        {
            HPredictor(dst, stride, 4, above, left);
        }

        public static unsafe void HPredictor8x8(byte* dst, int stride, byte* above, byte* left)
        {
            HPredictor(dst, stride, 8, above, left);
        }

        public static unsafe void HPredictor16x16(byte* dst, int stride, byte* above, byte* left)
        {
            HPredictor(dst, stride, 16, above, left);
        }

        public static unsafe void HPredictor32x32(byte* dst, int stride, byte* above, byte* left)
        {
            HPredictor(dst, stride, 32, above, left);
        }

        private static unsafe void HPredictor(byte* dst, int stride, int bs, byte* above, byte* left)
        {
            for (int r = 0; r < bs; r++)
            {
                MemoryUtil.Fill(dst, left[r], bs);
                dst += stride;
            }
        }

        public static unsafe void TmPredictor4x4(byte* dst, int stride, byte* above, byte* left)
        {
            TmPredictor(dst, stride, 4, above, left);
        }

        public static unsafe void TmPredictor8x8(byte* dst, int stride, byte* above, byte* left)
        {
            TmPredictor(dst, stride, 8, above, left);
        }

        public static unsafe void TmPredictor16x16(byte* dst, int stride, byte* above, byte* left)
        {
            TmPredictor(dst, stride, 16, above, left);
        }

        public static unsafe void TmPredictor32x32(byte* dst, int stride, byte* above, byte* left)
        {
            TmPredictor(dst, stride, 32, above, left);
        }

        private static unsafe void TmPredictor(byte* dst, int stride, int bs, byte* above, byte* left)
        {
            int yTopLeft = above[-1];

            for (int r = 0; r < bs; r++)
            {
                for (int c = 0; c < bs; c++)
                {
                    dst[c] = BitUtils.ClipPixel(left[r] + above[c] - yTopLeft);
                }

                dst += stride;
            }
        }

        public static unsafe void Dc128Predictor4x4(byte* dst, int stride, byte* above, byte* left)
        {
            Dc128Predictor(dst, stride, 4, above, left);
        }

        public static unsafe void Dc128Predictor8x8(byte* dst, int stride, byte* above, byte* left)
        {
            Dc128Predictor(dst, stride, 8, above, left);
        }

        public static unsafe void Dc128Predictor16x16(byte* dst, int stride, byte* above, byte* left)
        {
            Dc128Predictor(dst, stride, 16, above, left);
        }

        public static unsafe void Dc128Predictor32x32(byte* dst, int stride, byte* above, byte* left)
        {
            Dc128Predictor(dst, stride, 32, above, left);
        }

        private static unsafe void Dc128Predictor(byte* dst, int stride, int bs, byte* above, byte* left)
        {
            for (int r = 0; r < bs; r++)
            {
                MemoryUtil.Fill(dst, (byte)128, bs);
                dst += stride;
            }
        }

        public static unsafe void DcLeftPredictor4x4(byte* dst, int stride, byte* above, byte* left)
        {
            DcLeftPredictor(dst, stride, 4, above, left);
        }

        public static unsafe void DcLeftPredictor8x8(byte* dst, int stride, byte* above, byte* left)
        {
            DcLeftPredictor(dst, stride, 8, above, left);
        }

        public static unsafe void DcLeftPredictor16x16(byte* dst, int stride, byte* above, byte* left)
        {
            DcLeftPredictor(dst, stride, 16, above, left);
        }

        public static unsafe void DcLeftPredictor32x32(byte* dst, int stride, byte* above, byte* left)
        {
            DcLeftPredictor(dst, stride, 32, above, left);
        }

        private static unsafe void DcLeftPredictor(byte* dst, int stride, int bs, byte* above, byte* left)
        {
            int expectedDc, sum = 0;

            for (int i = 0; i < bs; i++)
            {
                sum += left[i];
            }

            expectedDc = (sum + (bs >> 1)) / bs;

            for (int r = 0; r < bs; r++)
            {
                MemoryUtil.Fill(dst, (byte)expectedDc, bs);
                dst += stride;
            }
        }

        public static unsafe void DcTopPredictor4x4(byte* dst, int stride, byte* above, byte* left)
        {
            DcTopPredictor(dst, stride, 4, above, left);
        }

        public static unsafe void DcTopPredictor8x8(byte* dst, int stride, byte* above, byte* left)
        {
            DcTopPredictor(dst, stride, 8, above, left);
        }

        public static unsafe void DcTopPredictor16x16(byte* dst, int stride, byte* above, byte* left)
        {
            DcTopPredictor(dst, stride, 16, above, left);
        }

        public static unsafe void DcTopPredictor32x32(byte* dst, int stride, byte* above, byte* left)
        {
            DcTopPredictor(dst, stride, 32, above, left);
        }

        private static unsafe void DcTopPredictor(byte* dst, int stride, int bs, byte* above, byte* left)
        {
            int expectedDc, sum = 0;

            for (int i = 0; i < bs; i++)
            {
                sum += above[i];
            }

            expectedDc = (sum + (bs >> 1)) / bs;

            for (int r = 0; r < bs; r++)
            {
                MemoryUtil.Fill(dst, (byte)expectedDc, bs);
                dst += stride;
            }
        }

        public static unsafe void DcPredictor4x4(byte* dst, int stride, byte* above, byte* left)
        {
            DcPredictor(dst, stride, 4, above, left);
        }

        public static unsafe void DcPredictor8x8(byte* dst, int stride, byte* above, byte* left)
        {
            DcPredictor(dst, stride, 8, above, left);
        }

        public static unsafe void DcPredictor16x16(byte* dst, int stride, byte* above, byte* left)
        {
            DcPredictor(dst, stride, 16, above, left);
        }

        public static unsafe void DcPredictor32x32(byte* dst, int stride, byte* above, byte* left)
        {
            DcPredictor(dst, stride, 32, above, left);
        }

        private static unsafe void DcPredictor(byte* dst, int stride, int bs, byte* above, byte* left)
        {
            int expectedDc, sum = 0;
            int count = 2 * bs;

            for (int i = 0; i < bs; i++)
            {
                sum += above[i];
                sum += left[i];
            }

            expectedDc = (sum + (count >> 1)) / count;

            for (int r = 0; r < bs; r++)
            {
                MemoryUtil.Fill(dst, (byte)expectedDc, bs);
                dst += stride;
            }
        }

        public static unsafe void HePredictor4x4(byte* dst, int stride, byte* above, byte* left)
        {
            byte h = above[-1];
            byte I = left[0];
            byte j = left[1];
            byte k = left[2];
            byte l = left[3];

            MemoryUtil.Fill(dst + (stride * 0), Avg3(h, I, j), 4);
            MemoryUtil.Fill(dst + (stride * 1), Avg3(I, j, k), 4);
            MemoryUtil.Fill(dst + (stride * 2), Avg3(j, k, l), 4);
            MemoryUtil.Fill(dst + (stride * 3), Avg3(k, l, l), 4);
        }

        public static unsafe void VePredictor4x4(byte* dst, int stride, byte* above, byte* left)
        {
            byte h = above[-1];
            byte I = above[0];
            byte j = above[1];
            byte k = above[2];
            byte l = above[3];
            byte m = above[4];

            dst[0] = Avg3(h, I, j);
            dst[1] = Avg3(I, j, k);
            dst[2] = Avg3(j, k, l);
            dst[3] = Avg3(k, l, m);
            MemoryUtil.Copy(dst + (stride * 1), dst, 4);
            MemoryUtil.Copy(dst + (stride * 2), dst, 4);
            MemoryUtil.Copy(dst + (stride * 3), dst, 4);
        }

        public static unsafe void D207Predictor4x4(byte* dst, int stride, byte* above, byte* left)
        {
            byte I = left[0];
            byte j = left[1];
            byte k = left[2];
            byte l = left[3];
            Dst(dst, stride, 0, 0) = Avg2(I, j);
            Dst(dst, stride, 2, 0) = Dst(dst, stride, 0, 1) = Avg2(j, k);
            Dst(dst, stride, 2, 1) = Dst(dst, stride, 0, 2) = Avg2(k, l);
            Dst(dst, stride, 1, 0) = Avg3(I, j, k);
            Dst(dst, stride, 3, 0) = Dst(dst, stride, 1, 1) = Avg3(j, k, l);
            Dst(dst, stride, 3, 1) = Dst(dst, stride, 1, 2) = Avg3(k, l, l);
            Dst(dst, stride, 3, 2) = Dst(dst, stride, 2, 2) = Dst(dst, stride, 0, 3) =
                Dst(dst, stride, 1, 3) = Dst(dst, stride, 2, 3) = Dst(dst, stride, 3, 3) = l;
        }

        public static unsafe void D63Predictor4x4(byte* dst, int stride, byte* above, byte* left)
        {
            byte a = above[0];
            byte b = above[1];
            byte c = above[2];
            byte d = above[3];
            byte e = above[4];
            byte f = above[5];
            byte g = above[6];
            Dst(dst, stride, 0, 0) = Avg2(a, b);
            Dst(dst, stride, 1, 0) = Dst(dst, stride, 0, 2) = Avg2(b, c);
            Dst(dst, stride, 2, 0) = Dst(dst, stride, 1, 2) = Avg2(c, d);
            Dst(dst, stride, 3, 0) = Dst(dst, stride, 2, 2) = Avg2(d, e);
            Dst(dst, stride, 3, 2) = Avg2(e, f); // Differs from vp8

            Dst(dst, stride, 0, 1) = Avg3(a, b, c);
            Dst(dst, stride, 1, 1) = Dst(dst, stride, 0, 3) = Avg3(b, c, d);
            Dst(dst, stride, 2, 1) = Dst(dst, stride, 1, 3) = Avg3(c, d, e);
            Dst(dst, stride, 3, 1) = Dst(dst, stride, 2, 3) = Avg3(d, e, f);
            Dst(dst, stride, 3, 3) = Avg3(e, f, g); // Differs from vp8
        }

        public static unsafe void D63EPredictor4x4(byte* dst, int stride, byte* above, byte* left)
        {
            byte a = above[0];
            byte b = above[1];
            byte c = above[2];
            byte d = above[3];
            byte e = above[4];
            byte f = above[5];
            byte g = above[6];
            byte h = above[7];
            Dst(dst, stride, 0, 0) = Avg2(a, b);
            Dst(dst, stride, 1, 0) = Dst(dst, stride, 0, 2) = Avg2(b, c);
            Dst(dst, stride, 2, 0) = Dst(dst, stride, 1, 2) = Avg2(c, d);
            Dst(dst, stride, 3, 0) = Dst(dst, stride, 2, 2) = Avg2(d, e);
            Dst(dst, stride, 3, 2) = Avg3(e, f, g);

            Dst(dst, stride, 0, 1) = Avg3(a, b, c);
            Dst(dst, stride, 1, 1) = Dst(dst, stride, 0, 3) = Avg3(b, c, d);
            Dst(dst, stride, 2, 1) = Dst(dst, stride, 1, 3) = Avg3(c, d, e);
            Dst(dst, stride, 3, 1) = Dst(dst, stride, 2, 3) = Avg3(d, e, f);
            Dst(dst, stride, 3, 3) = Avg3(f, g, h);
        }

        public static unsafe void D45Predictor4x4(byte* dst, int stride, byte* above, byte* left)
        {
            byte a = above[0];
            byte b = above[1];
            byte c = above[2];
            byte d = above[3];
            byte e = above[4];
            byte f = above[5];
            byte g = above[6];
            byte h = above[7];
            Dst(dst, stride, 0, 0) = Avg3(a, b, c);
            Dst(dst, stride, 1, 0) = Dst(dst, stride, 0, 1) = Avg3(b, c, d);
            Dst(dst, stride, 2, 0) = Dst(dst, stride, 1, 1) = Dst(dst, stride, 0, 2) = Avg3(c, d, e);
            Dst(dst, stride, 3, 0) =
                Dst(dst, stride, 2, 1) = Dst(dst, stride, 1, 2) = Dst(dst, stride, 0, 3) = Avg3(d, e, f);
            Dst(dst, stride, 3, 1) = Dst(dst, stride, 2, 2) = Dst(dst, stride, 1, 3) = Avg3(e, f, g);
            Dst(dst, stride, 3, 2) = Dst(dst, stride, 2, 3) = Avg3(f, g, h);
            Dst(dst, stride, 3, 3) = h; // differs from vp8
        }

        public static unsafe void D45EPredictor4x4(byte* dst, int stride, byte* above, byte* left)
        {
            byte a = above[0];
            byte b = above[1];
            byte c = above[2];
            byte d = above[3];
            byte e = above[4];
            byte f = above[5];
            byte g = above[6];
            byte h = above[7];
            Dst(dst, stride, 0, 0) = Avg3(a, b, c);
            Dst(dst, stride, 1, 0) = Dst(dst, stride, 0, 1) = Avg3(b, c, d);
            Dst(dst, stride, 2, 0) = Dst(dst, stride, 1, 1) = Dst(dst, stride, 0, 2) = Avg3(c, d, e);
            Dst(dst, stride, 3, 0) =
                Dst(dst, stride, 2, 1) = Dst(dst, stride, 1, 2) = Dst(dst, stride, 0, 3) = Avg3(d, e, f);
            Dst(dst, stride, 3, 1) = Dst(dst, stride, 2, 2) = Dst(dst, stride, 1, 3) = Avg3(e, f, g);
            Dst(dst, stride, 3, 2) = Dst(dst, stride, 2, 3) = Avg3(f, g, h);
            Dst(dst, stride, 3, 3) = Avg3(g, h, h);
        }

        public static unsafe void D117Predictor4x4(byte* dst, int stride, byte* above, byte* left)
        {
            byte I = left[0];
            byte j = left[1];
            byte k = left[2];
            byte x = above[-1];
            byte a = above[0];
            byte b = above[1];
            byte c = above[2];
            byte d = above[3];
            Dst(dst, stride, 0, 0) = Dst(dst, stride, 1, 2) = Avg2(x, a);
            Dst(dst, stride, 1, 0) = Dst(dst, stride, 2, 2) = Avg2(a, b);
            Dst(dst, stride, 2, 0) = Dst(dst, stride, 3, 2) = Avg2(b, c);
            Dst(dst, stride, 3, 0) = Avg2(c, d);

            Dst(dst, stride, 0, 3) = Avg3(k, j, I);
            Dst(dst, stride, 0, 2) = Avg3(j, I, x);
            Dst(dst, stride, 0, 1) = Dst(dst, stride, 1, 3) = Avg3(I, x, a);
            Dst(dst, stride, 1, 1) = Dst(dst, stride, 2, 3) = Avg3(x, a, b);
            Dst(dst, stride, 2, 1) = Dst(dst, stride, 3, 3) = Avg3(a, b, c);
            Dst(dst, stride, 3, 1) = Avg3(b, c, d);
        }

        public static unsafe void D135Predictor4x4(byte* dst, int stride, byte* above, byte* left)
        {
            byte I = left[0];
            byte j = left[1];
            byte k = left[2];
            byte l = left[3];
            byte x = above[-1];
            byte a = above[0];
            byte b = above[1];
            byte c = above[2];
            byte d = above[3];
            Dst(dst, stride, 0, 3) = Avg3(j, k, l);
            Dst(dst, stride, 1, 3) = Dst(dst, stride, 0, 2) = Avg3(I, j, k);
            Dst(dst, stride, 2, 3) = Dst(dst, stride, 1, 2) = Dst(dst, stride, 0, 1) = Avg3(x, I, j);
            Dst(dst, stride, 3, 3) =
                Dst(dst, stride, 2, 2) = Dst(dst, stride, 1, 1) = Dst(dst, stride, 0, 0) = Avg3(a, x, I);
            Dst(dst, stride, 3, 2) = Dst(dst, stride, 2, 1) = Dst(dst, stride, 1, 0) = Avg3(b, a, x);
            Dst(dst, stride, 3, 1) = Dst(dst, stride, 2, 0) = Avg3(c, b, a);
            Dst(dst, stride, 3, 0) = Avg3(d, c, b);
        }

        public static unsafe void D153Predictor4x4(byte* dst, int stride, byte* above, byte* left)
        {
            byte I = left[0];
            byte j = left[1];
            byte k = left[2];
            byte l = left[3];
            byte x = above[-1];
            byte a = above[0];
            byte b = above[1];
            byte c = above[2];
            Dst(dst, stride, 0, 0) = Dst(dst, stride, 2, 1) = Avg2(I, x);
            Dst(dst, stride, 0, 1) = Dst(dst, stride, 2, 2) = Avg2(j, I);
            Dst(dst, stride, 0, 2) = Dst(dst, stride, 2, 3) = Avg2(k, j);
            Dst(dst, stride, 0, 3) = Avg2(l, k);

            Dst(dst, stride, 3, 0) = Avg3(a, b, c);
            Dst(dst, stride, 2, 0) = Avg3(x, a, b);
            Dst(dst, stride, 1, 0) = Dst(dst, stride, 3, 1) = Avg3(I, x, a);
            Dst(dst, stride, 1, 1) = Dst(dst, stride, 3, 2) = Avg3(j, I, x);
            Dst(dst, stride, 1, 2) = Dst(dst, stride, 3, 3) = Avg3(k, j, I);
            Dst(dst, stride, 1, 3) = Avg3(l, k, j);
        }

        public static unsafe void HighbdD207Predictor8x8(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdD207Predictor(dst, stride, 8, above, left, bd);
        }

        public static unsafe void HighbdD207Predictor16x16(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdD207Predictor(dst, stride, 16, above, left, bd);
        }

        public static unsafe void HighbdD207Predictor32x32(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdD207Predictor(dst, stride, 32, above, left, bd);
        }

        private static unsafe void HighbdD207Predictor(ushort* dst, int stride, int bs, ushort* above, ushort* left,
            int bd)
        {
            // First column.
            for (int r = 0; r < bs - 1; ++r)
            {
                dst[r * stride] = Avg2(left[r], left[r + 1]);
            }

            dst[(bs - 1) * stride] = left[bs - 1];
            dst++;

            // Second column.
            for (int r = 0; r < bs - 2; ++r)
            {
                dst[r * stride] = Avg3(left[r], left[r + 1], left[r + 2]);
            }

            dst[(bs - 2) * stride] = Avg3(left[bs - 2], left[bs - 1], left[bs - 1]);
            dst[(bs - 1) * stride] = left[bs - 1];
            dst++;

            // Rest of last row.
            for (int c = 0; c < bs - 2; ++c)
            {
                dst[((bs - 1) * stride) + c] = left[bs - 1];
            }

            for (int r = bs - 2; r >= 0; --r)
            {
                for (int c = 0; c < bs - 2; ++c)
                {
                    dst[(r * stride) + c] = dst[((r + 1) * stride) + c - 2];
                }
            }
        }

        public static unsafe void HighbdD63Predictor8x8(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdD63Predictor(dst, stride, 8, above, left, bd);
        }

        public static unsafe void HighbdD63Predictor16x16(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdD63Predictor(dst, stride, 16, above, left, bd);
        }

        public static unsafe void HighbdD63Predictor32x32(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdD63Predictor(dst, stride, 32, above, left, bd);
        }

        private static unsafe void HighbdD63Predictor(ushort* dst, int stride, int bs, ushort* above, ushort* left,
            int bd)
        {
            for (int c = 0; c < bs; ++c)
            {
                dst[c] = Avg2(above[c], above[c + 1]);
                dst[stride + c] = Avg3(above[c], above[c + 1], above[c + 2]);
            }

            for (int r = 2, size = bs - 2; r < bs; r += 2, --size)
            {
                MemoryUtil.Copy(dst + ((r + 0) * stride), dst + (r >> 1), size);
                MemoryUtil.Fill(dst + ((r + 0) * stride) + size, above[bs - 1], bs - size);
                MemoryUtil.Copy(dst + ((r + 1) * stride), dst + stride + (r >> 1), size);
                MemoryUtil.Fill(dst + ((r + 1) * stride) + size, above[bs - 1], bs - size);
            }
        }

        public static unsafe void HighbdD45Predictor8x8(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdD45Predictor(dst, stride, 8, above, left, bd);
        }

        public static unsafe void HighbdD45Predictor16x16(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdD45Predictor(dst, stride, 16, above, left, bd);
        }

        public static unsafe void HighbdD45Predictor32x32(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdD45Predictor(dst, stride, 32, above, left, bd);
        }

        private static unsafe void HighbdD45Predictor(ushort* dst, int stride, int bs, ushort* above, ushort* left,
            int bd)
        {
            ushort aboveRight = above[bs - 1];
            ushort* dstRow0 = dst;

            for (int x = 0; x < bs - 1; ++x)
            {
                dst[x] = Avg3(above[x], above[x + 1], above[x + 2]);
            }

            dst[bs - 1] = aboveRight;
            dst += stride;
            for (int x = 1, size = bs - 2; x < bs; ++x, --size)
            {
                MemoryUtil.Copy(dst, dstRow0 + x, size);
                MemoryUtil.Fill(dst + size, aboveRight, x + 1);
                dst += stride;
            }
        }

        public static unsafe void HighbdD117Predictor8x8(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdD117Predictor(dst, stride, 8, above, left, bd);
        }

        public static unsafe void HighbdD117Predictor16x16(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdD117Predictor(dst, stride, 16, above, left, bd);
        }

        public static unsafe void HighbdD117Predictor32x32(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdD117Predictor(dst, stride, 32, above, left, bd);
        }

        private static unsafe void HighbdD117Predictor(ushort* dst, int stride, int bs, ushort* above, ushort* left,
            int bd)
        {
            // First row
            for (int c = 0; c < bs; c++)
            {
                dst[c] = Avg2(above[c - 1], above[c]);
            }

            dst += stride;

            // Second row
            dst[0] = Avg3(left[0], above[-1], above[0]);
            for (int c = 1; c < bs; c++)
            {
                dst[c] = Avg3(above[c - 2], above[c - 1], above[c]);
            }

            dst += stride;

            // The rest of first col
            dst[0] = Avg3(above[-1], left[0], left[1]);
            for (int r = 3; r < bs; ++r)
            {
                dst[(r - 2) * stride] = Avg3(left[r - 3], left[r - 2], left[r - 1]);
            }

            // The rest of the block
            for (int r = 2; r < bs; ++r)
            {
                for (int c = 1; c < bs; c++)
                {
                    dst[c] = dst[(-2 * stride) + c - 1];
                }

                dst += stride;
            }
        }

        public static unsafe void HighbdD135Predictor8x8(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdD135Predictor(dst, stride, 8, above, left, bd);
        }

        public static unsafe void HighbdD135Predictor16x16(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdD135Predictor(dst, stride, 16, above, left, bd);
        }

        public static unsafe void HighbdD135Predictor32x32(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdD135Predictor(dst, stride, 32, above, left, bd);
        }

        private static unsafe void HighbdD135Predictor(ushort* dst, int stride, int bs, ushort* above, ushort* left,
            int bd)
        {
            ushort* border = stackalloc ushort[32 + 32 - 1]; // Outer border from bottom-left to top-right

            // Dst(dst, stride, bs, bs - 2)[0], i.e., border starting at bottom-left
            for (int i = 0; i < bs - 2; ++i)
            {
                border[i] = Avg3(left[bs - 3 - i], left[bs - 2 - i], left[bs - 1 - i]);
            }

            border[bs - 2] = Avg3(above[-1], left[0], left[1]);
            border[bs - 1] = Avg3(left[0], above[-1], above[0]);
            border[bs - 0] = Avg3(above[-1], above[0], above[1]);
            // dst[0][2, size), i.e., remaining top border ascending
            for (int i = 0; i < bs - 2; ++i)
            {
                border[bs + 1 + i] = Avg3(above[i], above[i + 1], above[i + 2]);
            }

            for (int i = 0; i < bs; ++i)
            {
                MemoryUtil.Copy(dst + (i * stride), border + bs - 1 - i, bs);
            }
        }

        public static unsafe void HighbdD153Predictor8x8(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdD153Predictor(dst, stride, 8, above, left, bd);
        }

        public static unsafe void HighbdD153Predictor16x16(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdD153Predictor(dst, stride, 16, above, left, bd);
        }

        public static unsafe void HighbdD153Predictor32x32(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdD153Predictor(dst, stride, 32, above, left, bd);
        }

        private static unsafe void HighbdD153Predictor(ushort* dst, int stride, int bs, ushort* above, ushort* left,
            int bd)
        {
            dst[0] = Avg2(above[-1], left[0]);
            for (int r = 1; r < bs; r++)
            {
                dst[r * stride] = Avg2(left[r - 1], left[r]);
            }

            dst++;

            dst[0] = Avg3(left[0], above[-1], above[0]);
            dst[stride] = Avg3(above[-1], left[0], left[1]);
            for (int r = 2; r < bs; r++)
            {
                dst[r * stride] = Avg3(left[r - 2], left[r - 1], left[r]);
            }

            dst++;

            for (int c = 0; c < bs - 2; c++)
            {
                dst[c] = Avg3(above[c - 1], above[c], above[c + 1]);
            }

            dst += stride;

            for (int r = 1; r < bs; ++r)
            {
                for (int c = 0; c < bs - 2; c++)
                {
                    dst[c] = dst[-stride + c - 2];
                }

                dst += stride;
            }
        }

        public static unsafe void HighbdVPredictor4x4(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdVPredictor(dst, stride, 4, above, left, bd);
        }

        public static unsafe void HighbdVPredictor8x8(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdVPredictor(dst, stride, 8, above, left, bd);
        }

        public static unsafe void HighbdVPredictor16x16(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdVPredictor(dst, stride, 16, above, left, bd);
        }

        public static unsafe void HighbdVPredictor32x32(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdVPredictor(dst, stride, 32, above, left, bd);
        }

        private static unsafe void HighbdVPredictor(ushort* dst, int stride, int bs, ushort* above, ushort* left,
            int bd)
        {
            for (int r = 0; r < bs; r++)
            {
                MemoryUtil.Copy(dst, above, bs);
                dst += stride;
            }
        }

        public static unsafe void HighbdHPredictor4x4(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdHPredictor(dst, stride, 4, above, left, bd);
        }

        public static unsafe void HighbdHPredictor8x8(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdHPredictor(dst, stride, 8, above, left, bd);
        }

        public static unsafe void HighbdHPredictor16x16(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdHPredictor(dst, stride, 16, above, left, bd);
        }

        public static unsafe void HighbdHPredictor32x32(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdHPredictor(dst, stride, 32, above, left, bd);
        }

        private static unsafe void HighbdHPredictor(ushort* dst, int stride, int bs, ushort* above, ushort* left,
            int bd)
        {
            for (int r = 0; r < bs; r++)
            {
                MemoryUtil.Fill(dst, left[r], bs);
                dst += stride;
            }
        }

        public static unsafe void HighbdTmPredictor4x4(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdTmPredictor(dst, stride, 4, above, left, bd);
        }

        public static unsafe void HighbdTmPredictor8x8(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdTmPredictor(dst, stride, 8, above, left, bd);
        }

        public static unsafe void HighbdTmPredictor16x16(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdTmPredictor(dst, stride, 16, above, left, bd);
        }

        public static unsafe void HighbdTmPredictor32x32(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdTmPredictor(dst, stride, 32, above, left, bd);
        }

        private static unsafe void HighbdTmPredictor(ushort* dst, int stride, int bs, ushort* above, ushort* left,
            int bd)
        {
            int yTopLeft = above[-1];

            for (int r = 0; r < bs; r++)
            {
                for (int c = 0; c < bs; c++)
                {
                    dst[c] = BitUtils.ClipPixelHighbd(left[r] + above[c] - yTopLeft, bd);
                }

                dst += stride;
            }
        }

        public static unsafe void HighbdDc128Predictor4x4(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdDc128Predictor(dst, stride, 4, above, left, bd);
        }

        public static unsafe void HighbdDc128Predictor8x8(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdDc128Predictor(dst, stride, 8, above, left, bd);
        }

        public static unsafe void HighbdDc128Predictor16x16(ushort* dst, int stride, ushort* above, ushort* left,
            int bd)
        {
            HighbdDc128Predictor(dst, stride, 16, above, left, bd);
        }

        public static unsafe void HighbdDc128Predictor32x32(ushort* dst, int stride, ushort* above, ushort* left,
            int bd)
        {
            HighbdDc128Predictor(dst, stride, 32, above, left, bd);
        }

        private static unsafe void HighbdDc128Predictor(ushort* dst, int stride, int bs, ushort* above, ushort* left,
            int bd)
        {
            for (int r = 0; r < bs; r++)
            {
                MemoryUtil.Fill(dst, (ushort)(128 << (bd - 8)), bs);
                dst += stride;
            }
        }

        public static unsafe void HighbdDcLeftPredictor4x4(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdDcLeftPredictor(dst, stride, 4, above, left, bd);
        }

        public static unsafe void HighbdDcLeftPredictor8x8(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdDcLeftPredictor(dst, stride, 8, above, left, bd);
        }

        public static unsafe void HighbdDcLeftPredictor16x16(ushort* dst, int stride, ushort* above, ushort* left,
            int bd)
        {
            HighbdDcLeftPredictor(dst, stride, 16, above, left, bd);
        }

        public static unsafe void HighbdDcLeftPredictor32x32(ushort* dst, int stride, ushort* above, ushort* left,
            int bd)
        {
            HighbdDcLeftPredictor(dst, stride, 32, above, left, bd);
        }

        private static unsafe void HighbdDcLeftPredictor(ushort* dst, int stride, int bs, ushort* above, ushort* left,
            int bd)
        {
            int expectedDc, sum = 0;

            for (int i = 0; i < bs; i++)
            {
                sum += left[i];
            }

            expectedDc = (sum + (bs >> 1)) / bs;

            for (int r = 0; r < bs; r++)
            {
                MemoryUtil.Fill(dst, (ushort)expectedDc, bs);
                dst += stride;
            }
        }

        public static unsafe void HighbdDcTopPredictor4x4(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdDcTopPredictor(dst, stride, 4, above, left, bd);
        }

        public static unsafe void HighbdDcTopPredictor8x8(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdDcTopPredictor(dst, stride, 8, above, left, bd);
        }

        public static unsafe void HighbdDcTopPredictor16x16(ushort* dst, int stride, ushort* above, ushort* left,
            int bd)
        {
            HighbdDcTopPredictor(dst, stride, 16, above, left, bd);
        }

        public static unsafe void HighbdDcTopPredictor32x32(ushort* dst, int stride, ushort* above, ushort* left,
            int bd)
        {
            HighbdDcTopPredictor(dst, stride, 32, above, left, bd);
        }

        private static unsafe void HighbdDcTopPredictor(ushort* dst, int stride, int bs, ushort* above, ushort* left,
            int bd)
        {
            int expectedDc, sum = 0;

            for (int i = 0; i < bs; i++)
            {
                sum += above[i];
            }

            expectedDc = (sum + (bs >> 1)) / bs;

            for (int r = 0; r < bs; r++)
            {
                MemoryUtil.Fill(dst, (ushort)expectedDc, bs);
                dst += stride;
            }
        }

        public static unsafe void HighbdDcPredictor4x4(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdDcPredictor(dst, stride, 4, above, left, bd);
        }

        public static unsafe void HighbdDcPredictor8x8(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdDcPredictor(dst, stride, 8, above, left, bd);
        }

        public static unsafe void HighbdDcPredictor16x16(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdDcPredictor(dst, stride, 16, above, left, bd);
        }

        public static unsafe void HighbdDcPredictor32x32(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            HighbdDcPredictor(dst, stride, 32, above, left, bd);
        }

        private static unsafe void HighbdDcPredictor(ushort* dst, int stride, int bs, ushort* above, ushort* left,
            int bd)
        {
            int expectedDc, sum = 0;
            int count = 2 * bs;

            for (int i = 0; i < bs; i++)
            {
                sum += above[i];
                sum += left[i];
            }

            expectedDc = (sum + (count >> 1)) / count;

            for (int r = 0; r < bs; r++)
            {
                MemoryUtil.Fill(dst, (ushort)expectedDc, bs);
                dst += stride;
            }
        }

        public static unsafe void HighbdD207Predictor4x4(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            ushort I = left[0];
            ushort j = left[1];
            ushort k = left[2];
            ushort l = left[3];
            Dst(dst, stride, 0, 0) = Avg2(I, j);
            Dst(dst, stride, 2, 0) = Dst(dst, stride, 0, 1) = Avg2(j, k);
            Dst(dst, stride, 2, 1) = Dst(dst, stride, 0, 2) = Avg2(k, l);
            Dst(dst, stride, 1, 0) = Avg3(I, j, k);
            Dst(dst, stride, 3, 0) = Dst(dst, stride, 1, 1) = Avg3(j, k, l);
            Dst(dst, stride, 3, 1) = Dst(dst, stride, 1, 2) = Avg3(k, l, l);
            Dst(dst, stride, 3, 2) = Dst(dst, stride, 2, 2) = Dst(dst, stride, 0, 3) =
                Dst(dst, stride, 1, 3) = Dst(dst, stride, 2, 3) = Dst(dst, stride, 3, 3) = l;
        }

        public static unsafe void HighbdD63Predictor4x4(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            ushort a = above[0];
            ushort b = above[1];
            ushort c = above[2];
            ushort d = above[3];
            ushort e = above[4];
            ushort f = above[5];
            ushort g = above[6];
            Dst(dst, stride, 0, 0) = Avg2(a, b);
            Dst(dst, stride, 1, 0) = Dst(dst, stride, 0, 2) = Avg2(b, c);
            Dst(dst, stride, 2, 0) = Dst(dst, stride, 1, 2) = Avg2(c, d);
            Dst(dst, stride, 3, 0) = Dst(dst, stride, 2, 2) = Avg2(d, e);
            Dst(dst, stride, 3, 2) = Avg2(e, f); // Differs from vp8

            Dst(dst, stride, 0, 1) = Avg3(a, b, c);
            Dst(dst, stride, 1, 1) = Dst(dst, stride, 0, 3) = Avg3(b, c, d);
            Dst(dst, stride, 2, 1) = Dst(dst, stride, 1, 3) = Avg3(c, d, e);
            Dst(dst, stride, 3, 1) = Dst(dst, stride, 2, 3) = Avg3(d, e, f);
            Dst(dst, stride, 3, 3) = Avg3(e, f, g); // Differs from vp8
        }

        public static unsafe void HighbdD45Predictor4x4(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            ushort a = above[0];
            ushort b = above[1];
            ushort c = above[2];
            ushort d = above[3];
            ushort e = above[4];
            ushort f = above[5];
            ushort g = above[6];
            ushort h = above[7];
            Dst(dst, stride, 0, 0) = Avg3(a, b, c);
            Dst(dst, stride, 1, 0) = Dst(dst, stride, 0, 1) = Avg3(b, c, d);
            Dst(dst, stride, 2, 0) = Dst(dst, stride, 1, 1) = Dst(dst, stride, 0, 2) = Avg3(c, d, e);
            Dst(dst, stride, 3, 0) =
                Dst(dst, stride, 2, 1) = Dst(dst, stride, 1, 2) = Dst(dst, stride, 0, 3) = Avg3(d, e, f);
            Dst(dst, stride, 3, 1) = Dst(dst, stride, 2, 2) = Dst(dst, stride, 1, 3) = Avg3(e, f, g);
            Dst(dst, stride, 3, 2) = Dst(dst, stride, 2, 3) = Avg3(f, g, h);
            Dst(dst, stride, 3, 3) = h; // Differs from vp8
        }

        public static unsafe void HighbdD117Predictor4x4(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            ushort I = left[0];
            ushort j = left[1];
            ushort k = left[2];
            ushort x = above[-1];
            ushort a = above[0];
            ushort b = above[1];
            ushort c = above[2];
            ushort d = above[3];
            Dst(dst, stride, 0, 0) = Dst(dst, stride, 1, 2) = Avg2(x, a);
            Dst(dst, stride, 1, 0) = Dst(dst, stride, 2, 2) = Avg2(a, b);
            Dst(dst, stride, 2, 0) = Dst(dst, stride, 3, 2) = Avg2(b, c);
            Dst(dst, stride, 3, 0) = Avg2(c, d);

            Dst(dst, stride, 0, 3) = Avg3(k, j, I);
            Dst(dst, stride, 0, 2) = Avg3(j, I, x);
            Dst(dst, stride, 0, 1) = Dst(dst, stride, 1, 3) = Avg3(I, x, a);
            Dst(dst, stride, 1, 1) = Dst(dst, stride, 2, 3) = Avg3(x, a, b);
            Dst(dst, stride, 2, 1) = Dst(dst, stride, 3, 3) = Avg3(a, b, c);
            Dst(dst, stride, 3, 1) = Avg3(b, c, d);
        }

        public static unsafe void HighbdD135Predictor4x4(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            ushort I = left[0];
            ushort j = left[1];
            ushort k = left[2];
            ushort l = left[3];
            ushort x = above[-1];
            ushort a = above[0];
            ushort b = above[1];
            ushort c = above[2];
            ushort d = above[3];
            Dst(dst, stride, 0, 3) = Avg3(j, k, l);
            Dst(dst, stride, 1, 3) = Dst(dst, stride, 0, 2) = Avg3(I, j, k);
            Dst(dst, stride, 2, 3) = Dst(dst, stride, 1, 2) = Dst(dst, stride, 0, 1) = Avg3(x, I, j);
            Dst(dst, stride, 3, 3) =
                Dst(dst, stride, 2, 2) = Dst(dst, stride, 1, 1) = Dst(dst, stride, 0, 0) = Avg3(a, x, I);
            Dst(dst, stride, 3, 2) = Dst(dst, stride, 2, 1) = Dst(dst, stride, 1, 0) = Avg3(b, a, x);
            Dst(dst, stride, 3, 1) = Dst(dst, stride, 2, 0) = Avg3(c, b, a);
            Dst(dst, stride, 3, 0) = Avg3(d, c, b);
        }

        public static unsafe void HighbdD153Predictor4x4(ushort* dst, int stride, ushort* above, ushort* left, int bd)
        {
            ushort I = left[0];
            ushort j = left[1];
            ushort k = left[2];
            ushort l = left[3];
            ushort x = above[-1];
            ushort a = above[0];
            ushort b = above[1];
            ushort c = above[2];

            Dst(dst, stride, 0, 0) = Dst(dst, stride, 2, 1) = Avg2(I, x);
            Dst(dst, stride, 0, 1) = Dst(dst, stride, 2, 2) = Avg2(j, I);
            Dst(dst, stride, 0, 2) = Dst(dst, stride, 2, 3) = Avg2(k, j);
            Dst(dst, stride, 0, 3) = Avg2(l, k);

            Dst(dst, stride, 3, 0) = Avg3(a, b, c);
            Dst(dst, stride, 2, 0) = Avg3(x, a, b);
            Dst(dst, stride, 1, 0) = Dst(dst, stride, 3, 1) = Avg3(I, x, a);
            Dst(dst, stride, 1, 1) = Dst(dst, stride, 3, 2) = Avg3(j, I, x);
            Dst(dst, stride, 1, 2) = Dst(dst, stride, 3, 3) = Avg3(k, j, I);
            Dst(dst, stride, 1, 3) = Avg3(l, k, j);
        }
    }
}