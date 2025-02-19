using Ryujinx.Graphics.Nvdec.Vp9.Common;
using Ryujinx.Graphics.Nvdec.Vp9.Types;
using System;
using static Ryujinx.Graphics.Nvdec.Vp9.Dsp.IntraPred;

namespace Ryujinx.Graphics.Nvdec.Vp9
{
    internal static class ReconIntra
    {
        public static readonly TxType[] IntraModeToTxTypeLookup =
        [
            TxType.DctDct, // DC
            TxType.AdstDct, // V
            TxType.DctAdst, // H
            TxType.DctDct, // D45
            TxType.AdstAdst, // D135
            TxType.AdstDct, // D117
            TxType.DctAdst, // D153
            TxType.DctAdst, // D207
            TxType.AdstDct, // D63
            TxType.AdstAdst // TM
        ];

        private const int NeedLeft = 1 << 1;
        private const int NeedAbove = 1 << 2;
        private const int NeedAboveRight = 1 << 3;

        private static ReadOnlySpan<byte> ExtendModes =>
        [
            NeedAbove | NeedLeft, // DC
            NeedAbove, // V
            NeedLeft, // H
            NeedAboveRight, // D45
            NeedLeft | NeedAbove, // D135
            NeedLeft | NeedAbove, // D117
            NeedLeft | NeedAbove, // D153
            NeedLeft, // D207
            NeedAboveRight, // D63
            NeedLeft | NeedAbove // TM
        ];

        private unsafe delegate void IntraPredFn(byte* dst, int stride, byte* above, byte* left);

        private static readonly unsafe IntraPredFn[][] _pred =
        [
            [null, null, null, null],
            [VPredictor4X4, VPredictor8X8, VPredictor16X16, VPredictor32X32],
            [HPredictor4X4, HPredictor8X8, HPredictor16X16, HPredictor32X32],
            [D45Predictor4X4, D45Predictor8X8, D45Predictor16X16, D45Predictor32X32],
            [D135Predictor4X4, D135Predictor8X8, D135Predictor16X16, D135Predictor32X32],
            [D117Predictor4X4, D117Predictor8X8, D117Predictor16X16, D117Predictor32X32],
            [D153Predictor4X4, D153Predictor8X8, D153Predictor16X16, D153Predictor32X32],
            [D207Predictor4X4, D207Predictor8X8, D207Predictor16X16, D207Predictor32X32],
            [D63Predictor4X4, D63Predictor8X8, D63Predictor16X16, D63Predictor32X32],
            [TmPredictor4X4, TmPredictor8X8, TmPredictor16X16, TmPredictor32X32]
        ];

        private static readonly unsafe IntraPredFn[][][] _dcPred =
        [
            [
                [
                    Dc128Predictor4X4, Dc128Predictor8X8, Dc128Predictor16X16, Dc128Predictor32X32
                ],
                [
                    DcTopPredictor4X4, DcTopPredictor8X8, DcTopPredictor16X16, DcTopPredictor32X32
                ]
            ],
            [
                [
                    DcLeftPredictor4X4, DcLeftPredictor8X8, DcLeftPredictor16X16, DcLeftPredictor32X32
                ],
                [DcPredictor4X4, DcPredictor8X8, DcPredictor16X16, DcPredictor32X32]
            ]
        ];

        private unsafe delegate void IntraHighPredFn(ushort* dst, int stride, ushort* above, ushort* left, int bd);

        private static readonly unsafe IntraHighPredFn[][] _predHigh =
        [
            [null, null, null, null],
            [
                HighbdVPredictor4X4, HighbdVPredictor8X8, HighbdVPredictor16X16, HighbdVPredictor32X32
            ],
            [
                HighbdHPredictor4X4, HighbdHPredictor8X8, HighbdHPredictor16X16, HighbdHPredictor32X32
            ],
            [
                HighbdD45Predictor4X4, HighbdD45Predictor8X8, HighbdD45Predictor16X16, HighbdD45Predictor32X32
            ],
            [
                HighbdD135Predictor4X4, HighbdD135Predictor8X8, HighbdD135Predictor16X16,
                HighbdD135Predictor32X32
            ],
            [
                HighbdD117Predictor4X4, HighbdD117Predictor8X8, HighbdD117Predictor16X16,
                HighbdD117Predictor32X32
            ],
            [
                HighbdD153Predictor4X4, HighbdD153Predictor8X8, HighbdD153Predictor16X16,
                HighbdD153Predictor32X32
            ],
            [
                HighbdD207Predictor4X4, HighbdD207Predictor8X8, HighbdD207Predictor16X16,
                HighbdD207Predictor32X32
            ],
            [
                HighbdD63Predictor4X4, HighbdD63Predictor8X8, HighbdD63Predictor16X16, HighbdD63Predictor32X32
            ],
            [
                HighbdTmPredictor4X4, HighbdTmPredictor8X8, HighbdTmPredictor16X16, HighbdTmPredictor32X32
            ]
        ];

        private static readonly unsafe IntraHighPredFn[][][] _dcPredHigh =
        [
            [
                [
                    HighbdDc128Predictor4X4, HighbdDc128Predictor8X8, HighbdDc128Predictor16X16,
                    HighbdDc128Predictor32X32
                ],
                [
                    HighbdDcTopPredictor4X4, HighbdDcTopPredictor8X8, HighbdDcTopPredictor16X16,
                    HighbdDcTopPredictor32X32
                ]
            ],
            [
                [
                    HighbdDcLeftPredictor4X4, HighbdDcLeftPredictor8X8, HighbdDcLeftPredictor16X16,
                    HighbdDcLeftPredictor32X32
                ],
                [
                    HighbdDcPredictor4X4, HighbdDcPredictor8X8, HighbdDcPredictor16X16,
                    HighbdDcPredictor32X32
                ]
            ]
        ];

        private static unsafe void BuildIntraPredictorsHigh(
            ref MacroBlockD xd,
            byte* ref8,
            int refStride,
            byte* dst8,
            int dstStride,
            PredictionMode mode,
            TxSize txSize,
            int upAvailable,
            int leftAvailable,
            int rightAvailable,
            int x,
            int y,
            int plane)
        {
            int i;
            ushort* dst = (ushort*)dst8;
            ushort* refr = (ushort*)ref8;
            ushort* leftCol = stackalloc ushort[32];
            ushort* aboveData = stackalloc ushort[64 + 16];
            ushort* aboveRow = aboveData + 16;
            ushort* constAboveRow = aboveRow;
            int bs = 4 << (int)txSize;
            int frameWidth, frameHeight;
            int x0, y0;
            ref MacroBlockDPlane pd = ref xd.Plane[plane];
            int needLeft = ExtendModes[(int)mode] & NeedLeft;
            int needAbove = ExtendModes[(int)mode] & NeedAbove;
            int needAboveRight = ExtendModes[(int)mode] & NeedAboveRight;
            int baseVal = 128 << (xd.Bd - 8);
            // 127 127 127 .. 127 127 127 127 127 127
            // 129  A   B  ..  Y   Z
            // 129  C   D  ..  W   X
            // 129  E   F  ..  U   V
            // 129  G   H  ..  S   T   T   T   T   T
            // For 10 bit and 12 bit, 127 and 129 are replaced by base -1 and base + 1.

            // Get current frame pointer, width and height.
            if (plane == 0)
            {
                frameWidth = xd.CurBuf.Width;
                frameHeight = xd.CurBuf.Height;
            }
            else
            {
                frameWidth = xd.CurBuf.UvWidth;
                frameHeight = xd.CurBuf.UvHeight;
            }

            // Get block position in current frame.
            x0 = (-xd.MbToLeftEdge >> (3 + pd.SubsamplingX)) + x;
            y0 = (-xd.MbToTopEdge >> (3 + pd.SubsamplingY)) + y;

            // NEED_LEFT
            if (needLeft != 0)
            {
                if (leftAvailable != 0)
                {
                    if (xd.MbToBottomEdge < 0)
                    {
                        /* slower path if the block needs border extension */
                        if (y0 + bs <= frameHeight)
                        {
                            for (i = 0; i < bs; ++i)
                            {
                                leftCol[i] = refr[(i * refStride) - 1];
                            }
                        }
                        else
                        {
                            int extendBottom = frameHeight - y0;
                            for (i = 0; i < extendBottom; ++i)
                            {
                                leftCol[i] = refr[(i * refStride) - 1];
                            }

                            for (; i < bs; ++i)
                            {
                                leftCol[i] = refr[((extendBottom - 1) * refStride) - 1];
                            }
                        }
                    }
                    else
                    {
                        /* faster path if the block does not need extension */
                        for (i = 0; i < bs; ++i)
                        {
                            leftCol[i] = refr[(i * refStride) - 1];
                        }
                    }
                }
                else
                {
                    MemoryUtil.Fill(leftCol, (ushort)(baseVal + 1), bs);
                }
            }

            // NEED_ABOVE
            if (needAbove != 0)
            {
                if (upAvailable != 0)
                {
                    ushort* aboveRef = refr - refStride;
                    if (xd.MbToRightEdge < 0)
                    {
                        /* slower path if the block needs border extension */
                        if (x0 + bs <= frameWidth)
                        {
                            MemoryUtil.Copy(aboveRow, aboveRef, bs);
                        }
                        else if (x0 <= frameWidth)
                        {
                            int r = frameWidth - x0;
                            MemoryUtil.Copy(aboveRow, aboveRef, r);
                            MemoryUtil.Fill(aboveRow + r, aboveRow[r - 1], x0 + bs - frameWidth);
                        }
                    }
                    else
                    {
                        /* faster path if the block does not need extension */
                        if (bs == 4 && rightAvailable != 0 && leftAvailable != 0)
                        {
                            constAboveRow = aboveRef;
                        }
                        else
                        {
                            MemoryUtil.Copy(aboveRow, aboveRef, bs);
                        }
                    }

                    aboveRow[-1] = leftAvailable != 0 ? aboveRef[-1] : (ushort)(baseVal + 1);
                }
                else
                {
                    MemoryUtil.Fill(aboveRow, (ushort)(baseVal - 1), bs);
                    aboveRow[-1] = (ushort)(baseVal - 1);
                }
            }

            // NEED_ABOVERIGHT
            if (needAboveRight != 0)
            {
                if (upAvailable != 0)
                {
                    ushort* aboveRef = refr - refStride;
                    if (xd.MbToRightEdge < 0)
                    {
                        /* slower path if the block needs border extension */
                        if (x0 + (2 * bs) <= frameWidth)
                        {
                            if (rightAvailable != 0 && bs == 4)
                            {
                                MemoryUtil.Copy(aboveRow, aboveRef, 2 * bs);
                            }
                            else
                            {
                                MemoryUtil.Copy(aboveRow, aboveRef, bs);
                                MemoryUtil.Fill(aboveRow + bs, aboveRow[bs - 1], bs);
                            }
                        }
                        else if (x0 + bs <= frameWidth)
                        {
                            int r = frameWidth - x0;
                            if (rightAvailable != 0 && bs == 4)
                            {
                                MemoryUtil.Copy(aboveRow, aboveRef, r);
                                MemoryUtil.Fill(aboveRow + r, aboveRow[r - 1], x0 + (2 * bs) - frameWidth);
                            }
                            else
                            {
                                MemoryUtil.Copy(aboveRow, aboveRef, bs);
                                MemoryUtil.Fill(aboveRow + bs, aboveRow[bs - 1], bs);
                            }
                        }
                        else if (x0 <= frameWidth)
                        {
                            int r = frameWidth - x0;
                            MemoryUtil.Copy(aboveRow, aboveRef, r);
                            MemoryUtil.Fill(aboveRow + r, aboveRow[r - 1], x0 + (2 * bs) - frameWidth);
                        }

                        aboveRow[-1] = leftAvailable != 0 ? aboveRef[-1] : (ushort)(baseVal + 1);
                    }
                    else
                    {
                        /* faster path if the block does not need extension */
                        if (bs == 4 && rightAvailable != 0 && leftAvailable != 0)
                        {
                            constAboveRow = aboveRef;
                        }
                        else
                        {
                            MemoryUtil.Copy(aboveRow, aboveRef, bs);
                            if (bs == 4 && rightAvailable != 0)
                            {
                                MemoryUtil.Copy(aboveRow + bs, aboveRef + bs, bs);
                            }
                            else
                            {
                                MemoryUtil.Fill(aboveRow + bs, aboveRow[bs - 1], bs);
                            }

                            aboveRow[-1] = leftAvailable != 0 ? aboveRef[-1] : (ushort)(baseVal + 1);
                        }
                    }
                }
                else
                {
                    MemoryUtil.Fill(aboveRow, (ushort)(baseVal - 1), bs * 2);
                    aboveRow[-1] = (ushort)(baseVal - 1);
                }
            }

            // Predict
            if (mode == PredictionMode.DcPred)
            {
                _dcPredHigh[leftAvailable][upAvailable][(int)txSize](dst, dstStride, constAboveRow, leftCol, xd.Bd);
            }
            else
            {
                _predHigh[(int)mode][(int)txSize](dst, dstStride, constAboveRow, leftCol, xd.Bd);
            }
        }

        public static unsafe void BuildIntraPredictors(
            ref MacroBlockD xd,
            byte* refr,
            int refStride,
            byte* dst,
            int dstStride,
            PredictionMode mode,
            TxSize txSize,
            int upAvailable,
            int leftAvailable,
            int rightAvailable,
            int x,
            int y,
            int plane)
        {
            int i;
            byte* leftCol = stackalloc byte[32];
            byte* aboveData = stackalloc byte[64 + 16];
            byte* aboveRow = aboveData + 16;
            byte* constAboveRow = aboveRow;
            int bs = 4 << (int)txSize;
            int frameWidth, frameHeight;
            int x0, y0;
            ref MacroBlockDPlane pd = ref xd.Plane[plane];

            // 127 127 127 .. 127 127 127 127 127 127
            // 129  A   B  ..  Y   Z
            // 129  C   D  ..  W   X
            // 129  E   F  ..  U   V
            // 129  G   H  ..  S   T   T   T   T   T
            // ..

            // Get current frame pointer, width and height.
            if (plane == 0)
            {
                frameWidth = xd.CurBuf.Width;
                frameHeight = xd.CurBuf.Height;
            }
            else
            {
                frameWidth = xd.CurBuf.UvWidth;
                frameHeight = xd.CurBuf.UvHeight;
            }

            // Get block position in current frame.
            x0 = (-xd.MbToLeftEdge >> (3 + pd.SubsamplingX)) + x;
            y0 = (-xd.MbToTopEdge >> (3 + pd.SubsamplingY)) + y;

            // NEED_LEFT
            if ((ExtendModes[(int)mode] & NeedLeft) != 0)
            {
                if (leftAvailable != 0)
                {
                    if (xd.MbToBottomEdge < 0)
                    {
                        /* Slower path if the block needs border extension */
                        if (y0 + bs <= frameHeight)
                        {
                            for (i = 0; i < bs; ++i)
                            {
                                leftCol[i] = refr[(i * refStride) - 1];
                            }
                        }
                        else
                        {
                            int extendBottom = frameHeight - y0;
                            for (i = 0; i < extendBottom; ++i)
                            {
                                leftCol[i] = refr[(i * refStride) - 1];
                            }

                            for (; i < bs; ++i)
                            {
                                leftCol[i] = refr[((extendBottom - 1) * refStride) - 1];
                            }
                        }
                    }
                    else
                    {
                        /* Faster path if the block does not need extension */
                        for (i = 0; i < bs; ++i)
                        {
                            leftCol[i] = refr[(i * refStride) - 1];
                        }
                    }
                }
                else
                {
                    MemoryUtil.Fill(leftCol, (byte)129, bs);
                }
            }

            // NEED_ABOVE
            if ((ExtendModes[(int)mode] & NeedAbove) != 0)
            {
                if (upAvailable != 0)
                {
                    byte* aboveRef = refr - refStride;
                    if (xd.MbToRightEdge < 0)
                    {
                        /* Slower path if the block needs border extension */
                        if (x0 + bs <= frameWidth)
                        {
                            MemoryUtil.Copy(aboveRow, aboveRef, bs);
                        }
                        else if (x0 <= frameWidth)
                        {
                            int r = frameWidth - x0;
                            MemoryUtil.Copy(aboveRow, aboveRef, r);
                            MemoryUtil.Fill(aboveRow + r, aboveRow[r - 1], x0 + bs - frameWidth);
                        }
                    }
                    else
                    {
                        /* Faster path if the block does not need extension */
                        if (bs == 4 && rightAvailable != 0 && leftAvailable != 0)
                        {
                            constAboveRow = aboveRef;
                        }
                        else
                        {
                            MemoryUtil.Copy(aboveRow, aboveRef, bs);
                        }
                    }

                    aboveRow[-1] = leftAvailable != 0 ? aboveRef[-1] : (byte)129;
                }
                else
                {
                    MemoryUtil.Fill(aboveRow, (byte)127, bs);
                    aboveRow[-1] = 127;
                }
            }

            // NEED_ABOVERIGHT
            if ((ExtendModes[(int)mode] & NeedAboveRight) != 0)
            {
                if (upAvailable != 0)
                {
                    byte* aboveRef = refr - refStride;
                    if (xd.MbToRightEdge < 0)
                    {
                        /* Slower path if the block needs border extension */
                        if (x0 + (2 * bs) <= frameWidth)
                        {
                            if (rightAvailable != 0 && bs == 4)
                            {
                                MemoryUtil.Copy(aboveRow, aboveRef, 2 * bs);
                            }
                            else
                            {
                                MemoryUtil.Copy(aboveRow, aboveRef, bs);
                                MemoryUtil.Fill(aboveRow + bs, aboveRow[bs - 1], bs);
                            }
                        }
                        else if (x0 + bs <= frameWidth)
                        {
                            int r = frameWidth - x0;
                            if (rightAvailable != 0 && bs == 4)
                            {
                                MemoryUtil.Copy(aboveRow, aboveRef, r);
                                MemoryUtil.Fill(aboveRow + r, aboveRow[r - 1], x0 + (2 * bs) - frameWidth);
                            }
                            else
                            {
                                MemoryUtil.Copy(aboveRow, aboveRef, bs);
                                MemoryUtil.Fill(aboveRow + bs, aboveRow[bs - 1], bs);
                            }
                        }
                        else if (x0 <= frameWidth)
                        {
                            int r = frameWidth - x0;
                            MemoryUtil.Copy(aboveRow, aboveRef, r);
                            MemoryUtil.Fill(aboveRow + r, aboveRow[r - 1], x0 + (2 * bs) - frameWidth);
                        }
                    }
                    else
                    {
                        /* Faster path if the block does not need extension */
                        if (bs == 4 && rightAvailable != 0 && leftAvailable != 0)
                        {
                            constAboveRow = aboveRef;
                        }
                        else
                        {
                            MemoryUtil.Copy(aboveRow, aboveRef, bs);
                            if (bs == 4 && rightAvailable != 0)
                            {
                                MemoryUtil.Copy(aboveRow + bs, aboveRef + bs, bs);
                            }
                            else
                            {
                                MemoryUtil.Fill(aboveRow + bs, aboveRow[bs - 1], bs);
                            }
                        }
                    }

                    aboveRow[-1] = leftAvailable != 0 ? aboveRef[-1] : (byte)129;
                }
                else
                {
                    MemoryUtil.Fill(aboveRow, (byte)127, bs * 2);
                    aboveRow[-1] = 127;
                }
            }

            // Predict
            if (mode == PredictionMode.DcPred)
            {
                _dcPred[leftAvailable][upAvailable][(int)txSize](dst, dstStride, constAboveRow, leftCol);
            }
            else
            {
                _pred[(int)mode][(int)txSize](dst, dstStride, constAboveRow, leftCol);
            }
        }

        public static unsafe void PredictIntraBlock(
            ref MacroBlockD xd,
            int bwlIn,
            TxSize txSize,
            PredictionMode mode,
            byte* refr,
            int refStride,
            byte* dst,
            int dstStride,
            int aoff,
            int loff,
            int plane)
        {
            int bw = 1 << bwlIn;
            int txw = 1 << (int)txSize;
            int haveTop = loff != 0 || !xd.AboveMi.IsNull ? 1 : 0;
            int haveLeft = aoff != 0 || !xd.LeftMi.IsNull ? 1 : 0;
            int haveRight = aoff + txw < bw ? 1 : 0;
            int x = aoff * 4;
            int y = loff * 4;

            if (xd.CurBuf.HighBd)
            {
                BuildIntraPredictorsHigh(
                    ref xd,
                    refr,
                    refStride,
                    dst,
                    dstStride,
                    mode,
                    txSize,
                    haveTop,
                    haveLeft,
                    haveRight,
                    x,
                    y,
                    plane);
                return;
            }

            BuildIntraPredictors(
                ref xd,
                refr,
                refStride,
                dst,
                dstStride,
                mode,
                txSize,
                haveTop,
                haveLeft,
                haveRight,
                x,
                y,
                plane);
        }
    }
}