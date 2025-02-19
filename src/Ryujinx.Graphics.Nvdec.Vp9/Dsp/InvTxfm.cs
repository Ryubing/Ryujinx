using Ryujinx.Graphics.Nvdec.Vp9.Common;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static Ryujinx.Graphics.Nvdec.Vp9.Dsp.TxfmCommon;

namespace Ryujinx.Graphics.Nvdec.Vp9.Dsp
{
    internal static class InvTxfm
    {
        // 12 signal input bits + 7 2D forward transform amplify bits + 5 1D inverse
        // transform amplify bits + 1 bit for contingency in rounding and quantizing
        private const int HighbdValidTxfmMagnitudeRange = 1 << 25;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int DetectInvalidHighbdInput(ReadOnlySpan<int> input, int size)
        {
            for (int i = 0; i < size; ++i)
            {
                if (Math.Abs(input[i]) >= HighbdValidTxfmMagnitudeRange)
                {
                    return 1;
                }
            }

            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long CheckRange(long input)
        {
            // For valid VP9 input streams, intermediate stage coefficients should always
            // stay within the range of a signed 16 bit integer. Coefficients can go out
            // of this range for invalid/corrupt VP9 streams.
            Debug.Assert(short.MinValue <= input);
            Debug.Assert(input <= short.MaxValue);
            return input;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long HighbdCheckRange(long input, int bd)
        {
            // For valid highbitdepth VP9 streams, intermediate stage coefficients will
            // stay within the ranges:
            // - 8 bit: signed 16 bit integer
            // - 10 bit: signed 18 bit integer
            // - 12 bit: signed 20 bit integer
            int intMax = (1 << (7 + bd)) - 1;
            int intMin = -intMax - 1;
            Debug.Assert(intMin <= input);
            Debug.Assert(input <= intMax);

            return input;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int WrapLow(long x)
        {
            return (short)CheckRange(x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int HighbdWrapLow(long x, int bd)
        {
            return ((int)HighbdCheckRange(x, bd) << (24 - bd)) >> (24 - bd);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ClipPixelAdd(byte dest, long trans)
        {
            trans = WrapLow(trans);
            return BitUtils.ClipPixel(dest + (int)trans);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort HighbdClipPixelAdd(ushort dest, long trans, int bd)
        {
            trans = HighbdWrapLow(trans, bd);
            return BitUtils.ClipPixelHighbd(dest + (int)trans, bd);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long DctConstRoundShift(long input)
        {
            long rv = BitUtils.RoundPowerOfTwo(input, DctConstBits);
            return rv;
        }

        [SkipLocalsInit]
        public static void Iwht4X416Add(ReadOnlySpan<int> input, Span<byte> dest, int stride)
        {
            /* 4-point reversible, orthonormal inverse Walsh-Hadamard in 3.5 adds,
               0.5 shifts per pixel. */

            Span<int> output = stackalloc int[16];
            long a1, b1, c1, d1, e1;
            ReadOnlySpan<int> ip = input;
            Span<int> op = output;

            for (int i = 0; i < 4; i++)
            {
                a1 = ip[0] >> UnitQuantShift;
                c1 = ip[1] >> UnitQuantShift;
                d1 = ip[2] >> UnitQuantShift;
                b1 = ip[3] >> UnitQuantShift;
                a1 += c1;
                d1 -= b1;
                e1 = (a1 - d1) >> 1;
                b1 = e1 - b1;
                c1 = e1 - c1;
                a1 -= b1;
                d1 += c1;
                op[0] = WrapLow(a1);
                op[1] = WrapLow(b1);
                op[2] = WrapLow(c1);
                op[3] = WrapLow(d1);
                ip = ip.Slice(4);
                op = op.Slice(4);
            }

            Span<int> ip2 = output;
            for (int i = 0; i < 4; i++)
            {
                a1 = ip2[4 * 0];
                c1 = ip2[4 * 1];
                d1 = ip2[4 * 2];
                b1 = ip2[4 * 3];
                a1 += c1;
                d1 -= b1;
                e1 = (a1 - d1) >> 1;
                b1 = e1 - b1;
                c1 = e1 - c1;
                a1 -= b1;
                d1 += c1;
                dest[stride * 0] = ClipPixelAdd(dest[stride * 0], WrapLow(a1));
                dest[stride * 1] = ClipPixelAdd(dest[stride * 1], WrapLow(b1));
                dest[stride * 2] = ClipPixelAdd(dest[stride * 2], WrapLow(c1));
                dest[stride * 3] = ClipPixelAdd(dest[stride * 3], WrapLow(d1));

                ip2 = ip2.Slice(1);
                dest = dest.Slice(1);
            }
        }

        [SkipLocalsInit]
        public static void Iwht4X41Add(ReadOnlySpan<int> input, Span<byte> dest, int stride)
        {
            long a1, e1;
            Span<int> tmp = stackalloc int[4];
            ReadOnlySpan<int> ip = input;
            Span<int> op = tmp;

            a1 = ip[0] >> UnitQuantShift;
            e1 = a1 >> 1;
            a1 -= e1;
            op[0] = WrapLow(a1);
            op[1] = op[2] = op[3] = WrapLow(e1);

            Span<int> ip2 = tmp;
            for (int i = 0; i < 4; i++)
            {
                e1 = ip2[0] >> 1;
                a1 = ip2[0] - e1;
                dest[stride * 0] = ClipPixelAdd(dest[stride * 0], a1);
                dest[stride * 1] = ClipPixelAdd(dest[stride * 1], e1);
                dest[stride * 2] = ClipPixelAdd(dest[stride * 2], e1);
                dest[stride * 3] = ClipPixelAdd(dest[stride * 3], e1);
                ip2 = ip2.Slice(1);
                dest = dest.Slice(1);
            }
        }

        public static void Iadst4(ReadOnlySpan<int> input, Span<int> output)
        {
            long s0, s1, s2, s3, s4, s5, s6, s7;
            int x0 = input[0];
            int x1 = input[1];
            int x2 = input[2];
            int x3 = input[3];

            if ((x0 | x1 | x2 | x3) == 0)
            {
                output.Slice(0, 4).Clear();
                return;
            }

            // 32-bit result is enough for the following multiplications.
            s0 = SinPi19 * x0;
            s1 = SinPi29 * x0;
            s2 = SinPi39 * x1;
            s3 = SinPi49 * x2;
            s4 = SinPi19 * x2;
            s5 = SinPi29 * x3;
            s6 = SinPi49 * x3;
            s7 = WrapLow(x0 - x2 + x3);

            s0 = s0 + s3 + s5;
            s1 = s1 - s4 - s6;
            s3 = s2;
            s2 = SinPi39 * s7;

            // 1-D transform scaling factor is sqrt(2).
            // The overall dynamic range is 14b (input) + 14b (multiplication scaling)
            // + 1b (addition) = 29b.
            // Hence the output bit depth is 15b.
            output[0] = WrapLow(DctConstRoundShift(s0 + s3));
            output[1] = WrapLow(DctConstRoundShift(s1 + s3));
            output[2] = WrapLow(DctConstRoundShift(s2));
            output[3] = WrapLow(DctConstRoundShift(s0 + s1 - s3));
        }

        [SkipLocalsInit]
        public static void Idct4(ReadOnlySpan<int> input, Span<int> output)
        {
            Span<short> step = stackalloc short[4];
            long temp1, temp2;

            // stage 1
            temp1 = ((short)input[0] + (short)input[2]) * CosPi1664;
            temp2 = ((short)input[0] - (short)input[2]) * CosPi1664;
            step[0] = (short)WrapLow(DctConstRoundShift(temp1));
            step[1] = (short)WrapLow(DctConstRoundShift(temp2));
            temp1 = ((short)input[1] * CosPi2464) - ((short)input[3] * CosPi864);
            temp2 = ((short)input[1] * CosPi864) + ((short)input[3] * CosPi2464);
            step[2] = (short)WrapLow(DctConstRoundShift(temp1));
            step[3] = (short)WrapLow(DctConstRoundShift(temp2));

            // stage 2
            output[0] = WrapLow(step[0] + step[3]);
            output[1] = WrapLow(step[1] + step[2]);
            output[2] = WrapLow(step[1] - step[2]);
            output[3] = WrapLow(step[0] - step[3]);
        }

        [SkipLocalsInit]
        public static void Idct4X416Add(ReadOnlySpan<int> input, Span<byte> dest, int stride)
        {
            Span<int> output = stackalloc int[4 * 4];
            Span<int> outptr = output;
            Span<int> tempIn = stackalloc int[4];
            Span<int> tempOut = stackalloc int[4];

            // Rows
            for (int i = 0; i < 4; ++i)
            {
                Idct4(input, outptr);
                input = input.Slice(4);
                outptr = outptr.Slice(4);
            }

            // Columns
            for (int i = 0; i < 4; ++i)
            {
                for (int j = 0; j < 4; ++j)
                {
                    tempIn[j] = output[(j * 4) + i];
                }

                Idct4(tempIn, tempOut);
                for (int j = 0; j < 4; ++j)
                {
                    dest[(j * stride) + i] =
                        ClipPixelAdd(dest[(j * stride) + i], BitUtils.RoundPowerOfTwo(tempOut[j], 4));
                }
            }
        }

        public static void Idct4X41Add(ReadOnlySpan<int> input, Span<byte> dest, int stride)
        {
            long a1;
            int output = WrapLow(DctConstRoundShift((short)input[0] * CosPi1664));

            output = WrapLow(DctConstRoundShift(output * CosPi1664));
            a1 = BitUtils.RoundPowerOfTwo(output, 4);

            for (int i = 0; i < 4; i++)
            {
                dest[0] = ClipPixelAdd(dest[0], a1);
                dest[1] = ClipPixelAdd(dest[1], a1);
                dest[2] = ClipPixelAdd(dest[2], a1);
                dest[3] = ClipPixelAdd(dest[3], a1);
                dest = dest.Slice(stride);
            }
        }

        public static void Iadst8(ReadOnlySpan<int> input, Span<int> output)
        {
            int s0, s1, s2, s3, s4, s5, s6, s7;
            long x0 = input[7];
            long x1 = input[0];
            long x2 = input[5];
            long x3 = input[2];
            long x4 = input[3];
            long x5 = input[4];
            long x6 = input[1];
            long x7 = input[6];

            if ((x0 | x1 | x2 | x3 | x4 | x5 | x6 | x7) == 0)
            {
                output.Slice(0, 8).Clear();
                return;
            }

            // stage 1
            s0 = (int)((CosPi264 * x0) + (CosPi3064 * x1));
            s1 = (int)((CosPi3064 * x0) - (CosPi264 * x1));
            s2 = (int)((CosPi1064 * x2) + (CosPi2264 * x3));
            s3 = (int)((CosPi2264 * x2) - (CosPi1064 * x3));
            s4 = (int)((CosPi1864 * x4) + (CosPi1464 * x5));
            s5 = (int)((CosPi1464 * x4) - (CosPi1864 * x5));
            s6 = (int)((CosPi2664 * x6) + (CosPi664 * x7));
            s7 = (int)((CosPi664 * x6) - (CosPi2664 * x7));

            x0 = WrapLow(DctConstRoundShift(s0 + s4));
            x1 = WrapLow(DctConstRoundShift(s1 + s5));
            x2 = WrapLow(DctConstRoundShift(s2 + s6));
            x3 = WrapLow(DctConstRoundShift(s3 + s7));
            x4 = WrapLow(DctConstRoundShift(s0 - s4));
            x5 = WrapLow(DctConstRoundShift(s1 - s5));
            x6 = WrapLow(DctConstRoundShift(s2 - s6));
            x7 = WrapLow(DctConstRoundShift(s3 - s7));

            // stage 2
            s0 = (int)x0;
            s1 = (int)x1;
            s2 = (int)x2;
            s3 = (int)x3;
            s4 = (int)((CosPi864 * x4) + (CosPi2464 * x5));
            s5 = (int)((CosPi2464 * x4) - (CosPi864 * x5));
            s6 = (int)((-CosPi2464 * x6) + (CosPi864 * x7));
            s7 = (int)((CosPi864 * x6) + (CosPi2464 * x7));

            x0 = WrapLow(s0 + s2);
            x1 = WrapLow(s1 + s3);
            x2 = WrapLow(s0 - s2);
            x3 = WrapLow(s1 - s3);
            x4 = WrapLow(DctConstRoundShift(s4 + s6));
            x5 = WrapLow(DctConstRoundShift(s5 + s7));
            x6 = WrapLow(DctConstRoundShift(s4 - s6));
            x7 = WrapLow(DctConstRoundShift(s5 - s7));

            // stage 3
            s2 = (int)(CosPi1664 * (x2 + x3));
            s3 = (int)(CosPi1664 * (x2 - x3));
            s6 = (int)(CosPi1664 * (x6 + x7));
            s7 = (int)(CosPi1664 * (x6 - x7));

            x2 = WrapLow(DctConstRoundShift(s2));
            x3 = WrapLow(DctConstRoundShift(s3));
            x6 = WrapLow(DctConstRoundShift(s6));
            x7 = WrapLow(DctConstRoundShift(s7));

            output[0] = WrapLow(x0);
            output[1] = WrapLow(-x4);
            output[2] = WrapLow(x6);
            output[3] = WrapLow(-x2);
            output[4] = WrapLow(x3);
            output[5] = WrapLow(-x7);
            output[6] = WrapLow(x5);
            output[7] = WrapLow(-x1);
        }

        [SkipLocalsInit]
        public static void Idct8(ReadOnlySpan<int> input, Span<int> output)
        {
            Span<short> step1 = stackalloc short[8];
            Span<short> step2 = stackalloc short[8];
            long temp1, temp2;

            // stage 1
            step1[0] = (short)input[0];
            step1[2] = (short)input[4];
            step1[1] = (short)input[2];
            step1[3] = (short)input[6];
            temp1 = ((short)input[1] * CosPi2864) - ((short)input[7] * CosPi464);
            temp2 = ((short)input[1] * CosPi464) + ((short)input[7] * CosPi2864);
            step1[4] = (short)WrapLow(DctConstRoundShift(temp1));
            step1[7] = (short)WrapLow(DctConstRoundShift(temp2));
            temp1 = ((short)input[5] * CosPi1264) - ((short)input[3] * CosPi2064);
            temp2 = ((short)input[5] * CosPi2064) + ((short)input[3] * CosPi1264);
            step1[5] = (short)WrapLow(DctConstRoundShift(temp1));
            step1[6] = (short)WrapLow(DctConstRoundShift(temp2));

            // stage 2
            temp1 = (step1[0] + step1[2]) * CosPi1664;
            temp2 = (step1[0] - step1[2]) * CosPi1664;
            step2[0] = (short)WrapLow(DctConstRoundShift(temp1));
            step2[1] = (short)WrapLow(DctConstRoundShift(temp2));
            temp1 = (step1[1] * CosPi2464) - (step1[3] * CosPi864);
            temp2 = (step1[1] * CosPi864) + (step1[3] * CosPi2464);
            step2[2] = (short)WrapLow(DctConstRoundShift(temp1));
            step2[3] = (short)WrapLow(DctConstRoundShift(temp2));
            step2[4] = (short)WrapLow(step1[4] + step1[5]);
            step2[5] = (short)WrapLow(step1[4] - step1[5]);
            step2[6] = (short)WrapLow(-step1[6] + step1[7]);
            step2[7] = (short)WrapLow(step1[6] + step1[7]);

            // stage 3
            step1[0] = (short)WrapLow(step2[0] + step2[3]);
            step1[1] = (short)WrapLow(step2[1] + step2[2]);
            step1[2] = (short)WrapLow(step2[1] - step2[2]);
            step1[3] = (short)WrapLow(step2[0] - step2[3]);
            step1[4] = step2[4];
            temp1 = (step2[6] - step2[5]) * CosPi1664;
            temp2 = (step2[5] + step2[6]) * CosPi1664;
            step1[5] = (short)WrapLow(DctConstRoundShift(temp1));
            step1[6] = (short)WrapLow(DctConstRoundShift(temp2));
            step1[7] = step2[7];

            // stage 4
            output[0] = WrapLow(step1[0] + step1[7]);
            output[1] = WrapLow(step1[1] + step1[6]);
            output[2] = WrapLow(step1[2] + step1[5]);
            output[3] = WrapLow(step1[3] + step1[4]);
            output[4] = WrapLow(step1[3] - step1[4]);
            output[5] = WrapLow(step1[2] - step1[5]);
            output[6] = WrapLow(step1[1] - step1[6]);
            output[7] = WrapLow(step1[0] - step1[7]);
        }

        [SkipLocalsInit]
        public static void Idct8X864Add(ReadOnlySpan<int> input, Span<byte> dest, int stride)
        {
            Span<int> output = stackalloc int[8 * 8];
            Span<int> outptr = output;
            Span<int> tempIn = stackalloc int[8];
            Span<int> tempOut = stackalloc int[8];

            // First transform rows
            for (int i = 0; i < 8; ++i)
            {
                Idct8(input, outptr);
                input = input.Slice(8);
                outptr = outptr.Slice(8);
            }

            // Then transform columns
            for (int i = 0; i < 8; ++i)
            {
                for (int j = 0; j < 8; ++j)
                {
                    tempIn[j] = output[(j * 8) + i];
                }

                Idct8(tempIn, tempOut);
                for (int j = 0; j < 8; ++j)
                {
                    dest[(j * stride) + i] = ClipPixelAdd(dest[(j * stride) + i],
                        BitUtils.RoundPowerOfTwo(tempOut[j], 5));
                }
            }
        }

        [SkipLocalsInit]
        public static void Idct8X812Add(ReadOnlySpan<int> input, Span<byte> dest, int stride)
        {
            Span<int> output = stackalloc int[8 * 8];
            Span<int> outptr = output;
            Span<int> tempIn = stackalloc int[8];
            Span<int> tempOut = stackalloc int[8];

            output.Clear();

            // First transform rows
            // Only first 4 row has non-zero coefs
            for (int i = 0; i < 4; ++i)
            {
                Idct8(input, outptr);
                input = input.Slice(8);
                outptr = outptr.Slice(8);
            }

            // Then transform columns
            for (int i = 0; i < 8; ++i)
            {
                for (int j = 0; j < 8; ++j)
                {
                    tempIn[j] = output[(j * 8) + i];
                }

                Idct8(tempIn, tempOut);
                for (int j = 0; j < 8; ++j)
                {
                    dest[(j * stride) + i] =
                        ClipPixelAdd(dest[(j * stride) + i], BitUtils.RoundPowerOfTwo(tempOut[j], 5));
                }
            }
        }

        public static void Idct8X81Add(ReadOnlySpan<int> input, Span<byte> dest, int stride)
        {
            long a1;
            int output = WrapLow(DctConstRoundShift((short)input[0] * CosPi1664));

            output = WrapLow(DctConstRoundShift(output * CosPi1664));
            a1 = BitUtils.RoundPowerOfTwo(output, 5);
            for (int j = 0; j < 8; ++j)
            {
                for (int i = 0; i < 8; ++i)
                {
                    dest[i] = ClipPixelAdd(dest[i], a1);
                }

                dest = dest.Slice(stride);
            }
        }

        public static void Iadst16(ReadOnlySpan<int> input, Span<int> output)
        {
            long s0, s1, s2, s3, s4, s5, s6, s7, s8;
            long s9, s10, s11, s12, s13, s14, s15;
            long x0 = input[15];
            long x1 = input[0];
            long x2 = input[13];
            long x3 = input[2];
            long x4 = input[11];
            long x5 = input[4];
            long x6 = input[9];
            long x7 = input[6];
            long x8 = input[7];
            long x9 = input[8];
            long x10 = input[5];
            long x11 = input[10];
            long x12 = input[3];
            long x13 = input[12];
            long x14 = input[1];
            long x15 = input[14];

            if ((x0 | x1 | x2 | x3 | x4 | x5 | x6 | x7 | x8 | x9 | x10 | x11 | x12 | x13 | x14 | x15) == 0)
            {
                output.Slice(0, 16).Clear();
                return;
            }

            // stage 1
            s0 = (x0 * CosPi164) + (x1 * CosPi3164);
            s1 = (x0 * CosPi3164) - (x1 * CosPi164);
            s2 = (x2 * CosPi564) + (x3 * CosPi2764);
            s3 = (x2 * CosPi2764) - (x3 * CosPi564);
            s4 = (x4 * CosPi964) + (x5 * CosPi2364);
            s5 = (x4 * CosPi2364) - (x5 * CosPi964);
            s6 = (x6 * CosPi1364) + (x7 * CosPi1964);
            s7 = (x6 * CosPi1964) - (x7 * CosPi1364);
            s8 = (x8 * CosPi1764) + (x9 * CosPi1564);
            s9 = (x8 * CosPi1564) - (x9 * CosPi1764);
            s10 = (x10 * CosPi2164) + (x11 * CosPi1164);
            s11 = (x10 * CosPi1164) - (x11 * CosPi2164);
            s12 = (x12 * CosPi2564) + (x13 * CosPi764);
            s13 = (x12 * CosPi764) - (x13 * CosPi2564);
            s14 = (x14 * CosPi2964) + (x15 * CosPi364);
            s15 = (x14 * CosPi364) - (x15 * CosPi2964);

            x0 = WrapLow(DctConstRoundShift(s0 + s8));
            x1 = WrapLow(DctConstRoundShift(s1 + s9));
            x2 = WrapLow(DctConstRoundShift(s2 + s10));
            x3 = WrapLow(DctConstRoundShift(s3 + s11));
            x4 = WrapLow(DctConstRoundShift(s4 + s12));
            x5 = WrapLow(DctConstRoundShift(s5 + s13));
            x6 = WrapLow(DctConstRoundShift(s6 + s14));
            x7 = WrapLow(DctConstRoundShift(s7 + s15));
            x8 = WrapLow(DctConstRoundShift(s0 - s8));
            x9 = WrapLow(DctConstRoundShift(s1 - s9));
            x10 = WrapLow(DctConstRoundShift(s2 - s10));
            x11 = WrapLow(DctConstRoundShift(s3 - s11));
            x12 = WrapLow(DctConstRoundShift(s4 - s12));
            x13 = WrapLow(DctConstRoundShift(s5 - s13));
            x14 = WrapLow(DctConstRoundShift(s6 - s14));
            x15 = WrapLow(DctConstRoundShift(s7 - s15));

            // stage 2
            s0 = x0;
            s1 = x1;
            s2 = x2;
            s3 = x3;
            s4 = x4;
            s5 = x5;
            s6 = x6;
            s7 = x7;
            s8 = (x8 * CosPi464) + (x9 * CosPi2864);
            s9 = (x8 * CosPi2864) - (x9 * CosPi464);
            s10 = (x10 * CosPi2064) + (x11 * CosPi1264);
            s11 = (x10 * CosPi1264) - (x11 * CosPi2064);
            s12 = (-x12 * CosPi2864) + (x13 * CosPi464);
            s13 = (x12 * CosPi464) + (x13 * CosPi2864);
            s14 = (-x14 * CosPi1264) + (x15 * CosPi2064);
            s15 = (x14 * CosPi2064) + (x15 * CosPi1264);

            x0 = WrapLow(s0 + s4);
            x1 = WrapLow(s1 + s5);
            x2 = WrapLow(s2 + s6);
            x3 = WrapLow(s3 + s7);
            x4 = WrapLow(s0 - s4);
            x5 = WrapLow(s1 - s5);
            x6 = WrapLow(s2 - s6);
            x7 = WrapLow(s3 - s7);
            x8 = WrapLow(DctConstRoundShift(s8 + s12));
            x9 = WrapLow(DctConstRoundShift(s9 + s13));
            x10 = WrapLow(DctConstRoundShift(s10 + s14));
            x11 = WrapLow(DctConstRoundShift(s11 + s15));
            x12 = WrapLow(DctConstRoundShift(s8 - s12));
            x13 = WrapLow(DctConstRoundShift(s9 - s13));
            x14 = WrapLow(DctConstRoundShift(s10 - s14));
            x15 = WrapLow(DctConstRoundShift(s11 - s15));

            // stage 3
            s0 = x0;
            s1 = x1;
            s2 = x2;
            s3 = x3;
            s4 = (x4 * CosPi864) + (x5 * CosPi2464);
            s5 = (x4 * CosPi2464) - (x5 * CosPi864);
            s6 = (-x6 * CosPi2464) + (x7 * CosPi864);
            s7 = (x6 * CosPi864) + (x7 * CosPi2464);
            s8 = x8;
            s9 = x9;
            s10 = x10;
            s11 = x11;
            s12 = (x12 * CosPi864) + (x13 * CosPi2464);
            s13 = (x12 * CosPi2464) - (x13 * CosPi864);
            s14 = (-x14 * CosPi2464) + (x15 * CosPi864);
            s15 = (x14 * CosPi864) + (x15 * CosPi2464);

            x0 = WrapLow(s0 + s2);
            x1 = WrapLow(s1 + s3);
            x2 = WrapLow(s0 - s2);
            x3 = WrapLow(s1 - s3);
            x4 = WrapLow(DctConstRoundShift(s4 + s6));
            x5 = WrapLow(DctConstRoundShift(s5 + s7));
            x6 = WrapLow(DctConstRoundShift(s4 - s6));
            x7 = WrapLow(DctConstRoundShift(s5 - s7));
            x8 = WrapLow(s8 + s10);
            x9 = WrapLow(s9 + s11);
            x10 = WrapLow(s8 - s10);
            x11 = WrapLow(s9 - s11);
            x12 = WrapLow(DctConstRoundShift(s12 + s14));
            x13 = WrapLow(DctConstRoundShift(s13 + s15));
            x14 = WrapLow(DctConstRoundShift(s12 - s14));
            x15 = WrapLow(DctConstRoundShift(s13 - s15));

            // stage 4
            s2 = -CosPi1664 * (x2 + x3);
            s3 = CosPi1664 * (x2 - x3);
            s6 = CosPi1664 * (x6 + x7);
            s7 = CosPi1664 * (-x6 + x7);
            s10 = CosPi1664 * (x10 + x11);
            s11 = CosPi1664 * (-x10 + x11);
            s14 = -CosPi1664 * (x14 + x15);
            s15 = CosPi1664 * (x14 - x15);

            x2 = WrapLow(DctConstRoundShift(s2));
            x3 = WrapLow(DctConstRoundShift(s3));
            x6 = WrapLow(DctConstRoundShift(s6));
            x7 = WrapLow(DctConstRoundShift(s7));
            x10 = WrapLow(DctConstRoundShift(s10));
            x11 = WrapLow(DctConstRoundShift(s11));
            x14 = WrapLow(DctConstRoundShift(s14));
            x15 = WrapLow(DctConstRoundShift(s15));

            output[0] = WrapLow(x0);
            output[1] = WrapLow(-x8);
            output[2] = WrapLow(x12);
            output[3] = WrapLow(-x4);
            output[4] = WrapLow(x6);
            output[5] = WrapLow(x14);
            output[6] = WrapLow(x10);
            output[7] = WrapLow(x2);
            output[8] = WrapLow(x3);
            output[9] = WrapLow(x11);
            output[10] = WrapLow(x15);
            output[11] = WrapLow(x7);
            output[12] = WrapLow(x5);
            output[13] = WrapLow(-x13);
            output[14] = WrapLow(x9);
            output[15] = WrapLow(-x1);
        }

        [SkipLocalsInit]
        public static void Idct16(ReadOnlySpan<int> input, Span<int> output)
        {
            Span<short> step1 = stackalloc short[16];
            Span<short> step2 = stackalloc short[16];
            long temp1, temp2;

            // stage 1
            step1[0] = (short)input[0 / 2];
            step1[1] = (short)input[16 / 2];
            step1[2] = (short)input[8 / 2];
            step1[3] = (short)input[24 / 2];
            step1[4] = (short)input[4 / 2];
            step1[5] = (short)input[20 / 2];
            step1[6] = (short)input[12 / 2];
            step1[7] = (short)input[28 / 2];
            step1[8] = (short)input[2 / 2];
            step1[9] = (short)input[18 / 2];
            step1[10] = (short)input[10 / 2];
            step1[11] = (short)input[26 / 2];
            step1[12] = (short)input[6 / 2];
            step1[13] = (short)input[22 / 2];
            step1[14] = (short)input[14 / 2];
            step1[15] = (short)input[30 / 2];

            // stage 2
            step2[0] = step1[0];
            step2[1] = step1[1];
            step2[2] = step1[2];
            step2[3] = step1[3];
            step2[4] = step1[4];
            step2[5] = step1[5];
            step2[6] = step1[6];
            step2[7] = step1[7];

            temp1 = (step1[8] * CosPi3064) - (step1[15] * CosPi264);
            temp2 = (step1[8] * CosPi264) + (step1[15] * CosPi3064);
            step2[8] = (short)WrapLow(DctConstRoundShift(temp1));
            step2[15] = (short)WrapLow(DctConstRoundShift(temp2));

            temp1 = (step1[9] * CosPi1464) - (step1[14] * CosPi1864);
            temp2 = (step1[9] * CosPi1864) + (step1[14] * CosPi1464);
            step2[9] = (short)WrapLow(DctConstRoundShift(temp1));
            step2[14] = (short)WrapLow(DctConstRoundShift(temp2));

            temp1 = (step1[10] * CosPi2264) - (step1[13] * CosPi1064);
            temp2 = (step1[10] * CosPi1064) + (step1[13] * CosPi2264);
            step2[10] = (short)WrapLow(DctConstRoundShift(temp1));
            step2[13] = (short)WrapLow(DctConstRoundShift(temp2));

            temp1 = (step1[11] * CosPi664) - (step1[12] * CosPi2664);
            temp2 = (step1[11] * CosPi2664) + (step1[12] * CosPi664);
            step2[11] = (short)WrapLow(DctConstRoundShift(temp1));
            step2[12] = (short)WrapLow(DctConstRoundShift(temp2));

            // stage 3
            step1[0] = step2[0];
            step1[1] = step2[1];
            step1[2] = step2[2];
            step1[3] = step2[3];

            temp1 = (step2[4] * CosPi2864) - (step2[7] * CosPi464);
            temp2 = (step2[4] * CosPi464) + (step2[7] * CosPi2864);
            step1[4] = (short)WrapLow(DctConstRoundShift(temp1));
            step1[7] = (short)WrapLow(DctConstRoundShift(temp2));
            temp1 = (step2[5] * CosPi1264) - (step2[6] * CosPi2064);
            temp2 = (step2[5] * CosPi2064) + (step2[6] * CosPi1264);
            step1[5] = (short)WrapLow(DctConstRoundShift(temp1));
            step1[6] = (short)WrapLow(DctConstRoundShift(temp2));

            step1[8] = (short)WrapLow(step2[8] + step2[9]);
            step1[9] = (short)WrapLow(step2[8] - step2[9]);
            step1[10] = (short)WrapLow(-step2[10] + step2[11]);
            step1[11] = (short)WrapLow(step2[10] + step2[11]);
            step1[12] = (short)WrapLow(step2[12] + step2[13]);
            step1[13] = (short)WrapLow(step2[12] - step2[13]);
            step1[14] = (short)WrapLow(-step2[14] + step2[15]);
            step1[15] = (short)WrapLow(step2[14] + step2[15]);

            // stage 4
            temp1 = (step1[0] + step1[1]) * CosPi1664;
            temp2 = (step1[0] - step1[1]) * CosPi1664;
            step2[0] = (short)WrapLow(DctConstRoundShift(temp1));
            step2[1] = (short)WrapLow(DctConstRoundShift(temp2));
            temp1 = (step1[2] * CosPi2464) - (step1[3] * CosPi864);
            temp2 = (step1[2] * CosPi864) + (step1[3] * CosPi2464);
            step2[2] = (short)WrapLow(DctConstRoundShift(temp1));
            step2[3] = (short)WrapLow(DctConstRoundShift(temp2));
            step2[4] = (short)WrapLow(step1[4] + step1[5]);
            step2[5] = (short)WrapLow(step1[4] - step1[5]);
            step2[6] = (short)WrapLow(-step1[6] + step1[7]);
            step2[7] = (short)WrapLow(step1[6] + step1[7]);

            step2[8] = step1[8];
            step2[15] = step1[15];
            temp1 = (-step1[9] * CosPi864) + (step1[14] * CosPi2464);
            temp2 = (step1[9] * CosPi2464) + (step1[14] * CosPi864);
            step2[9] = (short)WrapLow(DctConstRoundShift(temp1));
            step2[14] = (short)WrapLow(DctConstRoundShift(temp2));
            temp1 = (-step1[10] * CosPi2464) - (step1[13] * CosPi864);
            temp2 = (-step1[10] * CosPi864) + (step1[13] * CosPi2464);
            step2[10] = (short)WrapLow(DctConstRoundShift(temp1));
            step2[13] = (short)WrapLow(DctConstRoundShift(temp2));
            step2[11] = step1[11];
            step2[12] = step1[12];

            // stage 5
            step1[0] = (short)WrapLow(step2[0] + step2[3]);
            step1[1] = (short)WrapLow(step2[1] + step2[2]);
            step1[2] = (short)WrapLow(step2[1] - step2[2]);
            step1[3] = (short)WrapLow(step2[0] - step2[3]);
            step1[4] = step2[4];
            temp1 = (step2[6] - step2[5]) * CosPi1664;
            temp2 = (step2[5] + step2[6]) * CosPi1664;
            step1[5] = (short)WrapLow(DctConstRoundShift(temp1));
            step1[6] = (short)WrapLow(DctConstRoundShift(temp2));
            step1[7] = step2[7];

            step1[8] = (short)WrapLow(step2[8] + step2[11]);
            step1[9] = (short)WrapLow(step2[9] + step2[10]);
            step1[10] = (short)WrapLow(step2[9] - step2[10]);
            step1[11] = (short)WrapLow(step2[8] - step2[11]);
            step1[12] = (short)WrapLow(-step2[12] + step2[15]);
            step1[13] = (short)WrapLow(-step2[13] + step2[14]);
            step1[14] = (short)WrapLow(step2[13] + step2[14]);
            step1[15] = (short)WrapLow(step2[12] + step2[15]);

            // stage 6
            step2[0] = (short)WrapLow(step1[0] + step1[7]);
            step2[1] = (short)WrapLow(step1[1] + step1[6]);
            step2[2] = (short)WrapLow(step1[2] + step1[5]);
            step2[3] = (short)WrapLow(step1[3] + step1[4]);
            step2[4] = (short)WrapLow(step1[3] - step1[4]);
            step2[5] = (short)WrapLow(step1[2] - step1[5]);
            step2[6] = (short)WrapLow(step1[1] - step1[6]);
            step2[7] = (short)WrapLow(step1[0] - step1[7]);
            step2[8] = step1[8];
            step2[9] = step1[9];
            temp1 = (-step1[10] + step1[13]) * CosPi1664;
            temp2 = (step1[10] + step1[13]) * CosPi1664;
            step2[10] = (short)WrapLow(DctConstRoundShift(temp1));
            step2[13] = (short)WrapLow(DctConstRoundShift(temp2));
            temp1 = (-step1[11] + step1[12]) * CosPi1664;
            temp2 = (step1[11] + step1[12]) * CosPi1664;
            step2[11] = (short)WrapLow(DctConstRoundShift(temp1));
            step2[12] = (short)WrapLow(DctConstRoundShift(temp2));
            step2[14] = step1[14];
            step2[15] = step1[15];

            // stage 7
            output[0] = WrapLow(step2[0] + step2[15]);
            output[1] = WrapLow(step2[1] + step2[14]);
            output[2] = WrapLow(step2[2] + step2[13]);
            output[3] = WrapLow(step2[3] + step2[12]);
            output[4] = WrapLow(step2[4] + step2[11]);
            output[5] = WrapLow(step2[5] + step2[10]);
            output[6] = WrapLow(step2[6] + step2[9]);
            output[7] = WrapLow(step2[7] + step2[8]);
            output[8] = WrapLow(step2[7] - step2[8]);
            output[9] = WrapLow(step2[6] - step2[9]);
            output[10] = WrapLow(step2[5] - step2[10]);
            output[11] = WrapLow(step2[4] - step2[11]);
            output[12] = WrapLow(step2[3] - step2[12]);
            output[13] = WrapLow(step2[2] - step2[13]);
            output[14] = WrapLow(step2[1] - step2[14]);
            output[15] = WrapLow(step2[0] - step2[15]);
        }

        [SkipLocalsInit]
        public static void Idct16X16256Add(ReadOnlySpan<int> input, Span<byte> dest, int stride)
        {
            Span<int> output = stackalloc int[16 * 16];
            Span<int> outptr = output;
            Span<int> tempIn = stackalloc int[16];
            Span<int> tempOut = stackalloc int[16];

            // First transform rows
            for (int i = 0; i < 16; ++i)
            {
                Idct16(input, outptr);
                input = input.Slice(16);
                outptr = outptr.Slice(16);
            }

            // Then transform columns
            for (int i = 0; i < 16; ++i)
            {
                for (int j = 0; j < 16; ++j)
                {
                    tempIn[j] = output[(j * 16) + i];
                }

                Idct16(tempIn, tempOut);
                for (int j = 0; j < 16; ++j)
                {
                    dest[(j * stride) + i] =
                        ClipPixelAdd(dest[(j * stride) + i], BitUtils.RoundPowerOfTwo(tempOut[j], 6));
                }
            }
        }

        [SkipLocalsInit]
        public static void Idct16X1638Add(ReadOnlySpan<int> input, Span<byte> dest, int stride)
        {
            Span<int> output = stackalloc int[16 * 16];
            Span<int> outptr = output;
            Span<int> tempIn = stackalloc int[16];
            Span<int> tempOut = stackalloc int[16];

            output.Clear();

            // First transform rows. Since all non-zero dct coefficients are in
            // upper-left 8x8 area, we only need to calculate first 8 rows here.
            for (int i = 0; i < 8; ++i)
            {
                Idct16(input, outptr);
                input = input.Slice(16);
                outptr = outptr.Slice(16);
            }

            // Then transform columns
            for (int i = 0; i < 16; ++i)
            {
                for (int j = 0; j < 16; ++j)
                {
                    tempIn[j] = output[(j * 16) + i];
                }

                Idct16(tempIn, tempOut);
                for (int j = 0; j < 16; ++j)
                {
                    dest[(j * stride) + i] =
                        ClipPixelAdd(dest[(j * stride) + i], BitUtils.RoundPowerOfTwo(tempOut[j], 6));
                }
            }
        }

        [SkipLocalsInit]
        public static void Idct16X1610Add(ReadOnlySpan<int> input, Span<byte> dest, int stride)
        {
            Span<int> output = stackalloc int[16 * 16];
            Span<int> outptr = output;
            Span<int> tempIn = stackalloc int[16];
            Span<int> tempOut = stackalloc int[16];

            output.Clear();

            // First transform rows. Since all non-zero dct coefficients are in
            // upper-left 4x4 area, we only need to calculate first 4 rows here.
            for (int i = 0; i < 4; ++i)
            {
                Idct16(input, outptr);
                input = input.Slice(16);
                outptr = outptr.Slice(16);
            }

            // Then transform columns
            for (int i = 0; i < 16; ++i)
            {
                for (int j = 0; j < 16; ++j)
                {
                    tempIn[j] = output[(j * 16) + i];
                }

                Idct16(tempIn, tempOut);
                for (int j = 0; j < 16; ++j)
                {
                    dest[(j * stride) + i] =
                        ClipPixelAdd(dest[(j * stride) + i], BitUtils.RoundPowerOfTwo(tempOut[j], 6));
                }
            }
        }

        public static void Idct16X161Add(ReadOnlySpan<int> input, Span<byte> dest, int stride)
        {
            long a1;
            int output = WrapLow(DctConstRoundShift((short)input[0] * CosPi1664));

            output = WrapLow(DctConstRoundShift(output * CosPi1664));
            a1 = BitUtils.RoundPowerOfTwo(output, 6);
            for (int j = 0; j < 16; ++j)
            {
                for (int i = 0; i < 16; ++i)
                {
                    dest[i] = ClipPixelAdd(dest[i], a1);
                }

                dest = dest.Slice(stride);
            }
        }

        [SkipLocalsInit]
        public static void Idct32(ReadOnlySpan<int> input, Span<int> output)
        {
            Span<short> step1 = stackalloc short[32];
            Span<short> step2 = stackalloc short[32];
            long temp1, temp2;

            // stage 1
            step1[0] = (short)input[0];
            step1[1] = (short)input[16];
            step1[2] = (short)input[8];
            step1[3] = (short)input[24];
            step1[4] = (short)input[4];
            step1[5] = (short)input[20];
            step1[6] = (short)input[12];
            step1[7] = (short)input[28];
            step1[8] = (short)input[2];
            step1[9] = (short)input[18];
            step1[10] = (short)input[10];
            step1[11] = (short)input[26];
            step1[12] = (short)input[6];
            step1[13] = (short)input[22];
            step1[14] = (short)input[14];
            step1[15] = (short)input[30];

            temp1 = ((short)input[1] * CosPi3164) - ((short)input[31] * CosPi164);
            temp2 = ((short)input[1] * CosPi164) + ((short)input[31] * CosPi3164);
            step1[16] = (short)WrapLow(DctConstRoundShift(temp1));
            step1[31] = (short)WrapLow(DctConstRoundShift(temp2));

            temp1 = ((short)input[17] * CosPi1564) - ((short)input[15] * CosPi1764);
            temp2 = ((short)input[17] * CosPi1764) + ((short)input[15] * CosPi1564);
            step1[17] = (short)WrapLow(DctConstRoundShift(temp1));
            step1[30] = (short)WrapLow(DctConstRoundShift(temp2));

            temp1 = ((short)input[9] * CosPi2364) - ((short)input[23] * CosPi964);
            temp2 = ((short)input[9] * CosPi964) + ((short)input[23] * CosPi2364);
            step1[18] = (short)WrapLow(DctConstRoundShift(temp1));
            step1[29] = (short)WrapLow(DctConstRoundShift(temp2));

            temp1 = ((short)input[25] * CosPi764) - ((short)input[7] * CosPi2564);
            temp2 = ((short)input[25] * CosPi2564) + ((short)input[7] * CosPi764);
            step1[19] = (short)WrapLow(DctConstRoundShift(temp1));
            step1[28] = (short)WrapLow(DctConstRoundShift(temp2));

            temp1 = ((short)input[5] * CosPi2764) - ((short)input[27] * CosPi564);
            temp2 = ((short)input[5] * CosPi564) + ((short)input[27] * CosPi2764);
            step1[20] = (short)WrapLow(DctConstRoundShift(temp1));
            step1[27] = (short)WrapLow(DctConstRoundShift(temp2));

            temp1 = ((short)input[21] * CosPi1164) - ((short)input[11] * CosPi2164);
            temp2 = ((short)input[21] * CosPi2164) + ((short)input[11] * CosPi1164);
            step1[21] = (short)WrapLow(DctConstRoundShift(temp1));
            step1[26] = (short)WrapLow(DctConstRoundShift(temp2));

            temp1 = ((short)input[13] * CosPi1964) - ((short)input[19] * CosPi1364);
            temp2 = ((short)input[13] * CosPi1364) + ((short)input[19] * CosPi1964);
            step1[22] = (short)WrapLow(DctConstRoundShift(temp1));
            step1[25] = (short)WrapLow(DctConstRoundShift(temp2));

            temp1 = ((short)input[29] * CosPi364) - ((short)input[3] * CosPi2964);
            temp2 = ((short)input[29] * CosPi2964) + ((short)input[3] * CosPi364);
            step1[23] = (short)WrapLow(DctConstRoundShift(temp1));
            step1[24] = (short)WrapLow(DctConstRoundShift(temp2));

            // stage 2
            step2[0] = step1[0];
            step2[1] = step1[1];
            step2[2] = step1[2];
            step2[3] = step1[3];
            step2[4] = step1[4];
            step2[5] = step1[5];
            step2[6] = step1[6];
            step2[7] = step1[7];

            temp1 = (step1[8] * CosPi3064) - (step1[15] * CosPi264);
            temp2 = (step1[8] * CosPi264) + (step1[15] * CosPi3064);
            step2[8] = (short)WrapLow(DctConstRoundShift(temp1));
            step2[15] = (short)WrapLow(DctConstRoundShift(temp2));

            temp1 = (step1[9] * CosPi1464) - (step1[14] * CosPi1864);
            temp2 = (step1[9] * CosPi1864) + (step1[14] * CosPi1464);
            step2[9] = (short)WrapLow(DctConstRoundShift(temp1));
            step2[14] = (short)WrapLow(DctConstRoundShift(temp2));

            temp1 = (step1[10] * CosPi2264) - (step1[13] * CosPi1064);
            temp2 = (step1[10] * CosPi1064) + (step1[13] * CosPi2264);
            step2[10] = (short)WrapLow(DctConstRoundShift(temp1));
            step2[13] = (short)WrapLow(DctConstRoundShift(temp2));

            temp1 = (step1[11] * CosPi664) - (step1[12] * CosPi2664);
            temp2 = (step1[11] * CosPi2664) + (step1[12] * CosPi664);
            step2[11] = (short)WrapLow(DctConstRoundShift(temp1));
            step2[12] = (short)WrapLow(DctConstRoundShift(temp2));

            step2[16] = (short)WrapLow(step1[16] + step1[17]);
            step2[17] = (short)WrapLow(step1[16] - step1[17]);
            step2[18] = (short)WrapLow(-step1[18] + step1[19]);
            step2[19] = (short)WrapLow(step1[18] + step1[19]);
            step2[20] = (short)WrapLow(step1[20] + step1[21]);
            step2[21] = (short)WrapLow(step1[20] - step1[21]);
            step2[22] = (short)WrapLow(-step1[22] + step1[23]);
            step2[23] = (short)WrapLow(step1[22] + step1[23]);
            step2[24] = (short)WrapLow(step1[24] + step1[25]);
            step2[25] = (short)WrapLow(step1[24] - step1[25]);
            step2[26] = (short)WrapLow(-step1[26] + step1[27]);
            step2[27] = (short)WrapLow(step1[26] + step1[27]);
            step2[28] = (short)WrapLow(step1[28] + step1[29]);
            step2[29] = (short)WrapLow(step1[28] - step1[29]);
            step2[30] = (short)WrapLow(-step1[30] + step1[31]);
            step2[31] = (short)WrapLow(step1[30] + step1[31]);

            // stage 3
            step1[0] = step2[0];
            step1[1] = step2[1];
            step1[2] = step2[2];
            step1[3] = step2[3];

            temp1 = (step2[4] * CosPi2864) - (step2[7] * CosPi464);
            temp2 = (step2[4] * CosPi464) + (step2[7] * CosPi2864);
            step1[4] = (short)WrapLow(DctConstRoundShift(temp1));
            step1[7] = (short)WrapLow(DctConstRoundShift(temp2));
            temp1 = (step2[5] * CosPi1264) - (step2[6] * CosPi2064);
            temp2 = (step2[5] * CosPi2064) + (step2[6] * CosPi1264);
            step1[5] = (short)WrapLow(DctConstRoundShift(temp1));
            step1[6] = (short)WrapLow(DctConstRoundShift(temp2));

            step1[8] = (short)WrapLow(step2[8] + step2[9]);
            step1[9] = (short)WrapLow(step2[8] - step2[9]);
            step1[10] = (short)WrapLow(-step2[10] + step2[11]);
            step1[11] = (short)WrapLow(step2[10] + step2[11]);
            step1[12] = (short)WrapLow(step2[12] + step2[13]);
            step1[13] = (short)WrapLow(step2[12] - step2[13]);
            step1[14] = (short)WrapLow(-step2[14] + step2[15]);
            step1[15] = (short)WrapLow(step2[14] + step2[15]);

            step1[16] = step2[16];
            step1[31] = step2[31];
            temp1 = (-step2[17] * CosPi464) + (step2[30] * CosPi2864);
            temp2 = (step2[17] * CosPi2864) + (step2[30] * CosPi464);
            step1[17] = (short)WrapLow(DctConstRoundShift(temp1));
            step1[30] = (short)WrapLow(DctConstRoundShift(temp2));
            temp1 = (-step2[18] * CosPi2864) - (step2[29] * CosPi464);
            temp2 = (-step2[18] * CosPi464) + (step2[29] * CosPi2864);
            step1[18] = (short)WrapLow(DctConstRoundShift(temp1));
            step1[29] = (short)WrapLow(DctConstRoundShift(temp2));
            step1[19] = step2[19];
            step1[20] = step2[20];
            temp1 = (-step2[21] * CosPi2064) + (step2[26] * CosPi1264);
            temp2 = (step2[21] * CosPi1264) + (step2[26] * CosPi2064);
            step1[21] = (short)WrapLow(DctConstRoundShift(temp1));
            step1[26] = (short)WrapLow(DctConstRoundShift(temp2));
            temp1 = (-step2[22] * CosPi1264) - (step2[25] * CosPi2064);
            temp2 = (-step2[22] * CosPi2064) + (step2[25] * CosPi1264);
            step1[22] = (short)WrapLow(DctConstRoundShift(temp1));
            step1[25] = (short)WrapLow(DctConstRoundShift(temp2));
            step1[23] = step2[23];
            step1[24] = step2[24];
            step1[27] = step2[27];
            step1[28] = step2[28];

            // stage 4
            temp1 = (step1[0] + step1[1]) * CosPi1664;
            temp2 = (step1[0] - step1[1]) * CosPi1664;
            step2[0] = (short)WrapLow(DctConstRoundShift(temp1));
            step2[1] = (short)WrapLow(DctConstRoundShift(temp2));
            temp1 = (step1[2] * CosPi2464) - (step1[3] * CosPi864);
            temp2 = (step1[2] * CosPi864) + (step1[3] * CosPi2464);
            step2[2] = (short)WrapLow(DctConstRoundShift(temp1));
            step2[3] = (short)WrapLow(DctConstRoundShift(temp2));
            step2[4] = (short)WrapLow(step1[4] + step1[5]);
            step2[5] = (short)WrapLow(step1[4] - step1[5]);
            step2[6] = (short)WrapLow(-step1[6] + step1[7]);
            step2[7] = (short)WrapLow(step1[6] + step1[7]);

            step2[8] = step1[8];
            step2[15] = step1[15];
            temp1 = (-step1[9] * CosPi864) + (step1[14] * CosPi2464);
            temp2 = (step1[9] * CosPi2464) + (step1[14] * CosPi864);
            step2[9] = (short)WrapLow(DctConstRoundShift(temp1));
            step2[14] = (short)WrapLow(DctConstRoundShift(temp2));
            temp1 = (-step1[10] * CosPi2464) - (step1[13] * CosPi864);
            temp2 = (-step1[10] * CosPi864) + (step1[13] * CosPi2464);
            step2[10] = (short)WrapLow(DctConstRoundShift(temp1));
            step2[13] = (short)WrapLow(DctConstRoundShift(temp2));
            step2[11] = step1[11];
            step2[12] = step1[12];

            step2[16] = (short)WrapLow(step1[16] + step1[19]);
            step2[17] = (short)WrapLow(step1[17] + step1[18]);
            step2[18] = (short)WrapLow(step1[17] - step1[18]);
            step2[19] = (short)WrapLow(step1[16] - step1[19]);
            step2[20] = (short)WrapLow(-step1[20] + step1[23]);
            step2[21] = (short)WrapLow(-step1[21] + step1[22]);
            step2[22] = (short)WrapLow(step1[21] + step1[22]);
            step2[23] = (short)WrapLow(step1[20] + step1[23]);

            step2[24] = (short)WrapLow(step1[24] + step1[27]);
            step2[25] = (short)WrapLow(step1[25] + step1[26]);
            step2[26] = (short)WrapLow(step1[25] - step1[26]);
            step2[27] = (short)WrapLow(step1[24] - step1[27]);
            step2[28] = (short)WrapLow(-step1[28] + step1[31]);
            step2[29] = (short)WrapLow(-step1[29] + step1[30]);
            step2[30] = (short)WrapLow(step1[29] + step1[30]);
            step2[31] = (short)WrapLow(step1[28] + step1[31]);

            // stage 5
            step1[0] = (short)WrapLow(step2[0] + step2[3]);
            step1[1] = (short)WrapLow(step2[1] + step2[2]);
            step1[2] = (short)WrapLow(step2[1] - step2[2]);
            step1[3] = (short)WrapLow(step2[0] - step2[3]);
            step1[4] = step2[4];
            temp1 = (step2[6] - step2[5]) * CosPi1664;
            temp2 = (step2[5] + step2[6]) * CosPi1664;
            step1[5] = (short)WrapLow(DctConstRoundShift(temp1));
            step1[6] = (short)WrapLow(DctConstRoundShift(temp2));
            step1[7] = step2[7];

            step1[8] = (short)WrapLow(step2[8] + step2[11]);
            step1[9] = (short)WrapLow(step2[9] + step2[10]);
            step1[10] = (short)WrapLow(step2[9] - step2[10]);
            step1[11] = (short)WrapLow(step2[8] - step2[11]);
            step1[12] = (short)WrapLow(-step2[12] + step2[15]);
            step1[13] = (short)WrapLow(-step2[13] + step2[14]);
            step1[14] = (short)WrapLow(step2[13] + step2[14]);
            step1[15] = (short)WrapLow(step2[12] + step2[15]);

            step1[16] = step2[16];
            step1[17] = step2[17];
            temp1 = (-step2[18] * CosPi864) + (step2[29] * CosPi2464);
            temp2 = (step2[18] * CosPi2464) + (step2[29] * CosPi864);
            step1[18] = (short)WrapLow(DctConstRoundShift(temp1));
            step1[29] = (short)WrapLow(DctConstRoundShift(temp2));
            temp1 = (-step2[19] * CosPi864) + (step2[28] * CosPi2464);
            temp2 = (step2[19] * CosPi2464) + (step2[28] * CosPi864);
            step1[19] = (short)WrapLow(DctConstRoundShift(temp1));
            step1[28] = (short)WrapLow(DctConstRoundShift(temp2));
            temp1 = (-step2[20] * CosPi2464) - (step2[27] * CosPi864);
            temp2 = (-step2[20] * CosPi864) + (step2[27] * CosPi2464);
            step1[20] = (short)WrapLow(DctConstRoundShift(temp1));
            step1[27] = (short)WrapLow(DctConstRoundShift(temp2));
            temp1 = (-step2[21] * CosPi2464) - (step2[26] * CosPi864);
            temp2 = (-step2[21] * CosPi864) + (step2[26] * CosPi2464);
            step1[21] = (short)WrapLow(DctConstRoundShift(temp1));
            step1[26] = (short)WrapLow(DctConstRoundShift(temp2));
            step1[22] = step2[22];
            step1[23] = step2[23];
            step1[24] = step2[24];
            step1[25] = step2[25];
            step1[30] = step2[30];
            step1[31] = step2[31];

            // stage 6
            step2[0] = (short)WrapLow(step1[0] + step1[7]);
            step2[1] = (short)WrapLow(step1[1] + step1[6]);
            step2[2] = (short)WrapLow(step1[2] + step1[5]);
            step2[3] = (short)WrapLow(step1[3] + step1[4]);
            step2[4] = (short)WrapLow(step1[3] - step1[4]);
            step2[5] = (short)WrapLow(step1[2] - step1[5]);
            step2[6] = (short)WrapLow(step1[1] - step1[6]);
            step2[7] = (short)WrapLow(step1[0] - step1[7]);
            step2[8] = step1[8];
            step2[9] = step1[9];
            temp1 = (-step1[10] + step1[13]) * CosPi1664;
            temp2 = (step1[10] + step1[13]) * CosPi1664;
            step2[10] = (short)WrapLow(DctConstRoundShift(temp1));
            step2[13] = (short)WrapLow(DctConstRoundShift(temp2));
            temp1 = (-step1[11] + step1[12]) * CosPi1664;
            temp2 = (step1[11] + step1[12]) * CosPi1664;
            step2[11] = (short)WrapLow(DctConstRoundShift(temp1));
            step2[12] = (short)WrapLow(DctConstRoundShift(temp2));
            step2[14] = step1[14];
            step2[15] = step1[15];

            step2[16] = (short)WrapLow(step1[16] + step1[23]);
            step2[17] = (short)WrapLow(step1[17] + step1[22]);
            step2[18] = (short)WrapLow(step1[18] + step1[21]);
            step2[19] = (short)WrapLow(step1[19] + step1[20]);
            step2[20] = (short)WrapLow(step1[19] - step1[20]);
            step2[21] = (short)WrapLow(step1[18] - step1[21]);
            step2[22] = (short)WrapLow(step1[17] - step1[22]);
            step2[23] = (short)WrapLow(step1[16] - step1[23]);

            step2[24] = (short)WrapLow(-step1[24] + step1[31]);
            step2[25] = (short)WrapLow(-step1[25] + step1[30]);
            step2[26] = (short)WrapLow(-step1[26] + step1[29]);
            step2[27] = (short)WrapLow(-step1[27] + step1[28]);
            step2[28] = (short)WrapLow(step1[27] + step1[28]);
            step2[29] = (short)WrapLow(step1[26] + step1[29]);
            step2[30] = (short)WrapLow(step1[25] + step1[30]);
            step2[31] = (short)WrapLow(step1[24] + step1[31]);

            // stage 7
            step1[0] = (short)WrapLow(step2[0] + step2[15]);
            step1[1] = (short)WrapLow(step2[1] + step2[14]);
            step1[2] = (short)WrapLow(step2[2] + step2[13]);
            step1[3] = (short)WrapLow(step2[3] + step2[12]);
            step1[4] = (short)WrapLow(step2[4] + step2[11]);
            step1[5] = (short)WrapLow(step2[5] + step2[10]);
            step1[6] = (short)WrapLow(step2[6] + step2[9]);
            step1[7] = (short)WrapLow(step2[7] + step2[8]);
            step1[8] = (short)WrapLow(step2[7] - step2[8]);
            step1[9] = (short)WrapLow(step2[6] - step2[9]);
            step1[10] = (short)WrapLow(step2[5] - step2[10]);
            step1[11] = (short)WrapLow(step2[4] - step2[11]);
            step1[12] = (short)WrapLow(step2[3] - step2[12]);
            step1[13] = (short)WrapLow(step2[2] - step2[13]);
            step1[14] = (short)WrapLow(step2[1] - step2[14]);
            step1[15] = (short)WrapLow(step2[0] - step2[15]);

            step1[16] = step2[16];
            step1[17] = step2[17];
            step1[18] = step2[18];
            step1[19] = step2[19];
            temp1 = (-step2[20] + step2[27]) * CosPi1664;
            temp2 = (step2[20] + step2[27]) * CosPi1664;
            step1[20] = (short)WrapLow(DctConstRoundShift(temp1));
            step1[27] = (short)WrapLow(DctConstRoundShift(temp2));
            temp1 = (-step2[21] + step2[26]) * CosPi1664;
            temp2 = (step2[21] + step2[26]) * CosPi1664;
            step1[21] = (short)WrapLow(DctConstRoundShift(temp1));
            step1[26] = (short)WrapLow(DctConstRoundShift(temp2));
            temp1 = (-step2[22] + step2[25]) * CosPi1664;
            temp2 = (step2[22] + step2[25]) * CosPi1664;
            step1[22] = (short)WrapLow(DctConstRoundShift(temp1));
            step1[25] = (short)WrapLow(DctConstRoundShift(temp2));
            temp1 = (-step2[23] + step2[24]) * CosPi1664;
            temp2 = (step2[23] + step2[24]) * CosPi1664;
            step1[23] = (short)WrapLow(DctConstRoundShift(temp1));
            step1[24] = (short)WrapLow(DctConstRoundShift(temp2));
            step1[28] = step2[28];
            step1[29] = step2[29];
            step1[30] = step2[30];
            step1[31] = step2[31];

            // final stage
            output[0] = WrapLow(step1[0] + step1[31]);
            output[1] = WrapLow(step1[1] + step1[30]);
            output[2] = WrapLow(step1[2] + step1[29]);
            output[3] = WrapLow(step1[3] + step1[28]);
            output[4] = WrapLow(step1[4] + step1[27]);
            output[5] = WrapLow(step1[5] + step1[26]);
            output[6] = WrapLow(step1[6] + step1[25]);
            output[7] = WrapLow(step1[7] + step1[24]);
            output[8] = WrapLow(step1[8] + step1[23]);
            output[9] = WrapLow(step1[9] + step1[22]);
            output[10] = WrapLow(step1[10] + step1[21]);
            output[11] = WrapLow(step1[11] + step1[20]);
            output[12] = WrapLow(step1[12] + step1[19]);
            output[13] = WrapLow(step1[13] + step1[18]);
            output[14] = WrapLow(step1[14] + step1[17]);
            output[15] = WrapLow(step1[15] + step1[16]);
            output[16] = WrapLow(step1[15] - step1[16]);
            output[17] = WrapLow(step1[14] - step1[17]);
            output[18] = WrapLow(step1[13] - step1[18]);
            output[19] = WrapLow(step1[12] - step1[19]);
            output[20] = WrapLow(step1[11] - step1[20]);
            output[21] = WrapLow(step1[10] - step1[21]);
            output[22] = WrapLow(step1[9] - step1[22]);
            output[23] = WrapLow(step1[8] - step1[23]);
            output[24] = WrapLow(step1[7] - step1[24]);
            output[25] = WrapLow(step1[6] - step1[25]);
            output[26] = WrapLow(step1[5] - step1[26]);
            output[27] = WrapLow(step1[4] - step1[27]);
            output[28] = WrapLow(step1[3] - step1[28]);
            output[29] = WrapLow(step1[2] - step1[29]);
            output[30] = WrapLow(step1[1] - step1[30]);
            output[31] = WrapLow(step1[0] - step1[31]);
        }

        [SkipLocalsInit]
        public static void Idct32X321024Add(ReadOnlySpan<int> input, Span<byte> dest, int stride)
        {
            Span<int> output = stackalloc int[32 * 32];
            Span<int> outptr = output;
            Span<int> tempIn = stackalloc int[32];
            Span<int> tempOut = stackalloc int[32];

            // Rows
            for (int i = 0; i < 32; ++i)
            {
                short zeroCoeff = 0;
                for (int j = 0; j < 32; ++j)
                {
                    zeroCoeff |= (short)input[j];
                }

                if (zeroCoeff != 0)
                {
                    Idct32(input, outptr);
                }
                else
                {
                    outptr.Slice(0, 32).Clear();
                }

                input = input.Slice(32);
                outptr = outptr.Slice(32);
            }

            // Columns
            for (int i = 0; i < 32; ++i)
            {
                for (int j = 0; j < 32; ++j)
                {
                    tempIn[j] = output[(j * 32) + i];
                }

                Idct32(tempIn, tempOut);
                for (int j = 0; j < 32; ++j)
                {
                    dest[(j * stride) + i] =
                        ClipPixelAdd(dest[(j * stride) + i], BitUtils.RoundPowerOfTwo(tempOut[j], 6));
                }
            }
        }

        [SkipLocalsInit]
        public static void Idct32X32135Add(ReadOnlySpan<int> input, Span<byte> dest, int stride)
        {
            Span<int> output = stackalloc int[32 * 32];
            Span<int> outptr = output;
            Span<int> tempIn = stackalloc int[32];
            Span<int> tempOut = stackalloc int[32];

            output.Clear();

            // Rows
            // Only upper-left 16x16 has non-zero coeff
            for (int i = 0; i < 16; ++i)
            {
                Idct32(input, outptr);
                input = input.Slice(32);
                outptr = outptr.Slice(32);
            }

            // Columns
            for (int i = 0; i < 32; ++i)
            {
                for (int j = 0; j < 32; ++j)
                {
                    tempIn[j] = output[(j * 32) + i];
                }

                Idct32(tempIn, tempOut);
                for (int j = 0; j < 32; ++j)
                {
                    dest[(j * stride) + i] =
                        ClipPixelAdd(dest[(j * stride) + i], BitUtils.RoundPowerOfTwo(tempOut[j], 6));
                }
            }
        }

        [SkipLocalsInit]
        public static void Idct32X3234Add(ReadOnlySpan<int> input, Span<byte> dest, int stride)
        {
            Span<int> output = stackalloc int[32 * 32];
            Span<int> outptr = output;
            Span<int> tempIn = stackalloc int[32];
            Span<int> tempOut = stackalloc int[32];

            output.Clear();

            // Rows
            // Only upper-left 8x8 has non-zero coeff
            for (int i = 0; i < 8; ++i)
            {
                Idct32(input, outptr);
                input = input.Slice(32);
                outptr = outptr.Slice(32);
            }

            // Columns
            for (int i = 0; i < 32; ++i)
            {
                for (int j = 0; j < 32; ++j)
                {
                    tempIn[j] = output[(j * 32) + i];
                }

                Idct32(tempIn, tempOut);
                for (int j = 0; j < 32; ++j)
                {
                    dest[(j * stride) + i] =
                        ClipPixelAdd(dest[(j * stride) + i], BitUtils.RoundPowerOfTwo(tempOut[j], 6));
                }
            }
        }

        public static void Idct32X321Add(ReadOnlySpan<int> input, Span<byte> dest, int stride)
        {
            long a1;
            int output = WrapLow(DctConstRoundShift((short)input[0] * CosPi1664));

            output = WrapLow(DctConstRoundShift(output * CosPi1664));
            a1 = BitUtils.RoundPowerOfTwo(output, 6);

            for (int j = 0; j < 32; ++j)
            {
                for (int i = 0; i < 32; ++i)
                {
                    dest[i] = ClipPixelAdd(dest[i], a1);
                }

                dest = dest.Slice(stride);
            }
        }

        [SkipLocalsInit]
        public static void HighbdIwht4X416Add(ReadOnlySpan<int> input, Span<ushort> dest, int stride, int bd)
        {
            /* 4-point reversible, orthonormal inverse Walsh-Hadamard in 3.5 adds,
               0.5 shifts per pixel. */

            Span<int> output = stackalloc int[16];
            long a1, b1, c1, d1, e1;
            ReadOnlySpan<int> ip = input;
            Span<int> op = output;

            for (int i = 0; i < 4; i++)
            {
                a1 = ip[0] >> UnitQuantShift;
                c1 = ip[1] >> UnitQuantShift;
                d1 = ip[2] >> UnitQuantShift;
                b1 = ip[3] >> UnitQuantShift;
                a1 += c1;
                d1 -= b1;
                e1 = (a1 - d1) >> 1;
                b1 = e1 - b1;
                c1 = e1 - c1;
                a1 -= b1;
                d1 += c1;
                op[0] = HighbdWrapLow(a1, bd);
                op[1] = HighbdWrapLow(b1, bd);
                op[2] = HighbdWrapLow(c1, bd);
                op[3] = HighbdWrapLow(d1, bd);
                ip = ip.Slice(4);
                op = op.Slice(4);
            }

            ReadOnlySpan<int> ip2 = output;
            for (int i = 0; i < 4; i++)
            {
                a1 = ip2[4 * 0];
                c1 = ip2[4 * 1];
                d1 = ip2[4 * 2];
                b1 = ip2[4 * 3];
                a1 += c1;
                d1 -= b1;
                e1 = (a1 - d1) >> 1;
                b1 = e1 - b1;
                c1 = e1 - c1;
                a1 -= b1;
                d1 += c1;
                dest[stride * 0] = HighbdClipPixelAdd(dest[stride * 0], HighbdWrapLow(a1, bd), bd);
                dest[stride * 1] = HighbdClipPixelAdd(dest[stride * 1], HighbdWrapLow(b1, bd), bd);
                dest[stride * 2] = HighbdClipPixelAdd(dest[stride * 2], HighbdWrapLow(c1, bd), bd);
                dest[stride * 3] = HighbdClipPixelAdd(dest[stride * 3], HighbdWrapLow(d1, bd), bd);

                ip2 = ip2.Slice(1);
                dest = dest.Slice(1);
            }
        }

        [SkipLocalsInit]
        public static void HighbdIwht4X41Add(ReadOnlySpan<int> input, Span<ushort> dest, int stride, int bd)
        {
            long a1, e1;
            Span<int> tmp = stackalloc int[4];
            ReadOnlySpan<int> ip = input;
            Span<int> op = tmp;

            a1 = ip[0] >> UnitQuantShift;
            e1 = a1 >> 1;
            a1 -= e1;
            op[0] = HighbdWrapLow(a1, bd);
            op[1] = op[2] = op[3] = HighbdWrapLow(e1, bd);

            ReadOnlySpan<int> ip2 = tmp;
            for (int i = 0; i < 4; i++)
            {
                e1 = ip2[0] >> 1;
                a1 = ip2[0] - e1;
                dest[stride * 0] = HighbdClipPixelAdd(dest[stride * 0], a1, bd);
                dest[stride * 1] = HighbdClipPixelAdd(dest[stride * 1], e1, bd);
                dest[stride * 2] = HighbdClipPixelAdd(dest[stride * 2], e1, bd);
                dest[stride * 3] = HighbdClipPixelAdd(dest[stride * 3], e1, bd);
                ip2 = ip2.Slice(1);
                dest = dest.Slice(1);
            }
        }

        public static void HighbdIadst4(ReadOnlySpan<int> input, Span<int> output, int bd)
        {
            long s0, s1, s2, s3, s4, s5, s6, s7;
            int x0 = input[0];
            int x1 = input[1];
            int x2 = input[2];
            int x3 = input[3];

            if (DetectInvalidHighbdInput(input, 4) != 0)
            {
                Debug.Assert(false, "invalid highbd txfm input");
                output.Slice(0, 4).Clear();
                return;
            }

            if ((x0 | x1 | x2 | x3) == 0)
            {
                output.Slice(0, 4).Clear();
                return;
            }

            s0 = (long)SinPi19 * x0;
            s1 = (long)SinPi29 * x0;
            s2 = (long)SinPi39 * x1;
            s3 = (long)SinPi49 * x2;
            s4 = (long)SinPi19 * x2;
            s5 = (long)SinPi29 * x3;
            s6 = (long)SinPi49 * x3;
            s7 = HighbdWrapLow(x0 - x2 + x3, bd);

            s0 = s0 + s3 + s5;
            s1 = s1 - s4 - s6;
            s3 = s2;
            s2 = SinPi39 * s7;

            // 1-D transform scaling factor is sqrt(2).
            // The overall dynamic range is 14b (input) + 14b (multiplication scaling)
            // + 1b (addition) = 29b.
            // Hence the output bit depth is 15b.
            output[0] = HighbdWrapLow(DctConstRoundShift(s0 + s3), bd);
            output[1] = HighbdWrapLow(DctConstRoundShift(s1 + s3), bd);
            output[2] = HighbdWrapLow(DctConstRoundShift(s2), bd);
            output[3] = HighbdWrapLow(DctConstRoundShift(s0 + s1 - s3), bd);
        }

        [SkipLocalsInit]
        public static void HighbdIdct4(ReadOnlySpan<int> input, Span<int> output, int bd)
        {
            Span<int> step = stackalloc int[4];
            long temp1, temp2;

            if (DetectInvalidHighbdInput(input, 4) != 0)
            {
                Debug.Assert(false, "invalid highbd txfm input");
                output.Slice(0, 4).Clear();
                return;
            }

            // stage 1
            temp1 = (input[0] + input[2]) * (long)CosPi1664;
            temp2 = (input[0] - input[2]) * (long)CosPi1664;
            step[0] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step[1] = HighbdWrapLow(DctConstRoundShift(temp2), bd);
            temp1 = (input[1] * (long)CosPi2464) - (input[3] * (long)CosPi864);
            temp2 = (input[1] * (long)CosPi864) + (input[3] * (long)CosPi2464);
            step[2] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step[3] = HighbdWrapLow(DctConstRoundShift(temp2), bd);

            // stage 2
            output[0] = HighbdWrapLow(step[0] + step[3], bd);
            output[1] = HighbdWrapLow(step[1] + step[2], bd);
            output[2] = HighbdWrapLow(step[1] - step[2], bd);
            output[3] = HighbdWrapLow(step[0] - step[3], bd);
        }

        [SkipLocalsInit]
        public static void HighbdIdct4X416Add(ReadOnlySpan<int> input, Span<ushort> dest, int stride, int bd)
        {
            Span<int> output = stackalloc int[4 * 4];
            Span<int> outptr = output;
            Span<int> tempIn = stackalloc int[4];
            Span<int> tempOut = stackalloc int[4];

            // Rows
            for (int i = 0; i < 4; ++i)
            {
                HighbdIdct4(input, outptr, bd);
                input = input.Slice(4);
                outptr = outptr.Slice(4);
            }

            // Columns
            for (int i = 0; i < 4; ++i)
            {
                for (int j = 0; j < 4; ++j)
                {
                    tempIn[j] = output[(j * 4) + i];
                }

                HighbdIdct4(tempIn, tempOut, bd);
                for (int j = 0; j < 4; ++j)
                {
                    dest[(j * stride) + i] = HighbdClipPixelAdd(dest[(j * stride) + i],
                        BitUtils.RoundPowerOfTwo(tempOut[j], 4), bd);
                }
            }
        }

        public static void HighbdIdct4X41Add(ReadOnlySpan<int> input, Span<ushort> dest, int stride, int bd)
        {
            long a1;
            int output = HighbdWrapLow(DctConstRoundShift(input[0] * (long)CosPi1664), bd);

            output = HighbdWrapLow(DctConstRoundShift(output * (long)CosPi1664), bd);
            a1 = BitUtils.RoundPowerOfTwo(output, 4);

            for (int i = 0; i < 4; i++)
            {
                dest[0] = HighbdClipPixelAdd(dest[0], a1, bd);
                dest[1] = HighbdClipPixelAdd(dest[1], a1, bd);
                dest[2] = HighbdClipPixelAdd(dest[2], a1, bd);
                dest[3] = HighbdClipPixelAdd(dest[3], a1, bd);
                dest = dest.Slice(stride);
            }
        }

        public static void HighbdIadst8(ReadOnlySpan<int> input, Span<int> output, int bd)
        {
            long s0, s1, s2, s3, s4, s5, s6, s7;
            int x0 = input[7];
            int x1 = input[0];
            int x2 = input[5];
            int x3 = input[2];
            int x4 = input[3];
            int x5 = input[4];
            int x6 = input[1];
            int x7 = input[6];

            if (DetectInvalidHighbdInput(input, 8) != 0)
            {
                Debug.Assert(false, "invalid highbd txfm input");
                output.Slice(0, 8).Clear();
                return;
            }

            if ((x0 | x1 | x2 | x3 | x4 | x5 | x6 | x7) == 0)
            {
                output.Slice(0, 8).Clear();
                return;
            }

            // stage 1
            s0 = ((long)CosPi264 * x0) + ((long)CosPi3064 * x1);
            s1 = ((long)CosPi3064 * x0) - ((long)CosPi264 * x1);
            s2 = ((long)CosPi1064 * x2) + ((long)CosPi2264 * x3);
            s3 = ((long)CosPi2264 * x2) - ((long)CosPi1064 * x3);
            s4 = ((long)CosPi1864 * x4) + ((long)CosPi1464 * x5);
            s5 = ((long)CosPi1464 * x4) - ((long)CosPi1864 * x5);
            s6 = ((long)CosPi2664 * x6) + ((long)CosPi664 * x7);
            s7 = ((long)CosPi664 * x6) - ((long)CosPi2664 * x7);

            x0 = HighbdWrapLow(DctConstRoundShift(s0 + s4), bd);
            x1 = HighbdWrapLow(DctConstRoundShift(s1 + s5), bd);
            x2 = HighbdWrapLow(DctConstRoundShift(s2 + s6), bd);
            x3 = HighbdWrapLow(DctConstRoundShift(s3 + s7), bd);
            x4 = HighbdWrapLow(DctConstRoundShift(s0 - s4), bd);
            x5 = HighbdWrapLow(DctConstRoundShift(s1 - s5), bd);
            x6 = HighbdWrapLow(DctConstRoundShift(s2 - s6), bd);
            x7 = HighbdWrapLow(DctConstRoundShift(s3 - s7), bd);

            // stage 2
            s0 = x0;
            s1 = x1;
            s2 = x2;
            s3 = x3;
            s4 = ((long)CosPi864 * x4) + ((long)CosPi2464 * x5);
            s5 = ((long)CosPi2464 * x4) - ((long)CosPi864 * x5);
            s6 = ((long)-CosPi2464 * x6) + ((long)CosPi864 * x7);
            s7 = ((long)CosPi864 * x6) + ((long)CosPi2464 * x7);

            x0 = HighbdWrapLow(s0 + s2, bd);
            x1 = HighbdWrapLow(s1 + s3, bd);
            x2 = HighbdWrapLow(s0 - s2, bd);
            x3 = HighbdWrapLow(s1 - s3, bd);
            x4 = HighbdWrapLow(DctConstRoundShift(s4 + s6), bd);
            x5 = HighbdWrapLow(DctConstRoundShift(s5 + s7), bd);
            x6 = HighbdWrapLow(DctConstRoundShift(s4 - s6), bd);
            x7 = HighbdWrapLow(DctConstRoundShift(s5 - s7), bd);

            // stage 3
            s2 = (long)CosPi1664 * (x2 + x3);
            s3 = (long)CosPi1664 * (x2 - x3);
            s6 = (long)CosPi1664 * (x6 + x7);
            s7 = (long)CosPi1664 * (x6 - x7);

            x2 = HighbdWrapLow(DctConstRoundShift(s2), bd);
            x3 = HighbdWrapLow(DctConstRoundShift(s3), bd);
            x6 = HighbdWrapLow(DctConstRoundShift(s6), bd);
            x7 = HighbdWrapLow(DctConstRoundShift(s7), bd);

            output[0] = HighbdWrapLow(x0, bd);
            output[1] = HighbdWrapLow(-x4, bd);
            output[2] = HighbdWrapLow(x6, bd);
            output[3] = HighbdWrapLow(-x2, bd);
            output[4] = HighbdWrapLow(x3, bd);
            output[5] = HighbdWrapLow(-x7, bd);
            output[6] = HighbdWrapLow(x5, bd);
            output[7] = HighbdWrapLow(-x1, bd);
        }

        [SkipLocalsInit]
        public static void HighbdIdct8(ReadOnlySpan<int> input, Span<int> output, int bd)
        {
            Span<int> step1 = stackalloc int[8];
            Span<int> step2 = stackalloc int[8];
            long temp1, temp2;

            if (DetectInvalidHighbdInput(input, 8) != 0)
            {
                Debug.Assert(false, "invalid highbd txfm input");
                output.Slice(0, 8).Clear();
                return;
            }

            // stage 1
            step1[0] = input[0];
            step1[2] = input[4];
            step1[1] = input[2];
            step1[3] = input[6];
            temp1 = (input[1] * (long)CosPi2864) - (input[7] * (long)CosPi464);
            temp2 = (input[1] * (long)CosPi464) + (input[7] * (long)CosPi2864);
            step1[4] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step1[7] = HighbdWrapLow(DctConstRoundShift(temp2), bd);
            temp1 = (input[5] * (long)CosPi1264) - (input[3] * (long)CosPi2064);
            temp2 = (input[5] * (long)CosPi2064) + (input[3] * (long)CosPi1264);
            step1[5] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step1[6] = HighbdWrapLow(DctConstRoundShift(temp2), bd);

            // stage 2 & stage 3 - even half
            HighbdIdct4(step1, step1, bd);

            // stage 2 - odd half
            step2[4] = HighbdWrapLow(step1[4] + step1[5], bd);
            step2[5] = HighbdWrapLow(step1[4] - step1[5], bd);
            step2[6] = HighbdWrapLow(-step1[6] + step1[7], bd);
            step2[7] = HighbdWrapLow(step1[6] + step1[7], bd);

            // stage 3 - odd half
            step1[4] = step2[4];
            temp1 = (step2[6] - step2[5]) * (long)CosPi1664;
            temp2 = (step2[5] + step2[6]) * (long)CosPi1664;
            step1[5] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step1[6] = HighbdWrapLow(DctConstRoundShift(temp2), bd);
            step1[7] = step2[7];

            // stage 4
            output[0] = HighbdWrapLow(step1[0] + step1[7], bd);
            output[1] = HighbdWrapLow(step1[1] + step1[6], bd);
            output[2] = HighbdWrapLow(step1[2] + step1[5], bd);
            output[3] = HighbdWrapLow(step1[3] + step1[4], bd);
            output[4] = HighbdWrapLow(step1[3] - step1[4], bd);
            output[5] = HighbdWrapLow(step1[2] - step1[5], bd);
            output[6] = HighbdWrapLow(step1[1] - step1[6], bd);
            output[7] = HighbdWrapLow(step1[0] - step1[7], bd);
        }

        [SkipLocalsInit]
        public static void HighbdIdct8X864Add(ReadOnlySpan<int> input, Span<ushort> dest, int stride, int bd)
        {
            Span<int> output = stackalloc int[8 * 8];
            Span<int> outptr = output;
            Span<int> tempIn = stackalloc int[8];
            Span<int> tempOut = stackalloc int[8];

            // First transform rows
            for (int i = 0; i < 8; ++i)
            {
                HighbdIdct8(input, outptr, bd);
                input = input.Slice(8);
                outptr = outptr.Slice(8);
            }

            // Then transform columns
            for (int i = 0; i < 8; ++i)
            {
                for (int j = 0; j < 8; ++j)
                {
                    tempIn[j] = output[(j * 8) + i];
                }

                HighbdIdct8(tempIn, tempOut, bd);
                for (int j = 0; j < 8; ++j)
                {
                    dest[(j * stride) + i] = HighbdClipPixelAdd(dest[(j * stride) + i],
                        BitUtils.RoundPowerOfTwo(tempOut[j], 5), bd);
                }
            }
        }

        [SkipLocalsInit]
        public static void HighbdIdct8X812Add(ReadOnlySpan<int> input, Span<ushort> dest, int stride, int bd)
        {
            Span<int> output = stackalloc int[8 * 8];
            Span<int> outptr = output;
            Span<int> tempIn = stackalloc int[8];
            Span<int> tempOut = stackalloc int[8];

            output.Clear();

            // First transform rows
            // Only first 4 row has non-zero coefs
            for (int i = 0; i < 4; ++i)
            {
                HighbdIdct8(input, outptr, bd);
                input = input.Slice(8);
                outptr = outptr.Slice(8);
            }

            // Then transform columns
            for (int i = 0; i < 8; ++i)
            {
                for (int j = 0; j < 8; ++j)
                {
                    tempIn[j] = output[(j * 8) + i];
                }

                HighbdIdct8(tempIn, tempOut, bd);
                for (int j = 0; j < 8; ++j)
                {
                    dest[(j * stride) + i] = HighbdClipPixelAdd(dest[(j * stride) + i],
                        BitUtils.RoundPowerOfTwo(tempOut[j], 5), bd);
                }
            }
        }

        public static void VpxHighbdidct8X81AddC(ReadOnlySpan<int> input, Span<ushort> dest, int stride, int bd)
        {
            long a1;
            int output = HighbdWrapLow(DctConstRoundShift(input[0] * (long)CosPi1664), bd);

            output = HighbdWrapLow(DctConstRoundShift(output * (long)CosPi1664), bd);
            a1 = BitUtils.RoundPowerOfTwo(output, 5);
            for (int j = 0; j < 8; ++j)
            {
                for (int i = 0; i < 8; ++i)
                {
                    dest[i] = HighbdClipPixelAdd(dest[i], a1, bd);
                }

                dest = dest.Slice(stride);
            }
        }

        public static void HighbdIadst16(ReadOnlySpan<int> input, Span<int> output, int bd)
        {
            long s0, s1, s2, s3, s4, s5, s6, s7, s8;
            long s9, s10, s11, s12, s13, s14, s15;
            int x0 = input[15];
            int x1 = input[0];
            int x2 = input[13];
            int x3 = input[2];
            int x4 = input[11];
            int x5 = input[4];
            int x6 = input[9];
            int x7 = input[6];
            int x8 = input[7];
            int x9 = input[8];
            int x10 = input[5];
            int x11 = input[10];
            int x12 = input[3];
            int x13 = input[12];
            int x14 = input[1];
            int x15 = input[14];
            if (DetectInvalidHighbdInput(input, 16) != 0)
            {
                Debug.Assert(false, "invalid highbd txfm input");
                output.Slice(0, 16).Clear();
                return;
            }

            if ((x0 | x1 | x2 | x3 | x4 | x5 | x6 | x7 | x8 | x9 | x10 | x11 | x12 | x13 | x14 | x15) == 0)
            {
                output.Slice(0, 16).Clear();
                return;
            }

            // stage 1
            s0 = (x0 * (long)CosPi164) + (x1 * (long)CosPi3164);
            s1 = (x0 * (long)CosPi3164) - (x1 * (long)CosPi164);
            s2 = (x2 * (long)CosPi564) + (x3 * (long)CosPi2764);
            s3 = (x2 * (long)CosPi2764) - (x3 * (long)CosPi564);
            s4 = (x4 * (long)CosPi964) + (x5 * (long)CosPi2364);
            s5 = (x4 * (long)CosPi2364) - (x5 * (long)CosPi964);
            s6 = (x6 * (long)CosPi1364) + (x7 * (long)CosPi1964);
            s7 = (x6 * (long)CosPi1964) - (x7 * (long)CosPi1364);
            s8 = (x8 * (long)CosPi1764) + (x9 * (long)CosPi1564);
            s9 = (x8 * (long)CosPi1564) - (x9 * (long)CosPi1764);
            s10 = (x10 * (long)CosPi2164) + (x11 * (long)CosPi1164);
            s11 = (x10 * (long)CosPi1164) - (x11 * (long)CosPi2164);
            s12 = (x12 * (long)CosPi2564) + (x13 * (long)CosPi764);
            s13 = (x12 * (long)CosPi764) - (x13 * (long)CosPi2564);
            s14 = (x14 * (long)CosPi2964) + (x15 * (long)CosPi364);
            s15 = (x14 * (long)CosPi364) - (x15 * (long)CosPi2964);

            x0 = HighbdWrapLow(DctConstRoundShift(s0 + s8), bd);
            x1 = HighbdWrapLow(DctConstRoundShift(s1 + s9), bd);
            x2 = HighbdWrapLow(DctConstRoundShift(s2 + s10), bd);
            x3 = HighbdWrapLow(DctConstRoundShift(s3 + s11), bd);
            x4 = HighbdWrapLow(DctConstRoundShift(s4 + s12), bd);
            x5 = HighbdWrapLow(DctConstRoundShift(s5 + s13), bd);
            x6 = HighbdWrapLow(DctConstRoundShift(s6 + s14), bd);
            x7 = HighbdWrapLow(DctConstRoundShift(s7 + s15), bd);
            x8 = HighbdWrapLow(DctConstRoundShift(s0 - s8), bd);
            x9 = HighbdWrapLow(DctConstRoundShift(s1 - s9), bd);
            x10 = HighbdWrapLow(DctConstRoundShift(s2 - s10), bd);
            x11 = HighbdWrapLow(DctConstRoundShift(s3 - s11), bd);
            x12 = HighbdWrapLow(DctConstRoundShift(s4 - s12), bd);
            x13 = HighbdWrapLow(DctConstRoundShift(s5 - s13), bd);
            x14 = HighbdWrapLow(DctConstRoundShift(s6 - s14), bd);
            x15 = HighbdWrapLow(DctConstRoundShift(s7 - s15), bd);

            // stage 2
            s0 = x0;
            s1 = x1;
            s2 = x2;
            s3 = x3;
            s4 = x4;
            s5 = x5;
            s6 = x6;
            s7 = x7;
            s8 = (x8 * (long)CosPi464) + (x9 * (long)CosPi2864);
            s9 = (x8 * (long)CosPi2864) - (x9 * (long)CosPi464);
            s10 = (x10 * (long)CosPi2064) + (x11 * (long)CosPi1264);
            s11 = (x10 * (long)CosPi1264) - (x11 * (long)CosPi2064);
            s12 = (-x12 * (long)CosPi2864) + (x13 * (long)CosPi464);
            s13 = (x12 * (long)CosPi464) + (x13 * (long)CosPi2864);
            s14 = (-x14 * (long)CosPi1264) + (x15 * (long)CosPi2064);
            s15 = (x14 * (long)CosPi2064) + (x15 * (long)CosPi1264);

            x0 = HighbdWrapLow(s0 + s4, bd);
            x1 = HighbdWrapLow(s1 + s5, bd);
            x2 = HighbdWrapLow(s2 + s6, bd);
            x3 = HighbdWrapLow(s3 + s7, bd);
            x4 = HighbdWrapLow(s0 - s4, bd);
            x5 = HighbdWrapLow(s1 - s5, bd);
            x6 = HighbdWrapLow(s2 - s6, bd);
            x7 = HighbdWrapLow(s3 - s7, bd);
            x8 = HighbdWrapLow(DctConstRoundShift(s8 + s12), bd);
            x9 = HighbdWrapLow(DctConstRoundShift(s9 + s13), bd);
            x10 = HighbdWrapLow(DctConstRoundShift(s10 + s14), bd);
            x11 = HighbdWrapLow(DctConstRoundShift(s11 + s15), bd);
            x12 = HighbdWrapLow(DctConstRoundShift(s8 - s12), bd);
            x13 = HighbdWrapLow(DctConstRoundShift(s9 - s13), bd);
            x14 = HighbdWrapLow(DctConstRoundShift(s10 - s14), bd);
            x15 = HighbdWrapLow(DctConstRoundShift(s11 - s15), bd);

            // stage 3
            s0 = x0;
            s1 = x1;
            s2 = x2;
            s3 = x3;
            s4 = (x4 * (long)CosPi864) + (x5 * (long)CosPi2464);
            s5 = (x4 * (long)CosPi2464) - (x5 * (long)CosPi864);
            s6 = (-x6 * (long)CosPi2464) + (x7 * (long)CosPi864);
            s7 = (x6 * (long)CosPi864) + (x7 * (long)CosPi2464);
            s8 = x8;
            s9 = x9;
            s10 = x10;
            s11 = x11;
            s12 = (x12 * (long)CosPi864) + (x13 * (long)CosPi2464);
            s13 = (x12 * (long)CosPi2464) - (x13 * (long)CosPi864);
            s14 = (-x14 * (long)CosPi2464) + (x15 * (long)CosPi864);
            s15 = (x14 * (long)CosPi864) + (x15 * (long)CosPi2464);

            x0 = HighbdWrapLow(s0 + s2, bd);
            x1 = HighbdWrapLow(s1 + s3, bd);
            x2 = HighbdWrapLow(s0 - s2, bd);
            x3 = HighbdWrapLow(s1 - s3, bd);
            x4 = HighbdWrapLow(DctConstRoundShift(s4 + s6), bd);
            x5 = HighbdWrapLow(DctConstRoundShift(s5 + s7), bd);
            x6 = HighbdWrapLow(DctConstRoundShift(s4 - s6), bd);
            x7 = HighbdWrapLow(DctConstRoundShift(s5 - s7), bd);
            x8 = HighbdWrapLow(s8 + s10, bd);
            x9 = HighbdWrapLow(s9 + s11, bd);
            x10 = HighbdWrapLow(s8 - s10, bd);
            x11 = HighbdWrapLow(s9 - s11, bd);
            x12 = HighbdWrapLow(DctConstRoundShift(s12 + s14), bd);
            x13 = HighbdWrapLow(DctConstRoundShift(s13 + s15), bd);
            x14 = HighbdWrapLow(DctConstRoundShift(s12 - s14), bd);
            x15 = HighbdWrapLow(DctConstRoundShift(s13 - s15), bd);

            // stage 4
            s2 = (long)-CosPi1664 * (x2 + x3);
            s3 = (long)CosPi1664 * (x2 - x3);
            s6 = (long)CosPi1664 * (x6 + x7);
            s7 = (long)CosPi1664 * (-x6 + x7);
            s10 = (long)CosPi1664 * (x10 + x11);
            s11 = (long)CosPi1664 * (-x10 + x11);
            s14 = (long)-CosPi1664 * (x14 + x15);
            s15 = (long)CosPi1664 * (x14 - x15);

            x2 = HighbdWrapLow(DctConstRoundShift(s2), bd);
            x3 = HighbdWrapLow(DctConstRoundShift(s3), bd);
            x6 = HighbdWrapLow(DctConstRoundShift(s6), bd);
            x7 = HighbdWrapLow(DctConstRoundShift(s7), bd);
            x10 = HighbdWrapLow(DctConstRoundShift(s10), bd);
            x11 = HighbdWrapLow(DctConstRoundShift(s11), bd);
            x14 = HighbdWrapLow(DctConstRoundShift(s14), bd);
            x15 = HighbdWrapLow(DctConstRoundShift(s15), bd);

            output[0] = HighbdWrapLow(x0, bd);
            output[1] = HighbdWrapLow(-x8, bd);
            output[2] = HighbdWrapLow(x12, bd);
            output[3] = HighbdWrapLow(-x4, bd);
            output[4] = HighbdWrapLow(x6, bd);
            output[5] = HighbdWrapLow(x14, bd);
            output[6] = HighbdWrapLow(x10, bd);
            output[7] = HighbdWrapLow(x2, bd);
            output[8] = HighbdWrapLow(x3, bd);
            output[9] = HighbdWrapLow(x11, bd);
            output[10] = HighbdWrapLow(x15, bd);
            output[11] = HighbdWrapLow(x7, bd);
            output[12] = HighbdWrapLow(x5, bd);
            output[13] = HighbdWrapLow(-x13, bd);
            output[14] = HighbdWrapLow(x9, bd);
            output[15] = HighbdWrapLow(-x1, bd);
        }

        [SkipLocalsInit]
        public static void HighbdIdct16(ReadOnlySpan<int> input, Span<int> output, int bd)
        {
            Span<int> step1 = stackalloc int[16];
            Span<int> step2 = stackalloc int[16];
            long temp1, temp2;

            if (DetectInvalidHighbdInput(input, 16) != 0)
            {
                Debug.Assert(false, "invalid highbd txfm input");
                output.Slice(0, 16).Clear();
                return;
            }

            // stage 1
            step1[0] = input[0 / 2];
            step1[1] = input[16 / 2];
            step1[2] = input[8 / 2];
            step1[3] = input[24 / 2];
            step1[4] = input[4 / 2];
            step1[5] = input[20 / 2];
            step1[6] = input[12 / 2];
            step1[7] = input[28 / 2];
            step1[8] = input[2 / 2];
            step1[9] = input[18 / 2];
            step1[10] = input[10 / 2];
            step1[11] = input[26 / 2];
            step1[12] = input[6 / 2];
            step1[13] = input[22 / 2];
            step1[14] = input[14 / 2];
            step1[15] = input[30 / 2];

            // stage 2
            step2[0] = step1[0];
            step2[1] = step1[1];
            step2[2] = step1[2];
            step2[3] = step1[3];
            step2[4] = step1[4];
            step2[5] = step1[5];
            step2[6] = step1[6];
            step2[7] = step1[7];

            temp1 = (step1[8] * (long)CosPi3064) - (step1[15] * (long)CosPi264);
            temp2 = (step1[8] * (long)CosPi264) + (step1[15] * (long)CosPi3064);
            step2[8] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step2[15] = HighbdWrapLow(DctConstRoundShift(temp2), bd);

            temp1 = (step1[9] * (long)CosPi1464) - (step1[14] * (long)CosPi1864);
            temp2 = (step1[9] * (long)CosPi1864) + (step1[14] * (long)CosPi1464);
            step2[9] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step2[14] = HighbdWrapLow(DctConstRoundShift(temp2), bd);

            temp1 = (step1[10] * (long)CosPi2264) - (step1[13] * (long)CosPi1064);
            temp2 = (step1[10] * (long)CosPi1064) + (step1[13] * (long)CosPi2264);
            step2[10] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step2[13] = HighbdWrapLow(DctConstRoundShift(temp2), bd);

            temp1 = (step1[11] * (long)CosPi664) - (step1[12] * (long)CosPi2664);
            temp2 = (step1[11] * (long)CosPi2664) + (step1[12] * (long)CosPi664);
            step2[11] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step2[12] = HighbdWrapLow(DctConstRoundShift(temp2), bd);

            // stage 3
            step1[0] = step2[0];
            step1[1] = step2[1];
            step1[2] = step2[2];
            step1[3] = step2[3];

            temp1 = (step2[4] * (long)CosPi2864) - (step2[7] * (long)CosPi464);
            temp2 = (step2[4] * (long)CosPi464) + (step2[7] * (long)CosPi2864);
            step1[4] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step1[7] = HighbdWrapLow(DctConstRoundShift(temp2), bd);
            temp1 = (step2[5] * (long)CosPi1264) - (step2[6] * (long)CosPi2064);
            temp2 = (step2[5] * (long)CosPi2064) + (step2[6] * (long)CosPi1264);
            step1[5] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step1[6] = HighbdWrapLow(DctConstRoundShift(temp2), bd);

            step1[8] = HighbdWrapLow(step2[8] + step2[9], bd);
            step1[9] = HighbdWrapLow(step2[8] - step2[9], bd);
            step1[10] = HighbdWrapLow(-step2[10] + step2[11], bd);
            step1[11] = HighbdWrapLow(step2[10] + step2[11], bd);
            step1[12] = HighbdWrapLow(step2[12] + step2[13], bd);
            step1[13] = HighbdWrapLow(step2[12] - step2[13], bd);
            step1[14] = HighbdWrapLow(-step2[14] + step2[15], bd);
            step1[15] = HighbdWrapLow(step2[14] + step2[15], bd);

            // stage 4
            temp1 = (step1[0] + step1[1]) * (long)CosPi1664;
            temp2 = (step1[0] - step1[1]) * (long)CosPi1664;
            step2[0] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step2[1] = HighbdWrapLow(DctConstRoundShift(temp2), bd);
            temp1 = (step1[2] * (long)CosPi2464) - (step1[3] * (long)CosPi864);
            temp2 = (step1[2] * (long)CosPi864) + (step1[3] * (long)CosPi2464);
            step2[2] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step2[3] = HighbdWrapLow(DctConstRoundShift(temp2), bd);
            step2[4] = HighbdWrapLow(step1[4] + step1[5], bd);
            step2[5] = HighbdWrapLow(step1[4] - step1[5], bd);
            step2[6] = HighbdWrapLow(-step1[6] + step1[7], bd);
            step2[7] = HighbdWrapLow(step1[6] + step1[7], bd);

            step2[8] = step1[8];
            step2[15] = step1[15];
            temp1 = (-step1[9] * (long)CosPi864) + (step1[14] * (long)CosPi2464);
            temp2 = (step1[9] * (long)CosPi2464) + (step1[14] * (long)CosPi864);
            step2[9] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step2[14] = HighbdWrapLow(DctConstRoundShift(temp2), bd);
            temp1 = (-step1[10] * (long)CosPi2464) - (step1[13] * (long)CosPi864);
            temp2 = (-step1[10] * (long)CosPi864) + (step1[13] * (long)CosPi2464);
            step2[10] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step2[13] = HighbdWrapLow(DctConstRoundShift(temp2), bd);
            step2[11] = step1[11];
            step2[12] = step1[12];

            // stage 5
            step1[0] = HighbdWrapLow(step2[0] + step2[3], bd);
            step1[1] = HighbdWrapLow(step2[1] + step2[2], bd);
            step1[2] = HighbdWrapLow(step2[1] - step2[2], bd);
            step1[3] = HighbdWrapLow(step2[0] - step2[3], bd);
            step1[4] = step2[4];
            temp1 = (step2[6] - step2[5]) * (long)CosPi1664;
            temp2 = (step2[5] + step2[6]) * (long)CosPi1664;
            step1[5] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step1[6] = HighbdWrapLow(DctConstRoundShift(temp2), bd);
            step1[7] = step2[7];

            step1[8] = HighbdWrapLow(step2[8] + step2[11], bd);
            step1[9] = HighbdWrapLow(step2[9] + step2[10], bd);
            step1[10] = HighbdWrapLow(step2[9] - step2[10], bd);
            step1[11] = HighbdWrapLow(step2[8] - step2[11], bd);
            step1[12] = HighbdWrapLow(-step2[12] + step2[15], bd);
            step1[13] = HighbdWrapLow(-step2[13] + step2[14], bd);
            step1[14] = HighbdWrapLow(step2[13] + step2[14], bd);
            step1[15] = HighbdWrapLow(step2[12] + step2[15], bd);

            // stage 6
            step2[0] = HighbdWrapLow(step1[0] + step1[7], bd);
            step2[1] = HighbdWrapLow(step1[1] + step1[6], bd);
            step2[2] = HighbdWrapLow(step1[2] + step1[5], bd);
            step2[3] = HighbdWrapLow(step1[3] + step1[4], bd);
            step2[4] = HighbdWrapLow(step1[3] - step1[4], bd);
            step2[5] = HighbdWrapLow(step1[2] - step1[5], bd);
            step2[6] = HighbdWrapLow(step1[1] - step1[6], bd);
            step2[7] = HighbdWrapLow(step1[0] - step1[7], bd);
            step2[8] = step1[8];
            step2[9] = step1[9];
            temp1 = (-step1[10] + step1[13]) * (long)CosPi1664;
            temp2 = (step1[10] + step1[13]) * (long)CosPi1664;
            step2[10] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step2[13] = HighbdWrapLow(DctConstRoundShift(temp2), bd);
            temp1 = (-step1[11] + step1[12]) * (long)CosPi1664;
            temp2 = (step1[11] + step1[12]) * (long)CosPi1664;
            step2[11] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step2[12] = HighbdWrapLow(DctConstRoundShift(temp2), bd);
            step2[14] = step1[14];
            step2[15] = step1[15];

            // stage 7
            output[0] = HighbdWrapLow(step2[0] + step2[15], bd);
            output[1] = HighbdWrapLow(step2[1] + step2[14], bd);
            output[2] = HighbdWrapLow(step2[2] + step2[13], bd);
            output[3] = HighbdWrapLow(step2[3] + step2[12], bd);
            output[4] = HighbdWrapLow(step2[4] + step2[11], bd);
            output[5] = HighbdWrapLow(step2[5] + step2[10], bd);
            output[6] = HighbdWrapLow(step2[6] + step2[9], bd);
            output[7] = HighbdWrapLow(step2[7] + step2[8], bd);
            output[8] = HighbdWrapLow(step2[7] - step2[8], bd);
            output[9] = HighbdWrapLow(step2[6] - step2[9], bd);
            output[10] = HighbdWrapLow(step2[5] - step2[10], bd);
            output[11] = HighbdWrapLow(step2[4] - step2[11], bd);
            output[12] = HighbdWrapLow(step2[3] - step2[12], bd);
            output[13] = HighbdWrapLow(step2[2] - step2[13], bd);
            output[14] = HighbdWrapLow(step2[1] - step2[14], bd);
            output[15] = HighbdWrapLow(step2[0] - step2[15], bd);
        }

        [SkipLocalsInit]
        public static void HighbdIdct16X16256Add(ReadOnlySpan<int> input, Span<ushort> dest, int stride, int bd)
        {
            Span<int> output = stackalloc int[16 * 16];
            Span<int> outptr = output;
            Span<int> tempIn = stackalloc int[16];
            Span<int> tempOut = stackalloc int[16];

            // First transform rows
            for (int i = 0; i < 16; ++i)
            {
                HighbdIdct16(input, outptr, bd);
                input = input.Slice(16);
                outptr = outptr.Slice(16);
            }

            // Then transform columns
            for (int i = 0; i < 16; ++i)
            {
                for (int j = 0; j < 16; ++j)
                {
                    tempIn[j] = output[(j * 16) + i];
                }

                HighbdIdct16(tempIn, tempOut, bd);
                for (int j = 0; j < 16; ++j)
                {
                    dest[(j * stride) + i] = HighbdClipPixelAdd(dest[(j * stride) + i],
                        BitUtils.RoundPowerOfTwo(tempOut[j], 6), bd);
                }
            }
        }

        [SkipLocalsInit]
        public static void HighbdIdct16X1638Add(ReadOnlySpan<int> input, Span<ushort> dest, int stride, int bd)
        {
            Span<int> output = stackalloc int[16 * 16];
            Span<int> outptr = output;
            Span<int> tempIn = stackalloc int[16];
            Span<int> tempOut = stackalloc int[16];

            output.Clear();

            // First transform rows. Since all non-zero dct coefficients are in
            // upper-left 8x8 area, we only need to calculate first 8 rows here.
            for (int i = 0; i < 8; ++i)
            {
                HighbdIdct16(input, outptr, bd);
                input = input.Slice(16);
                outptr = outptr.Slice(16);
            }

            // Then transform columns
            for (int i = 0; i < 16; ++i)
            {
                Span<ushort> destT = dest;
                for (int j = 0; j < 16; ++j)
                {
                    tempIn[j] = output[(j * 16) + i];
                }

                HighbdIdct16(tempIn, tempOut, bd);
                for (int j = 0; j < 16; ++j)
                {
                    destT[i] = HighbdClipPixelAdd(destT[i], BitUtils.RoundPowerOfTwo(tempOut[j], 6), bd);
                    destT = destT.Slice(stride);
                }
            }
        }

        [SkipLocalsInit]
        public static void HighbdIdct16X1610Add(ReadOnlySpan<int> input, Span<ushort> dest, int stride, int bd)
        {
            Span<int> output = stackalloc int[16 * 16];
            Span<int> outptr = output;
            Span<int> tempIn = stackalloc int[16];
            Span<int> tempOut = stackalloc int[16];

            output.Clear();

            // First transform rows. Since all non-zero dct coefficients are in
            // upper-left 4x4 area, we only need to calculate first 4 rows here.
            for (int i = 0; i < 4; ++i)
            {
                HighbdIdct16(input, outptr, bd);
                input = input.Slice(16);
                outptr = outptr.Slice(16);
            }

            // Then transform columns
            for (int i = 0; i < 16; ++i)
            {
                for (int j = 0; j < 16; ++j)
                {
                    tempIn[j] = output[(j * 16) + i];
                }

                HighbdIdct16(tempIn, tempOut, bd);
                for (int j = 0; j < 16; ++j)
                {
                    dest[(j * stride) + i] = HighbdClipPixelAdd(dest[(j * stride) + i],
                        BitUtils.RoundPowerOfTwo(tempOut[j], 6), bd);
                }
            }
        }

        public static void HighbdIdct16X161Add(ReadOnlySpan<int> input, Span<ushort> dest, int stride, int bd)
        {
            long a1;
            int output = HighbdWrapLow(DctConstRoundShift(input[0] * (long)CosPi1664), bd);

            output = HighbdWrapLow(DctConstRoundShift(output * (long)CosPi1664), bd);
            a1 = BitUtils.RoundPowerOfTwo(output, 6);
            for (int j = 0; j < 16; ++j)
            {
                for (int i = 0; i < 16; ++i)
                {
                    dest[i] = HighbdClipPixelAdd(dest[i], a1, bd);
                }

                dest = dest.Slice(stride);
            }
        }

        [SkipLocalsInit]
        public static void HighbdIdct32(ReadOnlySpan<int> input, Span<int> output, int bd)
        {
            Span<int> step1 = stackalloc int[32];
            Span<int> step2 = stackalloc int[32];
            long temp1, temp2;

            if (DetectInvalidHighbdInput(input, 32) != 0)
            {
                Debug.Assert(false, "invalid highbd txfm input");
                output.Slice(0, 32).Clear();
                return;
            }

            // stage 1
            step1[0] = input[0];
            step1[1] = input[16];
            step1[2] = input[8];
            step1[3] = input[24];
            step1[4] = input[4];
            step1[5] = input[20];
            step1[6] = input[12];
            step1[7] = input[28];
            step1[8] = input[2];
            step1[9] = input[18];
            step1[10] = input[10];
            step1[11] = input[26];
            step1[12] = input[6];
            step1[13] = input[22];
            step1[14] = input[14];
            step1[15] = input[30];

            temp1 = (input[1] * (long)CosPi3164) - (input[31] * (long)CosPi164);
            temp2 = (input[1] * (long)CosPi164) + (input[31] * (long)CosPi3164);
            step1[16] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step1[31] = HighbdWrapLow(DctConstRoundShift(temp2), bd);

            temp1 = (input[17] * (long)CosPi1564) - (input[15] * (long)CosPi1764);
            temp2 = (input[17] * (long)CosPi1764) + (input[15] * (long)CosPi1564);
            step1[17] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step1[30] = HighbdWrapLow(DctConstRoundShift(temp2), bd);

            temp1 = (input[9] * (long)CosPi2364) - (input[23] * (long)CosPi964);
            temp2 = (input[9] * (long)CosPi964) + (input[23] * (long)CosPi2364);
            step1[18] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step1[29] = HighbdWrapLow(DctConstRoundShift(temp2), bd);

            temp1 = (input[25] * (long)CosPi764) - (input[7] * (long)CosPi2564);
            temp2 = (input[25] * (long)CosPi2564) + (input[7] * (long)CosPi764);
            step1[19] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step1[28] = HighbdWrapLow(DctConstRoundShift(temp2), bd);

            temp1 = (input[5] * (long)CosPi2764) - (input[27] * (long)CosPi564);
            temp2 = (input[5] * (long)CosPi564) + (input[27] * (long)CosPi2764);
            step1[20] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step1[27] = HighbdWrapLow(DctConstRoundShift(temp2), bd);

            temp1 = (input[21] * (long)CosPi1164) - (input[11] * (long)CosPi2164);
            temp2 = (input[21] * (long)CosPi2164) + (input[11] * (long)CosPi1164);
            step1[21] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step1[26] = HighbdWrapLow(DctConstRoundShift(temp2), bd);

            temp1 = (input[13] * (long)CosPi1964) - (input[19] * (long)CosPi1364);
            temp2 = (input[13] * (long)CosPi1364) + (input[19] * (long)CosPi1964);
            step1[22] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step1[25] = HighbdWrapLow(DctConstRoundShift(temp2), bd);

            temp1 = (input[29] * (long)CosPi364) - (input[3] * (long)CosPi2964);
            temp2 = (input[29] * (long)CosPi2964) + (input[3] * (long)CosPi364);
            step1[23] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step1[24] = HighbdWrapLow(DctConstRoundShift(temp2), bd);

            // stage 2
            step2[0] = step1[0];
            step2[1] = step1[1];
            step2[2] = step1[2];
            step2[3] = step1[3];
            step2[4] = step1[4];
            step2[5] = step1[5];
            step2[6] = step1[6];
            step2[7] = step1[7];

            temp1 = (step1[8] * (long)CosPi3064) - (step1[15] * (long)CosPi264);
            temp2 = (step1[8] * (long)CosPi264) + (step1[15] * (long)CosPi3064);
            step2[8] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step2[15] = HighbdWrapLow(DctConstRoundShift(temp2), bd);

            temp1 = (step1[9] * (long)CosPi1464) - (step1[14] * (long)CosPi1864);
            temp2 = (step1[9] * (long)CosPi1864) + (step1[14] * (long)CosPi1464);
            step2[9] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step2[14] = HighbdWrapLow(DctConstRoundShift(temp2), bd);

            temp1 = (step1[10] * (long)CosPi2264) - (step1[13] * (long)CosPi1064);
            temp2 = (step1[10] * (long)CosPi1064) + (step1[13] * (long)CosPi2264);
            step2[10] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step2[13] = HighbdWrapLow(DctConstRoundShift(temp2), bd);

            temp1 = (step1[11] * (long)CosPi664) - (step1[12] * (long)CosPi2664);
            temp2 = (step1[11] * (long)CosPi2664) + (step1[12] * (long)CosPi664);
            step2[11] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step2[12] = HighbdWrapLow(DctConstRoundShift(temp2), bd);

            step2[16] = HighbdWrapLow(step1[16] + step1[17], bd);
            step2[17] = HighbdWrapLow(step1[16] - step1[17], bd);
            step2[18] = HighbdWrapLow(-step1[18] + step1[19], bd);
            step2[19] = HighbdWrapLow(step1[18] + step1[19], bd);
            step2[20] = HighbdWrapLow(step1[20] + step1[21], bd);
            step2[21] = HighbdWrapLow(step1[20] - step1[21], bd);
            step2[22] = HighbdWrapLow(-step1[22] + step1[23], bd);
            step2[23] = HighbdWrapLow(step1[22] + step1[23], bd);
            step2[24] = HighbdWrapLow(step1[24] + step1[25], bd);
            step2[25] = HighbdWrapLow(step1[24] - step1[25], bd);
            step2[26] = HighbdWrapLow(-step1[26] + step1[27], bd);
            step2[27] = HighbdWrapLow(step1[26] + step1[27], bd);
            step2[28] = HighbdWrapLow(step1[28] + step1[29], bd);
            step2[29] = HighbdWrapLow(step1[28] - step1[29], bd);
            step2[30] = HighbdWrapLow(-step1[30] + step1[31], bd);
            step2[31] = HighbdWrapLow(step1[30] + step1[31], bd);

            // stage 3
            step1[0] = step2[0];
            step1[1] = step2[1];
            step1[2] = step2[2];
            step1[3] = step2[3];

            temp1 = (step2[4] * (long)CosPi2864) - (step2[7] * (long)CosPi464);
            temp2 = (step2[4] * (long)CosPi464) + (step2[7] * (long)CosPi2864);
            step1[4] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step1[7] = HighbdWrapLow(DctConstRoundShift(temp2), bd);
            temp1 = (step2[5] * (long)CosPi1264) - (step2[6] * (long)CosPi2064);
            temp2 = (step2[5] * (long)CosPi2064) + (step2[6] * (long)CosPi1264);
            step1[5] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step1[6] = HighbdWrapLow(DctConstRoundShift(temp2), bd);

            step1[8] = HighbdWrapLow(step2[8] + step2[9], bd);
            step1[9] = HighbdWrapLow(step2[8] - step2[9], bd);
            step1[10] = HighbdWrapLow(-step2[10] + step2[11], bd);
            step1[11] = HighbdWrapLow(step2[10] + step2[11], bd);
            step1[12] = HighbdWrapLow(step2[12] + step2[13], bd);
            step1[13] = HighbdWrapLow(step2[12] - step2[13], bd);
            step1[14] = HighbdWrapLow(-step2[14] + step2[15], bd);
            step1[15] = HighbdWrapLow(step2[14] + step2[15], bd);

            step1[16] = step2[16];
            step1[31] = step2[31];
            temp1 = (-step2[17] * (long)CosPi464) + (step2[30] * (long)CosPi2864);
            temp2 = (step2[17] * (long)CosPi2864) + (step2[30] * (long)CosPi464);
            step1[17] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step1[30] = HighbdWrapLow(DctConstRoundShift(temp2), bd);
            temp1 = (-step2[18] * (long)CosPi2864) - (step2[29] * (long)CosPi464);
            temp2 = (-step2[18] * (long)CosPi464) + (step2[29] * (long)CosPi2864);
            step1[18] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step1[29] = HighbdWrapLow(DctConstRoundShift(temp2), bd);
            step1[19] = step2[19];
            step1[20] = step2[20];
            temp1 = (-step2[21] * (long)CosPi2064) + (step2[26] * (long)CosPi1264);
            temp2 = (step2[21] * (long)CosPi1264) + (step2[26] * (long)CosPi2064);
            step1[21] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step1[26] = HighbdWrapLow(DctConstRoundShift(temp2), bd);
            temp1 = (-step2[22] * (long)CosPi1264) - (step2[25] * (long)CosPi2064);
            temp2 = (-step2[22] * (long)CosPi2064) + (step2[25] * (long)CosPi1264);
            step1[22] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step1[25] = HighbdWrapLow(DctConstRoundShift(temp2), bd);
            step1[23] = step2[23];
            step1[24] = step2[24];
            step1[27] = step2[27];
            step1[28] = step2[28];

            // stage 4
            temp1 = (step1[0] + step1[1]) * (long)CosPi1664;
            temp2 = (step1[0] - step1[1]) * (long)CosPi1664;
            step2[0] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step2[1] = HighbdWrapLow(DctConstRoundShift(temp2), bd);
            temp1 = (step1[2] * (long)CosPi2464) - (step1[3] * (long)CosPi864);
            temp2 = (step1[2] * (long)CosPi864) + (step1[3] * (long)CosPi2464);
            step2[2] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step2[3] = HighbdWrapLow(DctConstRoundShift(temp2), bd);
            step2[4] = HighbdWrapLow(step1[4] + step1[5], bd);
            step2[5] = HighbdWrapLow(step1[4] - step1[5], bd);
            step2[6] = HighbdWrapLow(-step1[6] + step1[7], bd);
            step2[7] = HighbdWrapLow(step1[6] + step1[7], bd);

            step2[8] = step1[8];
            step2[15] = step1[15];
            temp1 = (-step1[9] * (long)CosPi864) + (step1[14] * (long)CosPi2464);
            temp2 = (step1[9] * (long)CosPi2464) + (step1[14] * (long)CosPi864);
            step2[9] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step2[14] = HighbdWrapLow(DctConstRoundShift(temp2), bd);
            temp1 = (-step1[10] * (long)CosPi2464) - (step1[13] * (long)CosPi864);
            temp2 = (-step1[10] * (long)CosPi864) + (step1[13] * (long)CosPi2464);
            step2[10] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step2[13] = HighbdWrapLow(DctConstRoundShift(temp2), bd);
            step2[11] = step1[11];
            step2[12] = step1[12];

            step2[16] = HighbdWrapLow(step1[16] + step1[19], bd);
            step2[17] = HighbdWrapLow(step1[17] + step1[18], bd);
            step2[18] = HighbdWrapLow(step1[17] - step1[18], bd);
            step2[19] = HighbdWrapLow(step1[16] - step1[19], bd);
            step2[20] = HighbdWrapLow(-step1[20] + step1[23], bd);
            step2[21] = HighbdWrapLow(-step1[21] + step1[22], bd);
            step2[22] = HighbdWrapLow(step1[21] + step1[22], bd);
            step2[23] = HighbdWrapLow(step1[20] + step1[23], bd);

            step2[24] = HighbdWrapLow(step1[24] + step1[27], bd);
            step2[25] = HighbdWrapLow(step1[25] + step1[26], bd);
            step2[26] = HighbdWrapLow(step1[25] - step1[26], bd);
            step2[27] = HighbdWrapLow(step1[24] - step1[27], bd);
            step2[28] = HighbdWrapLow(-step1[28] + step1[31], bd);
            step2[29] = HighbdWrapLow(-step1[29] + step1[30], bd);
            step2[30] = HighbdWrapLow(step1[29] + step1[30], bd);
            step2[31] = HighbdWrapLow(step1[28] + step1[31], bd);

            // stage 5
            step1[0] = HighbdWrapLow(step2[0] + step2[3], bd);
            step1[1] = HighbdWrapLow(step2[1] + step2[2], bd);
            step1[2] = HighbdWrapLow(step2[1] - step2[2], bd);
            step1[3] = HighbdWrapLow(step2[0] - step2[3], bd);
            step1[4] = step2[4];
            temp1 = (step2[6] - step2[5]) * (long)CosPi1664;
            temp2 = (step2[5] + step2[6]) * (long)CosPi1664;
            step1[5] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step1[6] = HighbdWrapLow(DctConstRoundShift(temp2), bd);
            step1[7] = step2[7];

            step1[8] = HighbdWrapLow(step2[8] + step2[11], bd);
            step1[9] = HighbdWrapLow(step2[9] + step2[10], bd);
            step1[10] = HighbdWrapLow(step2[9] - step2[10], bd);
            step1[11] = HighbdWrapLow(step2[8] - step2[11], bd);
            step1[12] = HighbdWrapLow(-step2[12] + step2[15], bd);
            step1[13] = HighbdWrapLow(-step2[13] + step2[14], bd);
            step1[14] = HighbdWrapLow(step2[13] + step2[14], bd);
            step1[15] = HighbdWrapLow(step2[12] + step2[15], bd);

            step1[16] = step2[16];
            step1[17] = step2[17];
            temp1 = (-step2[18] * (long)CosPi864) + (step2[29] * (long)CosPi2464);
            temp2 = (step2[18] * (long)CosPi2464) + (step2[29] * (long)CosPi864);
            step1[18] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step1[29] = HighbdWrapLow(DctConstRoundShift(temp2), bd);
            temp1 = (-step2[19] * (long)CosPi864) + (step2[28] * (long)CosPi2464);
            temp2 = (step2[19] * (long)CosPi2464) + (step2[28] * (long)CosPi864);
            step1[19] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step1[28] = HighbdWrapLow(DctConstRoundShift(temp2), bd);
            temp1 = (-step2[20] * (long)CosPi2464) - (step2[27] * (long)CosPi864);
            temp2 = (-step2[20] * (long)CosPi864) + (step2[27] * (long)CosPi2464);
            step1[20] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step1[27] = HighbdWrapLow(DctConstRoundShift(temp2), bd);
            temp1 = (-step2[21] * (long)CosPi2464) - (step2[26] * (long)CosPi864);
            temp2 = (-step2[21] * (long)CosPi864) + (step2[26] * (long)CosPi2464);
            step1[21] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step1[26] = HighbdWrapLow(DctConstRoundShift(temp2), bd);
            step1[22] = step2[22];
            step1[23] = step2[23];
            step1[24] = step2[24];
            step1[25] = step2[25];
            step1[30] = step2[30];
            step1[31] = step2[31];

            // stage 6
            step2[0] = HighbdWrapLow(step1[0] + step1[7], bd);
            step2[1] = HighbdWrapLow(step1[1] + step1[6], bd);
            step2[2] = HighbdWrapLow(step1[2] + step1[5], bd);
            step2[3] = HighbdWrapLow(step1[3] + step1[4], bd);
            step2[4] = HighbdWrapLow(step1[3] - step1[4], bd);
            step2[5] = HighbdWrapLow(step1[2] - step1[5], bd);
            step2[6] = HighbdWrapLow(step1[1] - step1[6], bd);
            step2[7] = HighbdWrapLow(step1[0] - step1[7], bd);
            step2[8] = step1[8];
            step2[9] = step1[9];
            temp1 = (-step1[10] + step1[13]) * (long)CosPi1664;
            temp2 = (step1[10] + step1[13]) * (long)CosPi1664;
            step2[10] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step2[13] = HighbdWrapLow(DctConstRoundShift(temp2), bd);
            temp1 = (-step1[11] + step1[12]) * (long)CosPi1664;
            temp2 = (step1[11] + step1[12]) * (long)CosPi1664;
            step2[11] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step2[12] = HighbdWrapLow(DctConstRoundShift(temp2), bd);
            step2[14] = step1[14];
            step2[15] = step1[15];

            step2[16] = HighbdWrapLow(step1[16] + step1[23], bd);
            step2[17] = HighbdWrapLow(step1[17] + step1[22], bd);
            step2[18] = HighbdWrapLow(step1[18] + step1[21], bd);
            step2[19] = HighbdWrapLow(step1[19] + step1[20], bd);
            step2[20] = HighbdWrapLow(step1[19] - step1[20], bd);
            step2[21] = HighbdWrapLow(step1[18] - step1[21], bd);
            step2[22] = HighbdWrapLow(step1[17] - step1[22], bd);
            step2[23] = HighbdWrapLow(step1[16] - step1[23], bd);

            step2[24] = HighbdWrapLow(-step1[24] + step1[31], bd);
            step2[25] = HighbdWrapLow(-step1[25] + step1[30], bd);
            step2[26] = HighbdWrapLow(-step1[26] + step1[29], bd);
            step2[27] = HighbdWrapLow(-step1[27] + step1[28], bd);
            step2[28] = HighbdWrapLow(step1[27] + step1[28], bd);
            step2[29] = HighbdWrapLow(step1[26] + step1[29], bd);
            step2[30] = HighbdWrapLow(step1[25] + step1[30], bd);
            step2[31] = HighbdWrapLow(step1[24] + step1[31], bd);

            // stage 7
            step1[0] = HighbdWrapLow(step2[0] + step2[15], bd);
            step1[1] = HighbdWrapLow(step2[1] + step2[14], bd);
            step1[2] = HighbdWrapLow(step2[2] + step2[13], bd);
            step1[3] = HighbdWrapLow(step2[3] + step2[12], bd);
            step1[4] = HighbdWrapLow(step2[4] + step2[11], bd);
            step1[5] = HighbdWrapLow(step2[5] + step2[10], bd);
            step1[6] = HighbdWrapLow(step2[6] + step2[9], bd);
            step1[7] = HighbdWrapLow(step2[7] + step2[8], bd);
            step1[8] = HighbdWrapLow(step2[7] - step2[8], bd);
            step1[9] = HighbdWrapLow(step2[6] - step2[9], bd);
            step1[10] = HighbdWrapLow(step2[5] - step2[10], bd);
            step1[11] = HighbdWrapLow(step2[4] - step2[11], bd);
            step1[12] = HighbdWrapLow(step2[3] - step2[12], bd);
            step1[13] = HighbdWrapLow(step2[2] - step2[13], bd);
            step1[14] = HighbdWrapLow(step2[1] - step2[14], bd);
            step1[15] = HighbdWrapLow(step2[0] - step2[15], bd);

            step1[16] = step2[16];
            step1[17] = step2[17];
            step1[18] = step2[18];
            step1[19] = step2[19];
            temp1 = (-step2[20] + step2[27]) * (long)CosPi1664;
            temp2 = (step2[20] + step2[27]) * (long)CosPi1664;
            step1[20] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step1[27] = HighbdWrapLow(DctConstRoundShift(temp2), bd);
            temp1 = (-step2[21] + step2[26]) * (long)CosPi1664;
            temp2 = (step2[21] + step2[26]) * (long)CosPi1664;
            step1[21] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step1[26] = HighbdWrapLow(DctConstRoundShift(temp2), bd);
            temp1 = (-step2[22] + step2[25]) * (long)CosPi1664;
            temp2 = (step2[22] + step2[25]) * (long)CosPi1664;
            step1[22] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step1[25] = HighbdWrapLow(DctConstRoundShift(temp2), bd);
            temp1 = (-step2[23] + step2[24]) * (long)CosPi1664;
            temp2 = (step2[23] + step2[24]) * (long)CosPi1664;
            step1[23] = HighbdWrapLow(DctConstRoundShift(temp1), bd);
            step1[24] = HighbdWrapLow(DctConstRoundShift(temp2), bd);
            step1[28] = step2[28];
            step1[29] = step2[29];
            step1[30] = step2[30];
            step1[31] = step2[31];

            // final stage
            output[0] = HighbdWrapLow(step1[0] + step1[31], bd);
            output[1] = HighbdWrapLow(step1[1] + step1[30], bd);
            output[2] = HighbdWrapLow(step1[2] + step1[29], bd);
            output[3] = HighbdWrapLow(step1[3] + step1[28], bd);
            output[4] = HighbdWrapLow(step1[4] + step1[27], bd);
            output[5] = HighbdWrapLow(step1[5] + step1[26], bd);
            output[6] = HighbdWrapLow(step1[6] + step1[25], bd);
            output[7] = HighbdWrapLow(step1[7] + step1[24], bd);
            output[8] = HighbdWrapLow(step1[8] + step1[23], bd);
            output[9] = HighbdWrapLow(step1[9] + step1[22], bd);
            output[10] = HighbdWrapLow(step1[10] + step1[21], bd);
            output[11] = HighbdWrapLow(step1[11] + step1[20], bd);
            output[12] = HighbdWrapLow(step1[12] + step1[19], bd);
            output[13] = HighbdWrapLow(step1[13] + step1[18], bd);
            output[14] = HighbdWrapLow(step1[14] + step1[17], bd);
            output[15] = HighbdWrapLow(step1[15] + step1[16], bd);
            output[16] = HighbdWrapLow(step1[15] - step1[16], bd);
            output[17] = HighbdWrapLow(step1[14] - step1[17], bd);
            output[18] = HighbdWrapLow(step1[13] - step1[18], bd);
            output[19] = HighbdWrapLow(step1[12] - step1[19], bd);
            output[20] = HighbdWrapLow(step1[11] - step1[20], bd);
            output[21] = HighbdWrapLow(step1[10] - step1[21], bd);
            output[22] = HighbdWrapLow(step1[9] - step1[22], bd);
            output[23] = HighbdWrapLow(step1[8] - step1[23], bd);
            output[24] = HighbdWrapLow(step1[7] - step1[24], bd);
            output[25] = HighbdWrapLow(step1[6] - step1[25], bd);
            output[26] = HighbdWrapLow(step1[5] - step1[26], bd);
            output[27] = HighbdWrapLow(step1[4] - step1[27], bd);
            output[28] = HighbdWrapLow(step1[3] - step1[28], bd);
            output[29] = HighbdWrapLow(step1[2] - step1[29], bd);
            output[30] = HighbdWrapLow(step1[1] - step1[30], bd);
            output[31] = HighbdWrapLow(step1[0] - step1[31], bd);
        }

        [SkipLocalsInit]
        public static void HighbdIdct32X321024Add(ReadOnlySpan<int> input, Span<ushort> dest, int stride, int bd)
        {
            Span<int> output = stackalloc int[32 * 32];
            Span<int> outptr = output;
            Span<int> tempIn = stackalloc int[32];
            Span<int> tempOut = stackalloc int[32];

            // Rows
            for (int i = 0; i < 32; ++i)
            {
                int zeroCoeff = 0;
                for (int j = 0; j < 32; ++j)
                {
                    zeroCoeff |= input[j];
                }

                if (zeroCoeff != 0)
                {
                    HighbdIdct32(input, outptr, bd);
                }
                else
                {
                    outptr.Slice(0, 32).Clear();
                }

                input = input.Slice(32);
                outptr = outptr.Slice(32);
            }

            // Columns
            for (int i = 0; i < 32; ++i)
            {
                for (int j = 0; j < 32; ++j)
                {
                    tempIn[j] = output[(j * 32) + i];
                }

                HighbdIdct32(tempIn, tempOut, bd);
                for (int j = 0; j < 32; ++j)
                {
                    dest[(j * stride) + i] = HighbdClipPixelAdd(dest[(j * stride) + i],
                        BitUtils.RoundPowerOfTwo(tempOut[j], 6), bd);
                }
            }
        }

        [SkipLocalsInit]
        public static void HighbdIdct32X32135Add(ReadOnlySpan<int> input, Span<ushort> dest, int stride, int bd)
        {
            Span<int> output = stackalloc int[32 * 32];
            Span<int> outptr = output;
            Span<int> tempIn = stackalloc int[32];
            Span<int> tempOut = stackalloc int[32];

            output.Clear();

            // Rows
            // Only upper-left 16x16 has non-zero coeff
            for (int i = 0; i < 16; ++i)
            {
                HighbdIdct32(input, outptr, bd);
                input = input.Slice(32);
                outptr = outptr.Slice(32);
            }

            // Columns
            for (int i = 0; i < 32; ++i)
            {
                Span<ushort> destT = dest;
                for (int j = 0; j < 32; ++j)
                {
                    tempIn[j] = output[(j * 32) + i];
                }

                HighbdIdct32(tempIn, tempOut, bd);
                for (int j = 0; j < 32; ++j)
                {
                    destT[i] = HighbdClipPixelAdd(destT[i], BitUtils.RoundPowerOfTwo(tempOut[j], 6), bd);
                    destT = destT.Slice(stride);
                }
            }
        }

        [SkipLocalsInit]
        public static void HighbdIdct32X3234Add(ReadOnlySpan<int> input, Span<ushort> dest, int stride, int bd)
        {
            Span<int> output = stackalloc int[32 * 32];
            Span<int> outptr = output;
            Span<int> tempIn = stackalloc int[32];
            Span<int> tempOut = stackalloc int[32];

            output.Clear();

            // Rows
            // Only upper-left 8x8 has non-zero coeff
            for (int i = 0; i < 8; ++i)
            {
                HighbdIdct32(input, outptr, bd);
                input = input.Slice(32);
                outptr = outptr.Slice(32);
            }

            // Columns
            for (int i = 0; i < 32; ++i)
            {
                for (int j = 0; j < 32; ++j)
                {
                    tempIn[j] = output[(j * 32) + i];
                }

                HighbdIdct32(tempIn, tempOut, bd);
                for (int j = 0; j < 32; ++j)
                {
                    dest[(j * stride) + i] = HighbdClipPixelAdd(dest[(j * stride) + i],
                        BitUtils.RoundPowerOfTwo(tempOut[j], 6), bd);
                }
            }
        }

        public static void HighbdIdct32X321Add(ReadOnlySpan<int> input, Span<ushort> dest, int stride, int bd)
        {
            int a1;
            int output = HighbdWrapLow(DctConstRoundShift(input[0] * (long)CosPi1664), bd);

            output = HighbdWrapLow(DctConstRoundShift(output * (long)CosPi1664), bd);
            a1 = BitUtils.RoundPowerOfTwo(output, 6);

            for (int j = 0; j < 32; ++j)
            {
                for (int i = 0; i < 32; ++i)
                {
                    dest[i] = HighbdClipPixelAdd(dest[i], a1, bd);
                }

                dest = dest.Slice(stride);
            }
        }
    }
}