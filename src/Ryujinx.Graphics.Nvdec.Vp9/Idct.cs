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

        private static readonly Transform2D[] _iht4 =
        [
            new(Idct4, Idct4), // DCT_DCT  = 0
            new(Iadst4, Idct4), // ADST_DCT = 1
            new(Idct4, Iadst4), // DCT_ADST = 2
            new(Iadst4, Iadst4) // ADST_ADST = 3
        ];

        public static void Iht4X416Add(ReadOnlySpan<int> input, Span<byte> dest, int stride, int txType)
        {
            Span<int> output = stackalloc int[4 * 4];
            Span<int> outptr = output;
            Span<int> tempIn = stackalloc int[4];
            Span<int> tempOut = stackalloc int[4];

            // Inverse transform row vectors
            for (int i = 0; i < 4; ++i)
            {
                _iht4[txType].Rows(input, outptr);
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

                _iht4[txType].Cols(tempIn, tempOut);
                for (int j = 0; j < 4; ++j)
                {
                    dest[(j * stride) + i] =
                        ClipPixelAdd(dest[(j * stride) + i], BitUtils.RoundPowerOfTwo(tempOut[j], 4));
                }
            }
        }

        private static readonly Transform2D[] _iht8 =
        [
            new(Idct8, Idct8), // DCT_DCT  = 0
            new(Iadst8, Idct8), // ADST_DCT = 1
            new(Idct8, Iadst8), // DCT_ADST = 2
            new(Iadst8, Iadst8) // ADST_ADST = 3
        ];

        public static void Iht8X864Add(ReadOnlySpan<int> input, Span<byte> dest, int stride, int txType)
        {
            Span<int> output = stackalloc int[8 * 8];
            Span<int> outptr = output;
            Span<int> tempIn = stackalloc int[8];
            Span<int> tempOut = stackalloc int[8];
            Transform2D ht = _iht8[txType];

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

        private static readonly Transform2D[] _iht16 =
        [
            new(Idct16, Idct16), // DCT_DCT  = 0
            new(Iadst16, Idct16), // ADST_DCT = 1
            new(Idct16, Iadst16), // DCT_ADST = 2
            new(Iadst16, Iadst16) // ADST_ADST = 3
        ];

        public static void Iht16X16256Add(ReadOnlySpan<int> input, Span<byte> dest, int stride, int txType)
        {
            Span<int> output = stackalloc int[16 * 16];
            Span<int> outptr = output;
            Span<int> tempIn = stackalloc int[16];
            Span<int> tempOut = stackalloc int[16];
            Transform2D ht = _iht16[txType];

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
        public static void Idct4X4Add(ReadOnlySpan<int> input, Span<byte> dest, int stride, int eob)
        {
            if (eob > 1)
            {
                Idct4X416Add(input, dest, stride);
            }
            else
            {
                Idct4X41Add(input, dest, stride);
            }
        }

        public static void Iwht4X4Add(ReadOnlySpan<int> input, Span<byte> dest, int stride, int eob)
        {
            if (eob > 1)
            {
                Iwht4X416Add(input, dest, stride);
            }
            else
            {
                Iwht4X41Add(input, dest, stride);
            }
        }

        public static void Idct8X8Add(ReadOnlySpan<int> input, Span<byte> dest, int stride, int eob)
        {
            // If dc is 1, then input[0] is the reconstructed value, do not need
            // dequantization. Also, when dc is 1, dc is counted in eobs, namely eobs >=1.

            // The calculation can be simplified if there are not many non-zero dct
            // coefficients. Use eobs to decide what to do.
            if (eob == 1)
            {
                // DC only DCT coefficient
                Idct8X81Add(input, dest, stride);
            }
            else if (eob <= 12)
            {
                Idct8X812Add(input, dest, stride);
            }
            else
            {
                Idct8X864Add(input, dest, stride);
            }
        }

        public static void Idct16X16Add(ReadOnlySpan<int> input, Span<byte> dest, int stride, int eob)
        {
            /* The calculation can be simplified if there are not many non-zero dct
             * coefficients. Use eobs to separate different cases. */
            if (eob == 1) /* DC only DCT coefficient. */
            {
                Idct16X161Add(input, dest, stride);
            }
            else if (eob <= 10)
            {
                Idct16X1610Add(input, dest, stride);
            }
            else if (eob <= 38)
            {
                Idct16X1638Add(input, dest, stride);
            }
            else
            {
                Idct16X16256Add(input, dest, stride);
            }
        }

        public static void Idct32X32Add(ReadOnlySpan<int> input, Span<byte> dest, int stride, int eob)
        {
            if (eob == 1)
            {
                Idct32X321Add(input, dest, stride);
            }
            else if (eob <= 34)
            {
                // Non-zero coeff only in upper-left 8x8
                Idct32X3234Add(input, dest, stride);
            }
            else if (eob <= 135)
            {
                // Non-zero coeff only in upper-left 16x16
                Idct32X32135Add(input, dest, stride);
            }
            else
            {
                Idct32X321024Add(input, dest, stride);
            }
        }

        // Iht
        public static void Iht4X4Add(TxType txType, ReadOnlySpan<int> input, Span<byte> dest, int stride, int eob)
        {
            if (txType == TxType.DctDct)
            {
                Idct4X4Add(input, dest, stride, eob);
            }
            else
            {
                Iht4X416Add(input, dest, stride, (int)txType);
            }
        }

        public static void Iht8X8Add(TxType txType, ReadOnlySpan<int> input, Span<byte> dest, int stride, int eob)
        {
            if (txType == TxType.DctDct)
            {
                Idct8X8Add(input, dest, stride, eob);
            }
            else
            {
                Iht8X864Add(input, dest, stride, (int)txType);
            }
        }

        public static void Iht16X16Add(TxType txType, ReadOnlySpan<int> input, Span<byte> dest,
            int stride, int eob)
        {
            if (txType == TxType.DctDct)
            {
                Idct16X16Add(input, dest, stride, eob);
            }
            else
            {
                Iht16X16256Add(input, dest, stride, (int)txType);
            }
        }

        private static readonly HighbdTransform2D[] _highbdIht4 =
        [
            new(HighbdIdct4, HighbdIdct4), // DCT_DCT  = 0
            new(HighbdIadst4, HighbdIdct4), // ADST_DCT = 1
            new(HighbdIdct4, HighbdIadst4), // DCT_ADST = 2
            new(HighbdIadst4, HighbdIadst4) // ADST_ADST = 3
        ];

        public static void HighbdIht4X416Add(ReadOnlySpan<int> input, Span<ushort> dest, int stride, int txType, int bd)
        {
            Span<int> output = stackalloc int[4 * 4];
            Span<int> outptr = output;
            Span<int> tempIn = stackalloc int[4];
            Span<int> tempOut = stackalloc int[4];

            // Inverse transform row vectors.
            for (int i = 0; i < 4; ++i)
            {
                _highbdIht4[txType].Rows(input, outptr, bd);
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

                _highbdIht4[txType].Cols(tempIn, tempOut, bd);
                for (int j = 0; j < 4; ++j)
                {
                    dest[(j * stride) + i] = HighbdClipPixelAdd(dest[(j * stride) + i],
                        BitUtils.RoundPowerOfTwo(tempOut[j], 4), bd);
                }
            }
        }

        private static readonly HighbdTransform2D[] _highIht8 =
        [
            new(HighbdIdct8, HighbdIdct8), // DCT_DCT  = 0
            new(HighbdIadst8, HighbdIdct8), // ADST_DCT = 1
            new(HighbdIdct8, HighbdIadst8), // DCT_ADST = 2
            new(HighbdIadst8, HighbdIadst8) // ADST_ADST = 3
        ];

        public static void HighbdIht8X864Add(ReadOnlySpan<int> input, Span<ushort> dest, int stride, int txType, int bd)
        {
            Span<int> output = stackalloc int[8 * 8];
            Span<int> outptr = output;
            Span<int> tempIn = stackalloc int[8];
            Span<int> tempOut = stackalloc int[8];
            HighbdTransform2D ht = _highIht8[txType];

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

        private static readonly HighbdTransform2D[] _highIht16 =
        [
            new(HighbdIdct16, HighbdIdct16), // DCT_DCT  = 0
            new(HighbdIadst16, HighbdIdct16), // ADST_DCT = 1
            new(HighbdIdct16, HighbdIadst16), // DCT_ADST = 2
            new(HighbdIadst16, HighbdIadst16) // ADST_ADST = 3
        ];

        public static void HighbdIht16X16256Add(ReadOnlySpan<int> input, Span<ushort> dest, int stride, int txType,
            int bd)
        {
            Span<int> output = stackalloc int[16 * 16];
            Span<int> outptr = output;
            Span<int> tempIn = stackalloc int[16];
            Span<int> tempOut = stackalloc int[16];
            HighbdTransform2D ht = _highIht16[txType];

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
        public static void HighbdIdct4X4Add(ReadOnlySpan<int> input, Span<ushort> dest, int stride, int eob, int bd)
        {
            if (eob > 1)
            {
                HighbdIdct4X416Add(input, dest, stride, bd);
            }
            else
            {
                HighbdIdct4X41Add(input, dest, stride, bd);
            }
        }

        public static void HighbdIwht4X4Add(ReadOnlySpan<int> input, Span<ushort> dest, int stride, int eob, int bd)
        {
            if (eob > 1)
            {
                HighbdIwht4X416Add(input, dest, stride, bd);
            }
            else
            {
                HighbdIwht4X41Add(input, dest, stride, bd);
            }
        }

        public static void HighbdIdct8X8Add(ReadOnlySpan<int> input, Span<ushort> dest, int stride, int eob, int bd)
        {
            // If dc is 1, then input[0] is the reconstructed value, do not need
            // dequantization. Also, when dc is 1, dc is counted in eobs, namely eobs >=1.

            // The calculation can be simplified if there are not many non-zero dct
            // coefficients. Use eobs to decide what to do.
            // DC only DCT coefficient
            if (eob == 1)
            {
                VpxHighbdidct8X81AddC(input, dest, stride, bd);
            }
            else if (eob <= 12)
            {
                HighbdIdct8X812Add(input, dest, stride, bd);
            }
            else
            {
                HighbdIdct8X864Add(input, dest, stride, bd);
            }
        }

        public static void HighbdIdct16X16Add(ReadOnlySpan<int> input, Span<ushort> dest, int stride, int eob, int bd)
        {
            // The calculation can be simplified if there are not many non-zero dct
            // coefficients. Use eobs to separate different cases.
            // DC only DCT coefficient.
            if (eob == 1)
            {
                HighbdIdct16X161Add(input, dest, stride, bd);
            }
            else if (eob <= 10)
            {
                HighbdIdct16X1610Add(input, dest, stride, bd);
            }
            else if (eob <= 38)
            {
                HighbdIdct16X1638Add(input, dest, stride, bd);
            }
            else
            {
                HighbdIdct16X16256Add(input, dest, stride, bd);
            }
        }

        public static void HighbdIdct32X32Add(ReadOnlySpan<int> input, Span<ushort> dest, int stride, int eob, int bd)
        {
            // Non-zero coeff only in upper-left 8x8
            if (eob == 1)
            {
                HighbdIdct32X321Add(input, dest, stride, bd);
            }
            else if (eob <= 34)
            {
                HighbdIdct32X3234Add(input, dest, stride, bd);
            }
            else if (eob <= 135)
            {
                HighbdIdct32X32135Add(input, dest, stride, bd);
            }
            else
            {
                HighbdIdct32X321024Add(input, dest, stride, bd);
            }
        }

        // Iht
        public static void HighbdIht4X4Add(TxType txType, ReadOnlySpan<int> input, Span<ushort> dest, int stride,
            int eob, int bd)
        {
            if (txType == TxType.DctDct)
            {
                HighbdIdct4X4Add(input, dest, stride, eob, bd);
            }
            else
            {
                HighbdIht4X416Add(input, dest, stride, (int)txType, bd);
            }
        }

        public static void HighbdIht8X8Add(TxType txType, ReadOnlySpan<int> input, Span<ushort> dest, int stride,
            int eob, int bd)
        {
            if (txType == TxType.DctDct)
            {
                HighbdIdct8X8Add(input, dest, stride, eob, bd);
            }
            else
            {
                HighbdIht8X864Add(input, dest, stride, (int)txType, bd);
            }
        }

        public static void HighbdIht16X16Add(TxType txType, ReadOnlySpan<int> input, Span<ushort> dest, int stride,
            int eob, int bd)
        {
            if (txType == TxType.DctDct)
            {
                HighbdIdct16X16Add(input, dest, stride, eob, bd);
            }
            else
            {
                HighbdIht16X16256Add(input, dest, stride, (int)txType, bd);
            }
        }
    }
}