using Ryujinx.Graphics.Nvdec.Vp9.Common;
using Ryujinx.Graphics.Nvdec.Vp9.Types;
using System;
using static Ryujinx.Graphics.Nvdec.Vp9.Dsp.InvTxfm;

namespace Ryujinx.Graphics.Nvdec.Vp9
{
    internal static class Idct
    {
        private delegate void Transform1D(ReadOnlySpan<int> input, Span<int> output);

        private delegate void HighbdTransform1D(ReadOnlySpan<int> input, Span<int> output, int bd);

        private struct Transform2D
        {
            public readonly Transform1D Cols; // Vertical and horizontal
            public readonly Transform1D Rows; // Vertical and horizontal

            public Transform2D(Transform1D cols, Transform1D rows)
            {
                Cols = cols;
                Rows = rows;
            }
        }

        private struct HighbdTransform2D
        {
            public readonly HighbdTransform1D Cols; // Vertical and horizontal
            public readonly HighbdTransform1D Rows; // Vertical and horizontal

            public HighbdTransform2D(HighbdTransform1D cols, HighbdTransform1D rows)
            {
                Cols = cols;
                Rows = rows;
            }
        }

        private static readonly Transform2D[] Iht4 =
        {
            new(Idct4, Idct4), // DCT_DCT  = 0
            new(Iadst4, Idct4), // ADST_DCT = 1
            new(Idct4, Iadst4), // DCT_ADST = 2
            new(Iadst4, Iadst4) // ADST_ADST = 3
        };

        public static void Iht4x416Add(ReadOnlySpan<int> input, Span<byte> dest, int stride, int txType)
        {
            Span<int> output = stackalloc int[4 * 4];
            Span<int> outptr = output;
            Span<int> tempIn = stackalloc int[4];
            Span<int> tempOut = stackalloc int[4];

            // Inverse transform row vectors
            for (int i = 0; i < 4; ++i)
            {
                Iht4[txType].Rows(input, outptr);
                input = input.Slice(4);
                outptr = outptr.Slice(4);
            }

            // Inverse transform column vectors
            for (int i = 0; i < 4; ++i)
            {
                for (int j = 0; j < 4; ++j)
                {
                    tempIn[j] = output[(j * 4) + i];
                }

                Iht4[txType].Cols(tempIn, tempOut);
                for (int j = 0; j < 4; ++j)
                {
                    dest[(j * stride) + i] =
                        ClipPixelAdd(dest[(j * stride) + i], BitUtils.RoundPowerOfTwo(tempOut[j], 4));
                }
            }
        }

        private static readonly Transform2D[] Iht8 =
        {
            new(Idct8, Idct8), // DCT_DCT  = 0
            new(Iadst8, Idct8), // ADST_DCT = 1
            new(Idct8, Iadst8), // DCT_ADST = 2
            new(Iadst8, Iadst8) // ADST_ADST = 3
        };

        public static void Iht8x864Add(ReadOnlySpan<int> input, Span<byte> dest, int stride, int txType)
        {
            Span<int> output = stackalloc int[8 * 8];
            Span<int> outptr = output;
            Span<int> tempIn = stackalloc int[8];
            Span<int> tempOut = stackalloc int[8];
            Transform2D ht = Iht8[txType];

            // Inverse transform row vectors
            for (int i = 0; i < 8; ++i)
            {
                ht.Rows(input, outptr);
                input = input.Slice(8);
                outptr = outptr.Slice(8);
            }

            // Inverse transform column vectors
            for (int i = 0; i < 8; ++i)
            {
                for (int j = 0; j < 8; ++j)
                {
                    tempIn[j] = output[(j * 8) + i];
                }

                ht.Cols(tempIn, tempOut);
                for (int j = 0; j < 8; ++j)
                {
                    dest[(j * stride) + i] =
                        ClipPixelAdd(dest[(j * stride) + i], BitUtils.RoundPowerOfTwo(tempOut[j], 5));
                }
            }
        }

        private static readonly Transform2D[] Iht16 =
        {
            new(Idct16, Idct16), // DCT_DCT  = 0
            new(Iadst16, Idct16), // ADST_DCT = 1
            new(Idct16, Iadst16), // DCT_ADST = 2
            new(Iadst16, Iadst16) // ADST_ADST = 3
        };

        public static void Iht16x16256Add(ReadOnlySpan<int> input, Span<byte> dest, int stride, int txType)
        {
            Span<int> output = stackalloc int[16 * 16];
            Span<int> outptr = output;
            Span<int> tempIn = stackalloc int[16];
            Span<int> tempOut = stackalloc int[16];
            Transform2D ht = Iht16[txType];

            // Rows
            for (int i = 0; i < 16; ++i)
            {
                ht.Rows(input, outptr);
                input = input.Slice(16);
                outptr = outptr.Slice(16);
            }

            // Columns
            for (int i = 0; i < 16; ++i)
            {
                for (int j = 0; j < 16; ++j)
                {
                    tempIn[j] = output[(j * 16) + i];
                }

                ht.Cols(tempIn, tempOut);
                for (int j = 0; j < 16; ++j)
                {
                    dest[(j * stride) + i] =
                        ClipPixelAdd(dest[(j * stride) + i], BitUtils.RoundPowerOfTwo(tempOut[j], 6));
                }
            }
        }

        // Idct
        public static void Idct4x4Add(ReadOnlySpan<int> input, Span<byte> dest, int stride, int eob)
        {
            if (eob > 1)
            {
                Idct4x416Add(input, dest, stride);
            }
            else
            {
                Idct4x41Add(input, dest, stride);
            }
        }

        public static void Iwht4x4Add(ReadOnlySpan<int> input, Span<byte> dest, int stride, int eob)
        {
            if (eob > 1)
            {
                Iwht4x416Add(input, dest, stride);
            }
            else
            {
                Iwht4x41Add(input, dest, stride);
            }
        }

        public static void Idct8x8Add(ReadOnlySpan<int> input, Span<byte> dest, int stride, int eob)
        {
            // If dc is 1, then input[0] is the reconstructed value, do not need
            // dequantization. Also, when dc is 1, dc is counted in eobs, namely eobs >=1.

            // The calculation can be simplified if there are not many non-zero dct
            // coefficients. Use eobs to decide what to do.
            if (eob == 1)
            {
                // DC only DCT coefficient
                Idct8x81Add(input, dest, stride);
            }
            else if (eob <= 12)
            {
                Idct8x812Add(input, dest, stride);
            }
            else
            {
                Idct8x864Add(input, dest, stride);
            }
        }

        public static void Idct16x16Add(ReadOnlySpan<int> input, Span<byte> dest, int stride, int eob)
        {
            /* The calculation can be simplified if there are not many non-zero dct
             * coefficients. Use eobs to separate different cases. */
            if (eob == 1) /* DC only DCT coefficient. */
            {
                Idct16x161Add(input, dest, stride);
            }
            else if (eob <= 10)
            {
                Idct16x1610Add(input, dest, stride);
            }
            else if (eob <= 38)
            {
                Idct16x1638Add(input, dest, stride);
            }
            else
            {
                Idct16x16256Add(input, dest, stride);
            }
        }

        public static void Idct32x32Add(ReadOnlySpan<int> input, Span<byte> dest, int stride, int eob)
        {
            if (eob == 1)
            {
                Idct32x321Add(input, dest, stride);
            }
            else if (eob <= 34)
            {
                // Non-zero coeff only in upper-left 8x8
                Idct32x3234Add(input, dest, stride);
            }
            else if (eob <= 135)
            {
                // Non-zero coeff only in upper-left 16x16
                Idct32x32135Add(input, dest, stride);
            }
            else
            {
                Idct32x321024Add(input, dest, stride);
            }
        }

        // Iht
        public static void Iht4x4Add(TxType txType, ReadOnlySpan<int> input, Span<byte> dest, int stride, int eob)
        {
            if (txType == TxType.DctDct)
            {
                Idct4x4Add(input, dest, stride, eob);
            }
            else
            {
                Iht4x416Add(input, dest, stride, (int)txType);
            }
        }

        public static void Iht8x8Add(TxType txType, ReadOnlySpan<int> input, Span<byte> dest, int stride, int eob)
        {
            if (txType == TxType.DctDct)
            {
                Idct8x8Add(input, dest, stride, eob);
            }
            else
            {
                Iht8x864Add(input, dest, stride, (int)txType);
            }
        }

        public static void Iht16x16Add(TxType txType, ReadOnlySpan<int> input, Span<byte> dest,
            int stride, int eob)
        {
            if (txType == TxType.DctDct)
            {
                Idct16x16Add(input, dest, stride, eob);
            }
            else
            {
                Iht16x16256Add(input, dest, stride, (int)txType);
            }
        }

        private static readonly HighbdTransform2D[] HighbdIht4 =
        {
            new(HighbdIdct4, HighbdIdct4), // DCT_DCT  = 0
            new(HighbdIadst4, HighbdIdct4), // ADST_DCT = 1
            new(HighbdIdct4, HighbdIadst4), // DCT_ADST = 2
            new(HighbdIadst4, HighbdIadst4) // ADST_ADST = 3
        };

        public static void HighbdIht4x416Add(ReadOnlySpan<int> input, Span<ushort> dest, int stride, int txType, int bd)
        {
            Span<int> output = stackalloc int[4 * 4];
            Span<int> outptr = output;
            Span<int> tempIn = stackalloc int[4];
            Span<int> tempOut = stackalloc int[4];

            // Inverse transform row vectors.
            for (int i = 0; i < 4; ++i)
            {
                HighbdIht4[txType].Rows(input, outptr, bd);
                input = input.Slice(4);
                outptr = outptr.Slice(4);
            }

            // Inverse transform column vectors.
            for (int i = 0; i < 4; ++i)
            {
                for (int j = 0; j < 4; ++j)
                {
                    tempIn[j] = output[(j * 4) + i];
                }

                HighbdIht4[txType].Cols(tempIn, tempOut, bd);
                for (int j = 0; j < 4; ++j)
                {
                    dest[(j * stride) + i] = HighbdClipPixelAdd(dest[(j * stride) + i],
                        BitUtils.RoundPowerOfTwo(tempOut[j], 4), bd);
                }
            }
        }

        private static readonly HighbdTransform2D[] HighIht8 =
        {
            new(HighbdIdct8, HighbdIdct8), // DCT_DCT  = 0
            new(HighbdIadst8, HighbdIdct8), // ADST_DCT = 1
            new(HighbdIdct8, HighbdIadst8), // DCT_ADST = 2
            new(HighbdIadst8, HighbdIadst8) // ADST_ADST = 3
        };

        public static void HighbdIht8x864Add(ReadOnlySpan<int> input, Span<ushort> dest, int stride, int txType, int bd)
        {
            Span<int> output = stackalloc int[8 * 8];
            Span<int> outptr = output;
            Span<int> tempIn = stackalloc int[8];
            Span<int> tempOut = stackalloc int[8];
            HighbdTransform2D ht = HighIht8[txType];

            // Inverse transform row vectors.
            for (int i = 0; i < 8; ++i)
            {
                ht.Rows(input, outptr, bd);
                input = input.Slice(8);
                outptr = output.Slice(8);
            }

            // Inverse transform column vectors.
            for (int i = 0; i < 8; ++i)
            {
                for (int j = 0; j < 8; ++j)
                {
                    tempIn[j] = output[(j * 8) + i];
                }

                ht.Cols(tempIn, tempOut, bd);
                for (int j = 0; j < 8; ++j)
                {
                    dest[(j * stride) + i] = HighbdClipPixelAdd(dest[(j * stride) + i],
                        BitUtils.RoundPowerOfTwo(tempOut[j], 5), bd);
                }
            }
        }

        private static readonly HighbdTransform2D[] HighIht16 =
        {
            new(HighbdIdct16, HighbdIdct16), // DCT_DCT  = 0
            new(HighbdIadst16, HighbdIdct16), // ADST_DCT = 1
            new(HighbdIdct16, HighbdIadst16), // DCT_ADST = 2
            new(HighbdIadst16, HighbdIadst16) // ADST_ADST = 3
        };

        public static void HighbdIht16x16256Add(ReadOnlySpan<int> input, Span<ushort> dest, int stride, int txType,
            int bd)
        {
            Span<int> output = stackalloc int[16 * 16];
            Span<int> outptr = output;
            Span<int> tempIn = stackalloc int[16];
            Span<int> tempOut = stackalloc int[16];
            HighbdTransform2D ht = HighIht16[txType];

            // Rows
            for (int i = 0; i < 16; ++i)
            {
                ht.Rows(input, outptr, bd);
                input = input.Slice(16);
                outptr = output.Slice(16);
            }

            // Columns
            for (int i = 0; i < 16; ++i)
            {
                for (int j = 0; j < 16; ++j)
                {
                    tempIn[j] = output[(j * 16) + i];
                }

                ht.Cols(tempIn, tempOut, bd);
                for (int j = 0; j < 16; ++j)
                {
                    dest[(j * stride) + i] = HighbdClipPixelAdd(dest[(j * stride) + i],
                        BitUtils.RoundPowerOfTwo(tempOut[j], 6), bd);
                }
            }
        }

        // Idct
        public static void HighbdIdct4x4Add(ReadOnlySpan<int> input, Span<ushort> dest, int stride, int eob, int bd)
        {
            if (eob > 1)
            {
                HighbdIdct4x416Add(input, dest, stride, bd);
            }
            else
            {
                HighbdIdct4x41Add(input, dest, stride, bd);
            }
        }

        public static void HighbdIwht4x4Add(ReadOnlySpan<int> input, Span<ushort> dest, int stride, int eob, int bd)
        {
            if (eob > 1)
            {
                HighbdIwht4x416Add(input, dest, stride, bd);
            }
            else
            {
                HighbdIwht4x41Add(input, dest, stride, bd);
            }
        }

        public static void HighbdIdct8x8Add(ReadOnlySpan<int> input, Span<ushort> dest, int stride, int eob, int bd)
        {
            // If dc is 1, then input[0] is the reconstructed value, do not need
            // dequantization. Also, when dc is 1, dc is counted in eobs, namely eobs >=1.

            // The calculation can be simplified if there are not many non-zero dct
            // coefficients. Use eobs to decide what to do.
            // DC only DCT coefficient
            if (eob == 1)
            {
                VpxHighbdidct8x81AddC(input, dest, stride, bd);
            }
            else if (eob <= 12)
            {
                HighbdIdct8x812Add(input, dest, stride, bd);
            }
            else
            {
                HighbdIdct8x864Add(input, dest, stride, bd);
            }
        }

        public static void HighbdIdct16x16Add(ReadOnlySpan<int> input, Span<ushort> dest, int stride, int eob, int bd)
        {
            // The calculation can be simplified if there are not many non-zero dct
            // coefficients. Use eobs to separate different cases.
            // DC only DCT coefficient.
            if (eob == 1)
            {
                HighbdIdct16x161Add(input, dest, stride, bd);
            }
            else if (eob <= 10)
            {
                HighbdIdct16x1610Add(input, dest, stride, bd);
            }
            else if (eob <= 38)
            {
                HighbdIdct16x1638Add(input, dest, stride, bd);
            }
            else
            {
                HighbdIdct16x16256Add(input, dest, stride, bd);
            }
        }

        public static void HighbdIdct32x32Add(ReadOnlySpan<int> input, Span<ushort> dest, int stride, int eob, int bd)
        {
            // Non-zero coeff only in upper-left 8x8
            if (eob == 1)
            {
                HighbdIdct32x321Add(input, dest, stride, bd);
            }
            else if (eob <= 34)
            {
                HighbdIdct32x3234Add(input, dest, stride, bd);
            }
            else if (eob <= 135)
            {
                HighbdIdct32x32135Add(input, dest, stride, bd);
            }
            else
            {
                HighbdIdct32x321024Add(input, dest, stride, bd);
            }
        }

        // Iht
        public static void HighbdIht4x4Add(TxType txType, ReadOnlySpan<int> input, Span<ushort> dest, int stride,
            int eob, int bd)
        {
            if (txType == TxType.DctDct)
            {
                HighbdIdct4x4Add(input, dest, stride, eob, bd);
            }
            else
            {
                HighbdIht4x416Add(input, dest, stride, (int)txType, bd);
            }
        }

        public static void HighbdIht8x8Add(TxType txType, ReadOnlySpan<int> input, Span<ushort> dest, int stride,
            int eob, int bd)
        {
            if (txType == TxType.DctDct)
            {
                HighbdIdct8x8Add(input, dest, stride, eob, bd);
            }
            else
            {
                HighbdIht8x864Add(input, dest, stride, (int)txType, bd);
            }
        }

        public static void HighbdIht16x16Add(TxType txType, ReadOnlySpan<int> input, Span<ushort> dest, int stride,
            int eob, int bd)
        {
            if (txType == TxType.DctDct)
            {
                HighbdIdct16x16Add(input, dest, stride, eob, bd);
            }
            else
            {
                HighbdIht16x16256Add(input, dest, stride, (int)txType, bd);
            }
        }
    }
}