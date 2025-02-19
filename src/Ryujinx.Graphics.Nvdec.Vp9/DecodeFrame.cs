using Ryujinx.Common.Memory;
using Ryujinx.Graphics.Nvdec.Vp9.Common;
using Ryujinx.Graphics.Nvdec.Vp9.Dsp;
using Ryujinx.Graphics.Nvdec.Vp9.Types;
using Ryujinx.Graphics.Video;
using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Ryujinx.Graphics.Nvdec.Vp9
{
    internal static class DecodeFrame
    {
        private static bool ReadIsValid(ArrayPtr<byte> start, int len)
        {
            return len != 0 && len <= start.Length;
        }

        private static void ReadTxModeProbs(ref Vp9EntropyProbs txProbs, ref Reader r)
        {
            for (int i = 0; i < EntropyMode.TxSizeContexts; ++i)
            {
                for (int j = 0; j < (int)TxSize.TxSizes - 3; ++j)
                {
                    r.DiffUpdateProb(ref txProbs.Tx8x8Prob[i][j]);
                }
            }

            for (int i = 0; i < EntropyMode.TxSizeContexts; ++i)
            {
                for (int j = 0; j < (int)TxSize.TxSizes - 2; ++j)
                {
                    r.DiffUpdateProb(ref txProbs.Tx16x16Prob[i][j]);
                }
            }

            for (int i = 0; i < EntropyMode.TxSizeContexts; ++i)
            {
                for (int j = 0; j < (int)TxSize.TxSizes - 1; ++j)
                {
                    r.DiffUpdateProb(ref txProbs.Tx32x32Prob[i][j]);
                }
            }
        }

        private static void ReadSwitchableInterpProbs(ref Vp9EntropyProbs fc, ref Reader r)
        {
            for (int j = 0; j < Constants.SwitchableFilterContexts; ++j)
            {
                for (int i = 0; i < Constants.SwitchableFilters - 1; ++i)
                {
                    r.DiffUpdateProb(ref fc.SwitchableInterpProb[j][i]);
                }
            }
        }

        private static void ReadInterModeProbs(ref Vp9EntropyProbs fc, ref Reader r)
        {
            for (int i = 0; i < Constants.InterModeContexts; ++i)
            {
                for (int j = 0; j < Constants.InterModes - 1; ++j)
                {
                    r.DiffUpdateProb( ref fc.InterModeProb[i][j]);
                }
            }
        }

        private static void ReadMvProbs(ref Vp9EntropyProbs ctx, bool allowHp, ref Reader r)
        {
            r.UpdateMvProbs(ctx.Joints.AsSpan(), EntropyMv.Joints - 1);

            for (int i = 0; i < 2; ++i)
            {
                r.UpdateMvProbs(MemoryMarshal.CreateSpan(ref ctx.Sign[i], 1), 1);
                r.UpdateMvProbs(ctx.Classes[i].AsSpan(), EntropyMv.Classes - 1);
                r.UpdateMvProbs(ctx.Class0[i].AsSpan(), EntropyMv.Class0Size - 1);
                r.UpdateMvProbs(ctx.Bits[i].AsSpan(), EntropyMv.OffsetBits);
            }

            for (int i = 0; i < 2; ++i)
            {
                for (int j = 0; j < EntropyMv.Class0Size; ++j)
                {
                    r.UpdateMvProbs(ctx.Class0Fp[i][j].AsSpan(), EntropyMv.FpSize - 1);
                }

                r.UpdateMvProbs(ctx.Fp[i].AsSpan(), 3);
            }

            if (allowHp)
            {
                for (int i = 0; i < 2; ++i)
                {
                    r.UpdateMvProbs(MemoryMarshal.CreateSpan(ref ctx.Class0Hp[i], 1), 1);
                    r.UpdateMvProbs(MemoryMarshal.CreateSpan(ref ctx.Hp[i], 1), 1);
                }
            }
        }

        private static void InverseTransformBlockInter(ref MacroBlockD xd, int plane, TxSize txSize, Span<byte> dst,
            int stride, int eob)
        {
            ref MacroBlockDPlane pd = ref xd.Plane[plane];
            ArrayPtr<int> dqcoeff = pd.DqCoeff;
            Debug.Assert(eob > 0);
            if (xd.CurBuf.HighBd)
            {
                Span<ushort> dst16 = MemoryMarshal.Cast<byte, ushort>(dst);
                if (xd.Lossless)
                {
                    Idct.HighbdIwht4X4Add(dqcoeff.AsSpan(), dst16, stride, eob, xd.Bd);
                }
                else
                {
                    switch (txSize)
                    {
                        case TxSize.Tx4X4:
                            Idct.HighbdIdct4X4Add(dqcoeff.AsSpan(), dst16, stride, eob, xd.Bd);
                            break;
                        case TxSize.Tx8X8:
                            Idct.HighbdIdct8X8Add(dqcoeff.AsSpan(), dst16, stride, eob, xd.Bd);
                            break;
                        case TxSize.Tx16X16:
                            Idct.HighbdIdct16X16Add(dqcoeff.AsSpan(), dst16, stride, eob, xd.Bd);
                            break;
                        case TxSize.Tx32X32:
                            Idct.HighbdIdct32X32Add(dqcoeff.AsSpan(), dst16, stride, eob, xd.Bd);
                            break;
                        default:
                            Debug.Assert(false, "Invalid transform size");
                            break;
                    }
                }
            }
            else
            {
                if (xd.Lossless)
                {
                    Idct.Iwht4X4Add(dqcoeff.AsSpan(), dst, stride, eob);
                }
                else
                {
                    switch (txSize)
                    {
                        case TxSize.Tx4X4:
                            Idct.Idct4X4Add(dqcoeff.AsSpan(), dst, stride, eob);
                            break;
                        case TxSize.Tx8X8:
                            Idct.Idct8X8Add(dqcoeff.AsSpan(), dst, stride, eob);
                            break;
                        case TxSize.Tx16X16:
                            Idct.Idct16X16Add(dqcoeff.AsSpan(), dst, stride, eob);
                            break;
                        case TxSize.Tx32X32:
                            Idct.Idct32X32Add(dqcoeff.AsSpan(), dst, stride, eob);
                            break;
                        default:
                            Debug.Assert(false, "Invalid transform size");
                            return;
                    }
                }
            }

            if (eob == 1)
            {
                dqcoeff.AsSpan()[0] = 0;
            }
            else
            {
                if (txSize <= TxSize.Tx16X16 && eob <= 10)
                {
                    dqcoeff.AsSpan().Slice(0, 4 * (4 << (int)txSize)).Clear();
                }
                else if (txSize == TxSize.Tx32X32 && eob <= 34)
                {
                    dqcoeff.AsSpan().Slice(0, 256).Clear();
                }
                else
                {
                    dqcoeff.AsSpan().Slice(0, 16 << ((int)txSize << 1)).Clear();
                }
            }
        }

        private static void InverseTransformBlockIntra(
            ref MacroBlockD xd,
            int plane,
            TxType txType,
            TxSize txSize,
            Span<byte> dst,
            int stride,
            int eob)
        {
            ref MacroBlockDPlane pd = ref xd.Plane[plane];
            ArrayPtr<int> dqcoeff = pd.DqCoeff;
            Debug.Assert(eob > 0);
            if (xd.CurBuf.HighBd)
            {
                Span<ushort> dst16 = MemoryMarshal.Cast<byte, ushort>(dst);
                if (xd.Lossless)
                {
                    Idct.HighbdIwht4X4Add(dqcoeff.AsSpan(), dst16, stride, eob, xd.Bd);
                }
                else
                {
                    switch (txSize)
                    {
                        case TxSize.Tx4X4:
                            Idct.HighbdIht4X4Add(txType, dqcoeff.AsSpan(), dst16, stride, eob, xd.Bd);
                            break;
                        case TxSize.Tx8X8:
                            Idct.HighbdIht8X8Add(txType, dqcoeff.AsSpan(), dst16, stride, eob, xd.Bd);
                            break;
                        case TxSize.Tx16X16:
                            Idct.HighbdIht16X16Add(txType, dqcoeff.AsSpan(), dst16, stride, eob, xd.Bd);
                            break;
                        case TxSize.Tx32X32:
                            Idct.HighbdIdct32X32Add(dqcoeff.AsSpan(), dst16, stride, eob, xd.Bd);
                            break;
                        default:
                            Debug.Assert(false, "Invalid transform size");
                            break;
                    }
                }
            }
            else
            {
                if (xd.Lossless)
                {
                    Idct.Iwht4X4Add(dqcoeff.AsSpan(), dst, stride, eob);
                }
                else
                {
                    switch (txSize)
                    {
                        case TxSize.Tx4X4:
                            Idct.Iht4X4Add(txType, dqcoeff.AsSpan(), dst, stride, eob);
                            break;
                        case TxSize.Tx8X8:
                            Idct.Iht8X8Add(txType, dqcoeff.AsSpan(), dst, stride, eob);
                            break;
                        case TxSize.Tx16X16:
                            Idct.Iht16X16Add(txType, dqcoeff.AsSpan(), dst, stride, eob);
                            break;
                        case TxSize.Tx32X32:
                            Idct.Idct32X32Add(dqcoeff.AsSpan(), dst, stride, eob);
                            break;
                        default:
                            Debug.Assert(false, "Invalid transform size");
                            return;
                    }
                }
            }

            if (eob == 1)
            {
                dqcoeff.AsSpan()[0] = 0;
            }
            else
            {
                if (txType == TxType.DctDct && txSize <= TxSize.Tx16X16 && eob <= 10)
                {
                    dqcoeff.AsSpan().Slice(0, 4 * (4 << (int)txSize)).Clear();
                }
                else if (txSize == TxSize.Tx32X32 && eob <= 34)
                {
                    dqcoeff.AsSpan().Slice(0, 256).Clear();
                }
                else
                {
                    dqcoeff.AsSpan().Slice(0, 16 << ((int)txSize << 1)).Clear();
                }
            }
        }

        private static unsafe void PredictAndReconstructIntraBlock(
            ref TileWorkerData twd,
            ref ModeInfo mi,
            int plane,
            int row,
            int col,
            TxSize txSize)
        {
            ref MacroBlockD xd = ref twd.Xd;
            ref MacroBlockDPlane pd = ref xd.Plane[plane];
            PredictionMode mode = plane == 0 ? mi.Mode : mi.UvMode;
            int dstOffset = (4 * row * pd.Dst.Stride) + (4 * col);
            byte* dst = &pd.Dst.Buf.ToPointer()[dstOffset];
            Span<byte> dstSpan = pd.Dst.Buf.AsSpan().Slice(dstOffset);

            if (mi.SbType < BlockSize.Block8X8)
            {
                if (plane == 0)
                {
                    mode = xd.Mi[0].Value.Bmi[(row << 1) + col].Mode;
                }
            }

            ReconIntra.PredictIntraBlock(ref xd, pd.N4Wl, txSize, mode, dst, pd.Dst.Stride, dst, pd.Dst.Stride, col,
                row, plane);

            if (mi.Skip == 0)
            {
                TxType txType =
                    plane != 0 || xd.Lossless ? TxType.DctDct : ReconIntra.IntraModeToTxTypeLookup[(int)mode];
                Luts.ScanOrder sc = plane != 0 || xd.Lossless
                    ? Luts.DefaultScanOrders[(int)txSize]
                    : Luts.ScanOrders[(int)txSize][(int)txType];
                int eob = Detokenize.DecodeBlockTokens(ref twd, plane, sc, col, row, txSize, mi.SegmentId);
                if (eob > 0)
                {
                    InverseTransformBlockIntra(ref xd, plane, txType, txSize, dstSpan, pd.Dst.Stride, eob);
                }
            }
        }

        private static int ReconstructInterBlock(
            ref TileWorkerData twd,
            ref ModeInfo mi,
            int plane,
            int row,
            int col,
            TxSize txSize)
        {
            ref MacroBlockD xd = ref twd.Xd;
            ref MacroBlockDPlane pd = ref xd.Plane[plane];
            Luts.ScanOrder sc = Luts.DefaultScanOrders[(int)txSize];
            int eob = Detokenize.DecodeBlockTokens(ref twd, plane, sc, col, row, txSize, mi.SegmentId);
            Span<byte> dst = pd.Dst.Buf.AsSpan().Slice((4 * row * pd.Dst.Stride) + (4 * col));

            if (eob > 0)
            {
                InverseTransformBlockInter(ref xd, plane, txSize, dst, pd.Dst.Stride, eob);
            }

            return eob;
        }

        private static unsafe void BuildMcBorder(
            byte* src,
            int srcStride,
            byte* dst,
            int dstStride,
            int x,
            int y,
            int bW,
            int bH,
            int w,
            int h)
        {
            // Get a pointer to the start of the real data for this row.
            byte* refRow = src - x - (y * srcStride);

            if (y >= h)
            {
                refRow += (h - 1) * srcStride;
            }
            else if (y > 0)
            {
                refRow += y * srcStride;
            }

            do
            {
                int right = 0, copy;
                int left = x < 0 ? -x : 0;

                if (left > bW)
                {
                    left = bW;
                }

                if (x + bW > w)
                {
                    right = x + bW - w;
                }

                if (right > bW)
                {
                    right = bW;
                }

                copy = bW - left - right;

                if (left != 0)
                {
                    MemoryUtil.Fill(dst, refRow[0], left);
                }

                if (copy != 0)
                {
                    MemoryUtil.Copy(dst + left, refRow + x + left, copy);
                }

                if (right != 0)
                {
                    MemoryUtil.Fill(dst + left + copy, refRow[w - 1], right);
                }

                dst += dstStride;
                ++y;

                if (y > 0 && y < h)
                {
                    refRow += srcStride;
                }
            } while (--bH != 0);
        }

        private static unsafe void HighBuildMcBorder(
            byte* src8,
            int srcStride,
            ushort* dst,
            int dstStride,
            int x,
            int y,
            int bW,
            int bH,
            int w,
            int h)
        {
            // Get a pointer to the start of the real data for this row.
            ushort* src = (ushort*)src8;
            ushort* refRow = src - x - (y * srcStride);

            if (y >= h)
            {
                refRow += (h - 1) * srcStride;
            }
            else if (y > 0)
            {
                refRow += y * srcStride;
            }

            do
            {
                int right = 0, copy;
                int left = x < 0 ? -x : 0;

                if (left > bW)
                {
                    left = bW;
                }

                if (x + bW > w)
                {
                    right = x + bW - w;
                }

                if (right > bW)
                {
                    right = bW;
                }

                copy = bW - left - right;

                if (left != 0)
                {
                    MemoryUtil.Fill(dst, refRow[0], left);
                }

                if (copy != 0)
                {
                    MemoryUtil.Copy(dst + left, refRow + x + left, copy);
                }

                if (right != 0)
                {
                    MemoryUtil.Fill(dst + left + copy, refRow[w - 1], right);
                }

                dst += dstStride;
                ++y;

                if (y > 0 && y < h)
                {
                    refRow += srcStride;
                }
            } while (--bH != 0);
        }

        [SkipLocalsInit]
        private static unsafe void ExtendAndPredict(
            byte* bufPtr1,
            int preBufStride,
            int x0,
            int y0,
            int bW,
            int bH,
            int frameWidth,
            int frameHeight,
            int borderOffset,
            byte* dst,
            int dstBufStride,
            int subpelX,
            int subpelY,
            Array8<short>[] kernel,
            ref ScaleFactors sf,
            ref MacroBlockD xd,
            int w,
            int h,
            int refr,
            int xs,
            int ys)
        {
            ushort* mcBufHigh = stackalloc ushort[80 * 2 * 80 * 2];
            if (xd.CurBuf.HighBd)
            {
                HighBuildMcBorder(bufPtr1, preBufStride, mcBufHigh, bW, x0, y0, bW, bH, frameWidth, frameHeight);
                ReconInter.HighbdInterPredictor(
                    mcBufHigh + borderOffset,
                    bW,
                    (ushort*)dst,
                    dstBufStride,
                    subpelX,
                    subpelY,
                    ref sf,
                    w,
                    h,
                    refr,
                    kernel,
                    xs,
                    ys,
                    xd.Bd);
            }
            else
            {
                BuildMcBorder(bufPtr1, preBufStride, (byte*)mcBufHigh, bW, x0, y0, bW, bH, frameWidth, frameHeight);
                ReconInter.InterPredictor(
                    (byte*)mcBufHigh + borderOffset,
                    bW,
                    dst,
                    dstBufStride,
                    subpelX,
                    subpelY,
                    ref sf,
                    w,
                    h,
                    refr,
                    kernel,
                    xs,
                    ys);
            }
        }

        private static unsafe void DecBuildInterPredictors(
            ref MacroBlockD xd,
            int plane,
            int bw,
            int bh,
            int x,
            int y,
            int w,
            int h,
            int miX,
            int miY,
            Array8<short>[] kernel,
            ref ScaleFactors sf,
            ref Buf2D preBuf,
            ref Buf2D dstBuf,
            ref Mv mv,
            ref Surface refFrameBuf,
            bool isScaled,
            int refr)
        {
            ref MacroBlockDPlane pd = ref xd.Plane[plane];
            byte* dst = dstBuf.Buf.ToPointer() + (dstBuf.Stride * y) + x;
            Mv32 scaledMv;
            int xs, ys, x0, y0, x016, y016, frameWidth, frameHeight, bufStride, subpelX, subpelY;
            byte* refFrame;
            byte* bufPtr;

            // Get reference frame pointer, width and height.
            if (plane == 0)
            {
                frameWidth = refFrameBuf.Width;
                frameHeight = refFrameBuf.Height;
                refFrame = refFrameBuf.YBuffer.ToPointer();
            }
            else
            {
                frameWidth = refFrameBuf.UvWidth;
                frameHeight = refFrameBuf.UvHeight;
                refFrame = plane == 1 ? refFrameBuf.UBuffer.ToPointer() : refFrameBuf.VBuffer.ToPointer();
            }

            if (isScaled)
            {
                Mv mvQ4 = ReconInter.ClampMvToUmvBorderSb(ref xd, ref mv, bw, bh, pd.SubsamplingX, pd.SubsamplingY);
                // Co-ordinate of containing block to pixel precision.
                int xStart = -xd.MbToLeftEdge >> (3 + pd.SubsamplingX);
                int yStart = -xd.MbToTopEdge >> (3 + pd.SubsamplingY);
                // Co-ordinate of the block to 1/16th pixel precision.
                x016 = (xStart + x) << Filter.SubpelBits;
                y016 = (yStart + y) << Filter.SubpelBits;

                // Co-ordinate of current block in reference frame
                // to 1/16th pixel precision.
                x016 = sf.ScaleValueX(x016);
                y016 = sf.ScaleValueY(y016);

                // Map the top left corner of the block into the reference frame.
                x0 = sf.ScaleValueX(xStart + x);
                y0 = sf.ScaleValueY(yStart + y);

                // Scale the MV and incorporate the sub-pixel offset of the block
                // in the reference frame.
                scaledMv = sf.ScaleMv(ref mvQ4, miX + x, miY + y);
                xs = sf.XStepQ4;
                ys = sf.YStepQ4;
            }
            else
            {
                // Co-ordinate of containing block to pixel precision.
                x0 = (-xd.MbToLeftEdge >> (3 + pd.SubsamplingX)) + x;
                y0 = (-xd.MbToTopEdge >> (3 + pd.SubsamplingY)) + y;

                // Co-ordinate of the block to 1/16th pixel precision.
                x016 = x0 << Filter.SubpelBits;
                y016 = y0 << Filter.SubpelBits;

                scaledMv.Row = mv.Row * (1 << (1 - pd.SubsamplingY));
                scaledMv.Col = mv.Col * (1 << (1 - pd.SubsamplingX));
                xs = ys = 16;
            }

            subpelX = scaledMv.Col & Filter.SubpelMask;
            subpelY = scaledMv.Row & Filter.SubpelMask;

            // Calculate the top left corner of the best matching block in the
            // reference frame.
            x0 += scaledMv.Col >> Filter.SubpelBits;
            y0 += scaledMv.Row >> Filter.SubpelBits;
            x016 += scaledMv.Col;
            y016 += scaledMv.Row;

            // Get reference block pointer.
            bufPtr = refFrame + (y0 * preBuf.Stride) + x0;
            bufStride = preBuf.Stride;

            // Do border extension if there is motion or the
            // width/height is not a multiple of 8 pixels.
            if (isScaled || scaledMv.Col != 0 || scaledMv.Row != 0 || (frameWidth & 0x7) != 0 ||
                (frameHeight & 0x7) != 0)
            {
                int y1 = ((y016 + ((h - 1) * ys)) >> Filter.SubpelBits) + 1;

                // Get reference block bottom right horizontal coordinate.
                int x1 = ((x016 + ((w - 1) * xs)) >> Filter.SubpelBits) + 1;
                int xPad = 0, yPad = 0;

                if (subpelX != 0 || sf.XStepQ4 != Filter.SubpelShifts)
                {
                    x0 -= Constants.InterpExtend - 1;
                    x1 += Constants.InterpExtend;
                    xPad = 1;
                }

                if (subpelY != 0 || sf.YStepQ4 != Filter.SubpelShifts)
                {
                    y0 -= Constants.InterpExtend - 1;
                    y1 += Constants.InterpExtend;
                    yPad = 1;
                }

                // Skip border extension if block is inside the frame.
                if (x0 < 0 || x0 > frameWidth - 1 || x1 < 0 || x1 > frameWidth - 1 ||
                    y0 < 0 || y0 > frameHeight - 1 || y1 < 0 || y1 > frameHeight - 1)
                {
                    // Extend the border.
                    byte* bufPtr1 = refFrame + (y0 * bufStride) + x0;
                    int bW = x1 - x0 + 1;
                    int bH = y1 - y0 + 1;
                    int borderOffset = (yPad * 3 * bW) + (xPad * 3);

                    ExtendAndPredict(
                        bufPtr1,
                        bufStride,
                        x0,
                        y0,
                        bW,
                        bH,
                        frameWidth,
                        frameHeight,
                        borderOffset,
                        dst,
                        dstBuf.Stride,
                        subpelX,
                        subpelY,
                        kernel,
                        ref sf,
                        ref xd,
                        w,
                        h,
                        refr,
                        xs,
                        ys);
                    return;
                }
            }

            if (xd.CurBuf.HighBd)
            {
                ReconInter.HighbdInterPredictor(
                    (ushort*)bufPtr,
                    bufStride,
                    (ushort*)dst,
                    dstBuf.Stride,
                    subpelX,
                    subpelY,
                    ref sf,
                    w,
                    h,
                    refr,
                    kernel,
                    xs,
                    ys,
                    xd.Bd);
            }
            else
            {
                ReconInter.InterPredictor(
                    bufPtr,
                    bufStride,
                    dst,
                    dstBuf.Stride,
                    subpelX,
                    subpelY,
                    ref sf,
                    w,
                    h,
                    refr,
                    kernel,
                    xs,
                    ys);
            }
        }

        private static void DecBuildInterPredictorsSb(ref Vp9Common cm, ref MacroBlockD xd, int miRow, int miCol)
        {
            int plane;
            int miX = miCol * Constants.MiSize;
            int miY = miRow * Constants.MiSize;
            ref ModeInfo mi = ref xd.Mi[0].Value;
            Array8<short>[] kernel = Luts.FilterKernels[mi.InterpFilter];
            BlockSize sbType = mi.SbType;
            int isCompound = mi.HasSecondRef() ? 1 : 0;
            int refr;
            bool isScaled;

            for (refr = 0; refr < 1 + isCompound; ++refr)
            {
                int frame = mi.RefFrame[refr];
                ref RefBuffer refBuf = ref cm.FrameRefs[frame - Constants.LastFrame];
                ref ScaleFactors sf = ref refBuf.Sf;
                ref Surface refFrameBuf = ref refBuf.Buf;

                if (!sf.IsValidScale())
                {
                    xd.ErrorInfo.Value.InternalError(CodecErr.UnsupBitstream,
                        "Reference frame has invalid dimensions");
                }

                isScaled = sf.IsScaled();
                ReconInter.SetupPrePlanes(ref xd, refr, ref refFrameBuf, miRow, miCol,
                    isScaled ? new Ptr<ScaleFactors>(ref sf) : Ptr<ScaleFactors>.Null);
                xd.BlockRefs[refr] = new Ptr<RefBuffer>(ref refBuf);

                if (sbType < BlockSize.Block8X8)
                {
                    for (plane = 0; plane < Constants.MaxMbPlane; ++plane)
                    {
                        ref MacroBlockDPlane pd = ref xd.Plane[plane];
                        ref Buf2D dstBuf = ref pd.Dst;
                        int num4X4W = pd.N4W;
                        int num4X4H = pd.N4H;
                        int n4Wx4 = 4 * num4X4W;
                        int n4Hx4 = 4 * num4X4H;
                        ref Buf2D preBuf = ref pd.Pre[refr];
                        int i = 0;
                        for (int y = 0; y < num4X4H; ++y)
                        {
                            for (int x = 0; x < num4X4W; ++x)
                            {
                                Mv mv = ReconInter.AverageSplitMvs(ref pd, ref mi, refr, i++);
                                DecBuildInterPredictors(
                                    ref xd,
                                    plane,
                                    n4Wx4,
                                    n4Hx4,
                                    4 * x,
                                    4 * y,
                                    4,
                                    4,
                                    miX,
                                    miY,
                                    kernel,
                                    ref sf,
                                    ref preBuf,
                                    ref dstBuf,
                                    ref mv,
                                    ref refFrameBuf,
                                    isScaled,
                                    refr);
                            }
                        }
                    }
                }
                else
                {
                    Mv mv = mi.Mv[refr];
                    for (plane = 0; plane < Constants.MaxMbPlane; ++plane)
                    {
                        ref MacroBlockDPlane pd = ref xd.Plane[plane];
                        ref Buf2D dstBuf = ref pd.Dst;
                        int num4X4W = pd.N4W;
                        int num4X4H = pd.N4H;
                        int n4Wx4 = 4 * num4X4W;
                        int n4Hx4 = 4 * num4X4H;
                        ref Buf2D preBuf = ref pd.Pre[refr];
                        DecBuildInterPredictors(
                            ref xd,
                            plane,
                            n4Wx4,
                            n4Hx4,
                            0,
                            0,
                            n4Wx4,
                            n4Hx4,
                            miX,
                            miY,
                            kernel,
                            ref sf,
                            ref preBuf,
                            ref dstBuf,
                            ref mv,
                            ref refFrameBuf,
                            isScaled,
                            refr);
                    }
                }
            }
        }

        private static void SetPlaneN4(ref MacroBlockD xd, int bw, int bh, int bwl, int bhl)
        {
            for (int i = 0; i < Constants.MaxMbPlane; i++)
            {
                xd.Plane[i].N4W = (ushort)((bw << 1) >> xd.Plane[i].SubsamplingX);
                xd.Plane[i].N4H = (ushort)((bh << 1) >> xd.Plane[i].SubsamplingY);
                xd.Plane[i].N4Wl = (byte)(bwl - xd.Plane[i].SubsamplingX);
                xd.Plane[i].N4Hl = (byte)(bhl - xd.Plane[i].SubsamplingY);
            }
        }

        private static ref ModeInfo SetOffsets(
            ref Vp9Common cm,
            ref MacroBlockD xd,
            BlockSize bsize,
            int miRow,
            int miCol,
            int bw,
            int bh,
            int xMis,
            int yMis,
            int bwl,
            int bhl)
        {
            int offset = (miRow * cm.MiStride) + miCol;

            ref TileInfo tile = ref xd.Tile;

            xd.Mi = cm.MiGridVisible.Slice(offset);
            xd.Mi[0] = new Ptr<ModeInfo>(ref cm.Mi[offset]);
            xd.Mi[0].Value.SbType = bsize;
            for (int y = 0; y < yMis; ++y)
            {
                for (int x = y == 0 ? 1 : 0; x < xMis; ++x)
                {
                    xd.Mi[(y * cm.MiStride) + x] = xd.Mi[0];
                }
            }

            SetPlaneN4(ref xd, bw, bh, bwl, bhl);

            xd.SetSkipContext(miRow, miCol);

            // Distance of Mb to the various image edges. These are specified to 8th pel
            // as they are always compared to values that are in 1/8th pel units
            xd.SetMiRowCol(ref tile, miRow, bh, miCol, bw, cm.MiRows, cm.MiCols);

            ReconInter.SetupDstPlanes(ref xd.Plane, ref xd.CurBuf, miRow, miCol);
            return ref xd.Mi[0].Value;
        }

        private static void DecodeBlock(
            ref TileWorkerData twd,
            ref Vp9Common cm,
            int miRow,
            int miCol,
            BlockSize bsize,
            int bwl,
            int bhl)
        {
            bool less8X8 = bsize < BlockSize.Block8X8;
            int bw = 1 << (bwl - 1);
            int bh = 1 << (bhl - 1);
            int xMis = Math.Min(bw, cm.MiCols - miCol);
            int yMis = Math.Min(bh, cm.MiRows - miRow);
            ref Reader r = ref twd.BitReader;
            ref MacroBlockD xd = ref twd.Xd;

            ref ModeInfo mi = ref SetOffsets(ref cm, ref xd, bsize, miRow, miCol, bw, bh, xMis, yMis, bwl, bhl);

            if (bsize >= BlockSize.Block8X8 && (cm.SubsamplingX != 0 || cm.SubsamplingY != 0))
            {
                BlockSize uvSubsize = Luts.SsSizeLookup[(int)bsize][cm.SubsamplingX][cm.SubsamplingY];
                if (uvSubsize == BlockSize.BlockInvalid)
                {
                    xd.ErrorInfo.Value.InternalError(CodecErr.CorruptFrame, "Invalid block size.");
                }
            }

            DecodeMv.ReadModeInfo(ref twd, ref cm, miRow, miCol, xMis, yMis);

            if (mi.Skip != 0)
            {
                xd.DecResetSkipContext();
            }

            if (!mi.IsInterBlock())
            {
                int plane;
                for (plane = 0; plane < Constants.MaxMbPlane; ++plane)
                {
                    ref MacroBlockDPlane pd = ref xd.Plane[plane];
                    TxSize txSize = plane != 0 ? mi.GetUvTxSize(ref pd) : mi.TxSize;
                    int num4X4W = pd.N4W;
                    int num4X4H = pd.N4H;
                    int step = 1 << (int)txSize;
                    int row, col;
                    int maxBlocksWide =
                        num4X4W + (xd.MbToRightEdge >= 0 ? 0 : xd.MbToRightEdge >> (5 + pd.SubsamplingX));
                    int maxBlocksHigh =
                        num4X4H + (xd.MbToBottomEdge >= 0 ? 0 : xd.MbToBottomEdge >> (5 + pd.SubsamplingY));

                    xd.MaxBlocksWide = (uint)(xd.MbToRightEdge >= 0 ? 0 : maxBlocksWide);
                    xd.MaxBlocksHigh = (uint)(xd.MbToBottomEdge >= 0 ? 0 : maxBlocksHigh);

                    for (row = 0; row < maxBlocksHigh; row += step)
                    {
                        for (col = 0; col < maxBlocksWide; col += step)
                        {
                            PredictAndReconstructIntraBlock(ref twd, ref mi, plane, row, col, txSize);
                        }
                    }
                }
            }
            else
            {
                // Prediction
                DecBuildInterPredictorsSb(ref cm, ref xd, miRow, miCol);

                // Reconstruction
                if (mi.Skip == 0)
                {
                    int eobtotal = 0;
                    int plane;

                    for (plane = 0; plane < Constants.MaxMbPlane; ++plane)
                    {
                        ref MacroBlockDPlane pd = ref xd.Plane[plane];
                        TxSize txSize = plane != 0 ? mi.GetUvTxSize(ref pd) : mi.TxSize;
                        int num4X4W = pd.N4W;
                        int num4X4H = pd.N4H;
                        int step = 1 << (int)txSize;
                        int row, col;
                        int maxBlocksWide =
                            num4X4W + (xd.MbToRightEdge >= 0 ? 0 : xd.MbToRightEdge >> (5 + pd.SubsamplingX));
                        int maxBlocksHigh = num4X4H +
                                            (xd.MbToBottomEdge >= 0 ? 0 : xd.MbToBottomEdge >> (5 + pd.SubsamplingY));

                        xd.MaxBlocksWide = (uint)(xd.MbToRightEdge >= 0 ? 0 : maxBlocksWide);
                        xd.MaxBlocksHigh = (uint)(xd.MbToBottomEdge >= 0 ? 0 : maxBlocksHigh);

                        for (row = 0; row < maxBlocksHigh; row += step)
                        {
                            for (col = 0; col < maxBlocksWide; col += step)
                            {
                                eobtotal += ReconstructInterBlock(ref twd, ref mi, plane, row, col, txSize);
                            }
                        }
                    }

                    if (!less8X8 && eobtotal == 0)
                    {
                        mi.Skip = 1; // Skip loopfilter
                    }
                }
            }

            xd.Corrupted |= r.HasError();

            if (cm.Lf.FilterLevel != 0)
            {
                LoopFilter.BuildMask(ref cm, ref mi, miRow, miCol, bw, bh);
            }
        }

        private static void DecUpdatePartitionContext(
            ref TileWorkerData twd,
            int miRow,
            int miCol,
            BlockSize subsize,
            int bw)
        {
            Span<sbyte> aboveCtx = twd.Xd.AboveSegContext.Slice(miCol).AsSpan();
            Span<sbyte> leftCtx = MemoryMarshal.CreateSpan(ref twd.Xd.LeftSegContext[miRow & Constants.MiMask],
                8 - (miRow & Constants.MiMask));

            // Update the partition context at the end notes. Set partition bits
            // of block sizes larger than the current one to be one, and partition
            // bits of smaller block sizes to be zero.
            aboveCtx.Slice(0, bw).Fill(Luts.PartitionContextLookup[(int)subsize].Above);
            leftCtx.Slice(0, bw).Fill(Luts.PartitionContextLookup[(int)subsize].Left);
        }

        private static PartitionType ReadPartition(
            ref TileWorkerData twd,
            int miRow,
            int miCol,
            int hasRows,
            int hasCols,
            int bsl)
        {
            int ctx = twd.DecPartitionPlaneContext(miRow, miCol, bsl);
            ReadOnlySpan<byte> probs = MemoryMarshal.CreateReadOnlySpan(ref twd.Xd.PartitionProbs[ctx][0], 3);
            PartitionType p;
            ref Reader r = ref twd.BitReader;

            if (hasRows != 0 && hasCols != 0)
            {
                p = (PartitionType)r.ReadTree(Luts.PartitionTree, probs);
            }
            else if (hasRows == 0 && hasCols != 0)
            {
                p = r.Read(probs[1]) != 0 ? PartitionType.PartitionSplit : PartitionType.PartitionHorz;
            }
            else if (hasRows != 0 && hasCols == 0)
            {
                p = r.Read(probs[2]) != 0 ? PartitionType.PartitionSplit : PartitionType.PartitionVert;
            }
            else
            {
                p = PartitionType.PartitionSplit;
            }

            if (!twd.Xd.Counts.IsNull)
            {
                ++twd.Xd.Counts.Value.Partition[ctx][(int)p];
            }

            return p;
        }

        private static void DecodePartition(
            ref TileWorkerData twd,
            ref Vp9Common cm,
            int miRow,
            int miCol,
            BlockSize bsize,
            int n4X4L2)
        {
            int n8X8L2 = n4X4L2 - 1;
            int num8X8Wh = 1 << n8X8L2;
            int hbs = num8X8Wh >> 1;
            PartitionType partition;
            BlockSize subsize;
            bool hasRows = miRow + hbs < cm.MiRows;
            bool hasCols = miCol + hbs < cm.MiCols;
            ref MacroBlockD xd = ref twd.Xd;

            if (miRow >= cm.MiRows || miCol >= cm.MiCols)
            {
                return;
            }

            partition = ReadPartition(ref twd, miRow, miCol, hasRows ? 1 : 0, hasCols ? 1 : 0, n8X8L2);
            subsize = Luts.SubsizeLookup[(int)partition][(int)bsize];
            if (hbs == 0)
            {
                // Calculate bmode block dimensions (log 2)
                xd.BmodeBlocksWl = (byte)(1 >> ((partition & PartitionType.PartitionVert) != 0 ? 1 : 0));
                xd.BmodeBlocksHl = (byte)(1 >> ((partition & PartitionType.PartitionHorz) != 0 ? 1 : 0));
                DecodeBlock(ref twd, ref cm, miRow, miCol, subsize, 1, 1);
            }
            else
            {
                switch (partition)
                {
                    case PartitionType.PartitionNone:
                        DecodeBlock(ref twd, ref cm, miRow, miCol, subsize, n4X4L2, n4X4L2);
                        break;
                    case PartitionType.PartitionHorz:
                        DecodeBlock(ref twd, ref cm, miRow, miCol, subsize, n4X4L2, n8X8L2);
                        if (hasRows)
                        {
                            DecodeBlock(ref twd, ref cm, miRow + hbs, miCol, subsize, n4X4L2, n8X8L2);
                        }

                        break;
                    case PartitionType.PartitionVert:
                        DecodeBlock(ref twd, ref cm, miRow, miCol, subsize, n8X8L2, n4X4L2);
                        if (hasCols)
                        {
                            DecodeBlock(ref twd, ref cm, miRow, miCol + hbs, subsize, n8X8L2, n4X4L2);
                        }

                        break;
                    case PartitionType.PartitionSplit:
                        DecodePartition(ref twd, ref cm, miRow, miCol, subsize, n8X8L2);
                        DecodePartition(ref twd, ref cm, miRow, miCol + hbs, subsize, n8X8L2);
                        DecodePartition(ref twd, ref cm, miRow + hbs, miCol, subsize, n8X8L2);
                        DecodePartition(ref twd, ref cm, miRow + hbs, miCol + hbs, subsize, n8X8L2);
                        break;
                    default:
                        Debug.Assert(false, "Invalid partition type");
                        break;
                }
            }

            // Update partition context
            if (bsize >= BlockSize.Block8X8 &&
                (bsize == BlockSize.Block8X8 || partition != PartitionType.PartitionSplit))
            {
                DecUpdatePartitionContext(ref twd, miRow, miCol, subsize, num8X8Wh);
            }
        }

        private static void SetupTokenDecoder(
            ArrayPtr<byte> data,
            int readSize,
            ref InternalErrorInfo errorInfo,
            ref Reader r)
        {
            // Validate the calculated partition length. If the buffer described by the
            // partition can't be fully read then throw an error.
            if (!ReadIsValid(data, readSize))
            {
                errorInfo.InternalError(CodecErr.CorruptFrame, "Truncated packet or corrupt tile length");
            }

            if (r.Init(data, readSize))
            {
                errorInfo.InternalError(CodecErr.MemError, "Failed to allocate bool decoder 1");
            }
        }

        private static void ReadCoefProbsCommon(ref Array2<Array2<Array6<Array6<Array3<byte>>>>> coefProbs,
            ref Reader r, int txSize)
        {
            if (r.ReadBit() != 0)
            {
                for (int i = 0; i < Constants.PlaneTypes; ++i)
                {
                    for (int j = 0; j < Entropy.RefTypes; ++j)
                    {
                        for (int k = 0; k < Entropy.CoefBands; ++k)
                        {
                            for (int l = 0; l < Entropy.BAND_COEFF_CONTEXTS(k); ++l)
                            {
                                for (int m = 0; m < Entropy.UnconstrainedNodes; ++m)
                                {
                                    r.DiffUpdateProb( ref coefProbs[i][j][k][l][m]);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void ReadCoefProbs(ref Vp9EntropyProbs fc, TxMode txMode, ref Reader r)
        {
            int maxTxSize = (int)Luts.TxModeToBiggestTxSize[(int)txMode];
            for (int txSize = (int)TxSize.Tx4X4; txSize <= maxTxSize; ++txSize)
            {
                ReadCoefProbsCommon(ref fc.CoefProbs[txSize], ref r, txSize);
            }
        }

        private static void SetupLoopfilter(ref Types.LoopFilter lf, ref ReadBitBuffer rb)
        {
            lf.FilterLevel = rb.ReadLiteral(6);
            lf.SharpnessLevel = rb.ReadLiteral(3);

            // Read in loop filter deltas applied at the MB level based on mode or ref
            // frame.
            lf.ModeRefDeltaUpdate = false;

            lf.ModeRefDeltaEnabled = rb.ReadBit() != 0;
            if (lf.ModeRefDeltaEnabled)
            {
                lf.ModeRefDeltaUpdate = rb.ReadBit() != 0;
                if (lf.ModeRefDeltaUpdate)
                {
                    for (int i = 0; i < LoopFilter.MaxRefLfDeltas; i++)
                    {
                        if (rb.ReadBit() != 0)
                        {
                            lf.RefDeltas[i] = (sbyte)rb.ReadSignedLiteral(6);
                        }
                    }

                    for (int i = 0; i < LoopFilter.MaxModeLfDeltas; i++)
                    {
                        if (rb.ReadBit() != 0)
                        {
                            lf.ModeDeltas[i] = (sbyte)rb.ReadSignedLiteral(6);
                        }
                    }
                }
            }
        }

        private static void SetupQuantization(ref Vp9Common cm, ref MacroBlockD xd, ref ReadBitBuffer rb)
        {
            cm.BaseQindex = rb.ReadLiteral(QuantCommon.QindexBits);
            cm.YDcDeltaQ = rb.ReadDeltaQ();
            cm.UvDcDeltaQ = rb.ReadDeltaQ();
            cm.UvAcDeltaQ = rb.ReadDeltaQ();
            cm.DequantBitDepth = cm.BitDepth;
            xd.Lossless = cm.BaseQindex == 0 && cm.YDcDeltaQ == 0 && cm.UvDcDeltaQ == 0 && cm.UvAcDeltaQ == 0;

            xd.Bd = (int)cm.BitDepth;
        }

        private static readonly byte[] _literalToFilter =
        [
            Constants.EightTapSmooth, Constants.EightTap, Constants.EightTapSharp, Constants.Bilinear
        ];

        private static byte ReadInterpFilter(ref ReadBitBuffer rb)
        {
            return rb.ReadBit() != 0
                ? (byte)Constants.Switchable
                : _literalToFilter[rb.ReadLiteral(2)];
        }

        private static void SetupRenderSize(ref Vp9Common cm, ref ReadBitBuffer rb)
        {
            cm.RenderWidth = cm.Width;
            cm.RenderHeight = cm.Height;
            if (rb.ReadBit() != 0)
            {
                rb.ReadFrameSize(out cm.RenderWidth, out cm.RenderHeight);
            }
        }

        private static void SetupFrameSize(MemoryAllocator allocator, ref Vp9Common cm, ref ReadBitBuffer rb)
        {
            int width = 0, height = 0;
            ref BufferPool pool = ref cm.BufferPool.Value;
            rb.ReadFrameSize(out width, out height);
            cm.ResizeContextBuffers(allocator, width, height);
            SetupRenderSize(ref cm, ref rb);

            if (cm.GetFrameNewBuffer().ReallocFrameBuffer(
                    allocator,
                    cm.Width,
                    cm.Height,
                    cm.SubsamplingX,
                    cm.SubsamplingY,
                    cm.UseHighBitDepth,
                    Surface.DecBorderInPixels,
                    cm.ByteAlignment,
                    new Ptr<VpxCodecFrameBuffer>(ref pool.FrameBufs[cm.NewFbIdx].RawFrameBuffer),
                    FrameBuffers.GetFrameBuffer,
                    pool.CbPriv) != 0)
            {
                cm.Error.InternalError(CodecErr.MemError, "Failed to allocate frame buffer");
            }

            pool.FrameBufs[cm.NewFbIdx].Released = 0;
            pool.FrameBufs[cm.NewFbIdx].Buf.SubsamplingX = cm.SubsamplingX;
            pool.FrameBufs[cm.NewFbIdx].Buf.SubsamplingY = cm.SubsamplingY;
            pool.FrameBufs[cm.NewFbIdx].Buf.BitDepth = (uint)cm.BitDepth;
            pool.FrameBufs[cm.NewFbIdx].Buf.ColorSpace = cm.ColorSpace;
            pool.FrameBufs[cm.NewFbIdx].Buf.ColorRange = cm.ColorRange;
            pool.FrameBufs[cm.NewFbIdx].Buf.RenderWidth = cm.RenderWidth;
            pool.FrameBufs[cm.NewFbIdx].Buf.RenderHeight = cm.RenderHeight;
        }

        private static bool ValidRefFrameImgFmt(
            BitDepth refBitDepth,
            int refXss, int refYss,
            BitDepth thisBitDepth,
            int thisXss,
            int thisYss)
        {
            return refBitDepth == thisBitDepth && refXss == thisXss && refYss == thisYss;
        }

        private static void SetupFrameSizeWithRefs(MemoryAllocator allocator, ref Vp9Common cm,
            ref ReadBitBuffer rb)
        {
            int width = 0, height = 0;
            bool found = false;

            bool hasValidRefFrame = false;
            ref BufferPool pool = ref cm.BufferPool.Value;
            for (int i = 0; i < Constants.RefsPerFrame; ++i)
            {
                if (rb.ReadBit() != 0)
                {
                    if (cm.FrameRefs[i].Idx != RefBuffer.InvalidIdx)
                    {
                        ref Surface buf = ref cm.FrameRefs[i].Buf;
                        width = buf.YCropWidth;
                        height = buf.YCropHeight;
                        found = true;
                        break;
                    }

                    cm.Error.InternalError(CodecErr.CorruptFrame, "Failed to decode frame size");
                }
            }

            if (!found)
            {
                rb.ReadFrameSize(out width, out height);
            }

            if (width <= 0 || height <= 0)
            {
                cm.Error.InternalError(CodecErr.CorruptFrame, "Invalid frame size");
            }

            // Check to make sure at least one of frames that this frame references
            // has valid dimensions.
            for (int i = 0; i < Constants.RefsPerFrame; ++i)
            {
                ref RefBuffer refFrame = ref cm.FrameRefs[i];
                hasValidRefFrame |=
                    refFrame.Idx != RefBuffer.InvalidIdx &&
                    ScaleFactors.ValidRefFrameSize(refFrame.Buf.YCropWidth, refFrame.Buf.YCropHeight, width,
                        height);
            }

            if (!hasValidRefFrame)
            {
                cm.Error.InternalError(CodecErr.CorruptFrame, "Referenced frame has invalid size");
            }

            for (int i = 0; i < Constants.RefsPerFrame; ++i)
            {
                ref RefBuffer refFrame = ref cm.FrameRefs[i];
                if (refFrame.Idx == RefBuffer.InvalidIdx ||
                    !ValidRefFrameImgFmt(
                        (BitDepth)refFrame.Buf.BitDepth,
                        refFrame.Buf.SubsamplingX,
                        refFrame.Buf.SubsamplingY,
                        cm.BitDepth,
                        cm.SubsamplingX,
                        cm.SubsamplingY))
                {
                    cm.Error.InternalError(CodecErr.CorruptFrame,
                        "Referenced frame has incompatible color format");
                }
            }

            cm.ResizeContextBuffers(allocator, width, height);
            SetupRenderSize(ref cm, ref rb);

            if (cm.GetFrameNewBuffer().ReallocFrameBuffer(
                    allocator,
                    cm.Width,
                    cm.Height,
                    cm.SubsamplingX,
                    cm.SubsamplingY,
                    cm.UseHighBitDepth,
                    Surface.DecBorderInPixels,
                    cm.ByteAlignment,
                    new Ptr<VpxCodecFrameBuffer>(ref pool.FrameBufs[cm.NewFbIdx].RawFrameBuffer),
                    FrameBuffers.GetFrameBuffer,
                    pool.CbPriv) != 0)
            {
                cm.Error.InternalError(CodecErr.MemError, "Failed to allocate frame buffer");
            }

            pool.FrameBufs[cm.NewFbIdx].Released = 0;
            pool.FrameBufs[cm.NewFbIdx].Buf.SubsamplingX = cm.SubsamplingX;
            pool.FrameBufs[cm.NewFbIdx].Buf.SubsamplingY = cm.SubsamplingY;
            pool.FrameBufs[cm.NewFbIdx].Buf.BitDepth = (uint)cm.BitDepth;
            pool.FrameBufs[cm.NewFbIdx].Buf.ColorSpace = cm.ColorSpace;
            pool.FrameBufs[cm.NewFbIdx].Buf.ColorRange = cm.ColorRange;
            pool.FrameBufs[cm.NewFbIdx].Buf.RenderWidth = cm.RenderWidth;
            pool.FrameBufs[cm.NewFbIdx].Buf.RenderHeight = cm.RenderHeight;
        }

        // Reads the next tile returning its size and adjusting '*data' accordingly
        // based on 'isLast'.
        private static void GetTileBuffer(
            bool isLast,
            ref InternalErrorInfo errorInfo,
            ref ArrayPtr<byte> data,
            ref TileBuffer buf)
        {
            int size;

            if (!isLast)
            {
                if (!ReadIsValid(data, 4))
                {
                    errorInfo.InternalError(CodecErr.CorruptFrame, "Truncated packet or corrupt tile length");
                }

                size = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan());
                data = data.Slice(4);

                if (size > data.Length)
                {
                    errorInfo.InternalError(CodecErr.CorruptFrame, "Truncated packet or corrupt tile size");
                }
            }
            else
            {
                size = data.Length;
            }

            buf.Data = data;
            buf.Size = size;

            data = data.Slice(size);
        }

        private static void GetTileBuffers(ref Vp9Common cm, ArrayPtr<byte> data, int tileCols,
            ref Array64<TileBuffer> tileBuffers)
        {
            for (int c = 0; c < tileCols; ++c)
            {
                bool isLast = c == tileCols - 1;
                ref TileBuffer buf = ref tileBuffers[c];
                buf.Col = c;
                GetTileBuffer(isLast, ref cm.Error, ref data, ref buf);
            }
        }

        private static void GetTileBuffers(
            ref Vp9Common cm,
            ArrayPtr<byte> data,
            int tileCols,
            int tileRows,
            ref Array4<Array64<TileBuffer>> tileBuffers)
        {
            for (int r = 0; r < tileRows; ++r)
            {
                for (int c = 0; c < tileCols; ++c)
                {
                    bool isLast = r == tileRows - 1 && c == tileCols - 1;
                    ref TileBuffer buf = ref tileBuffers[r][c];
                    GetTileBuffer(isLast, ref cm.Error, ref data, ref buf);
                }
            }
        }

        public static unsafe ArrayPtr<byte> DecodeTiles(ref Vp9Common cm, ArrayPtr<byte> data)
        {
            int alignedCols = TileInfo.MiColsAlignedToSb(cm.MiCols);
            int tileCols = 1 << cm.Log2TileCols;
            int tileRows = 1 << cm.Log2TileRows;
            Array4<Array64<TileBuffer>> tileBuffers = new();
            int tileRow, tileCol;
            int miRow, miCol;

            Debug.Assert(tileRows <= 4);
            Debug.Assert(tileCols <= 1 << 6);

            // Note: this memset assumes above_context[0], [1] and [2]
            // are allocated as part of the same buffer.
            MemoryUtil.Fill(cm.AboveContext.ToPointer(), (sbyte)0, Constants.MaxMbPlane * 2 * alignedCols);
            MemoryUtil.Fill(cm.AboveSegContext.ToPointer(), (sbyte)0, alignedCols);

            LoopFilter.ResetLfm(ref cm);

            GetTileBuffers(ref cm, data, tileCols, tileRows, ref tileBuffers);
            // Load all tile information into tile_data.
            for (tileRow = 0; tileRow < tileRows; ++tileRow)
            {
                for (tileCol = 0; tileCol < tileCols; ++tileCol)
                {
                    ref TileBuffer buf = ref tileBuffers[tileRow][tileCol];
                    ref TileWorkerData tileData = ref cm.TileWorkerData[(tileCols * tileRow) + tileCol];
                    tileData.Xd = cm.Mb;
                    tileData.Xd.Corrupted = false;
                    tileData.Xd.Counts = cm.Counts;
                    tileData.Dqcoeff = new Array32<Array32<int>>();
                    tileData.Xd.Tile.Init(ref cm, tileRow, tileCol);
                    SetupTokenDecoder(buf.Data, buf.Size, ref cm.Error, ref tileData.BitReader);
                    cm.InitMacroBlockD(ref tileData.Xd, new ArrayPtr<int>(ref tileData.Dqcoeff[0][0], 32 * 32));
                }
            }

            for (tileRow = 0; tileRow < tileRows; ++tileRow)
            {
                TileInfo tile = new();
                tile.SetRow(ref cm, tileRow);
                for (miRow = tile.MiRowStart; miRow < tile.MiRowEnd; miRow += Constants.MiBlockSize)
                {
                    for (tileCol = 0; tileCol < tileCols; ++tileCol)
                    {
                        int col = tileCol;
                        ref TileWorkerData tileData = ref cm.TileWorkerData[(tileCols * tileRow) + col];
                        tile.SetCol(ref cm, col);
                        tileData.Xd.LeftContext = new Array3<Array16<sbyte>>();
                        tileData.Xd.LeftSegContext = new Array8<sbyte>();
                        for (miCol = tile.MiColStart; miCol < tile.MiColEnd; miCol += Constants.MiBlockSize)
                        {
                            DecodePartition(ref tileData, ref cm, miRow, miCol, BlockSize.Block64X64, 4);
                        }

                        cm.Mb.Corrupted |= tileData.Xd.Corrupted;
                        if (cm.Mb.Corrupted)
                        {
                            cm.Error.InternalError(CodecErr.CorruptFrame, "Failed to decode tile data");
                        }
                    }
                }
            }

            // Get last tile data.
            return cm.TileWorkerData[(tileCols * tileRows) - 1].BitReader.FindEnd();
        }

        private static bool DecodeTileCol(ref TileWorkerData tileData, ref Vp9Common cm,
            ref Array64<TileBuffer> tileBuffers)
        {
            ref TileInfo tile = ref tileData.Xd.Tile;
            int finalCol = (1 << cm.Log2TileCols) - 1;
            ArrayPtr<byte> bitReaderEnd = ArrayPtr<byte>.Null;

            int n = tileData.BufStart;

            tileData.Xd.Corrupted = false;

            do
            {
                ref TileBuffer buf = ref tileBuffers[n];

                Debug.Assert(cm.Log2TileRows == 0);
                tileData.Dqcoeff = new Array32<Array32<int>>();
                tile.Init(ref cm, 0, buf.Col);
                SetupTokenDecoder(buf.Data, buf.Size, ref tileData.ErrorInfo, ref tileData.BitReader);
                cm.InitMacroBlockD(ref tileData.Xd, new ArrayPtr<int>(ref tileData.Dqcoeff[0][0], 32 * 32));
                tileData.Xd.ErrorInfo = new Ptr<InternalErrorInfo>(ref tileData.ErrorInfo);

                for (int miRow = tile.MiRowStart; miRow < tile.MiRowEnd; miRow += Constants.MiBlockSize)
                {
                    tileData.Xd.LeftContext = new Array3<Array16<sbyte>>();
                    tileData.Xd.LeftSegContext = new Array8<sbyte>();
                    for (int miCol = tile.MiColStart; miCol < tile.MiColEnd; miCol += Constants.MiBlockSize)
                    {
                        DecodePartition(ref tileData, ref cm, miRow, miCol, BlockSize.Block64X64, 4);
                    }
                }

                if (buf.Col == finalCol)
                {
                    bitReaderEnd = tileData.BitReader.FindEnd();
                }
            } while (!tileData.Xd.Corrupted && ++n <= tileData.BufEnd);

            tileData.DataEnd = bitReaderEnd;
            return !tileData.Xd.Corrupted;
        }

        public static ArrayPtr<byte> DecodeTilesMt(ref Vp9Common cm, ArrayPtr<byte> data, int maxThreads)
        {
            ArrayPtr<byte> bitReaderEnd = ArrayPtr<byte>.Null;

            int tileCols = 1 << cm.Log2TileCols;
            int tileRows = 1 << cm.Log2TileRows;
            int totalTiles = tileCols * tileRows;
            int numWorkers = Math.Min(maxThreads, tileCols);
            int n;

            Debug.Assert(tileCols <= 1 << 6);
            Debug.Assert(tileRows == 1);

            LoopFilter.ResetLfm(ref cm);

            cm.AboveContext.AsSpan().Clear();
            cm.AboveSegContext.AsSpan().Clear();

            for (n = 0; n < numWorkers; ++n)
            {
                ref TileWorkerData tileData = ref cm.TileWorkerData[n + totalTiles];

                tileData.Xd = cm.Mb;
                tileData.Xd.Counts = new Ptr<Vp9BackwardUpdates>(ref tileData.Counts);
                tileData.Counts = new Vp9BackwardUpdates();
            }

            Array64<TileBuffer> tileBuffers = new();

            GetTileBuffers(ref cm, data, tileCols, ref tileBuffers);

            tileBuffers.AsSpan().Slice(0, tileCols).Sort(CompareTileBuffers);

            if (numWorkers == tileCols)
            {
                TileBuffer largest = tileBuffers[0];
                Span<TileBuffer> buffers = tileBuffers.AsSpan();
                buffers.Slice(1).CopyTo(buffers.Slice(0, tileBuffers.Length - 1));
                tileBuffers[tileCols - 1] = largest;
            }
            else
            {
                int start = 0, end = tileCols - 2;
                TileBuffer tmp;

                // Interleave the tiles to distribute the load between threads, assuming a
                // larger tile implies it is more difficult to decode.
                while (start < end)
                {
                    tmp = tileBuffers[start];
                    tileBuffers[start] = tileBuffers[end];
                    tileBuffers[end] = tmp;
                    start += 2;
                    end -= 2;
                }
            }

            int baseVal = tileCols / numWorkers;
            int remain = tileCols % numWorkers;
            int bufStart = 0;

            for (n = 0; n < numWorkers; ++n)
            {
                int count = baseVal + ((remain + n) / numWorkers);
                ref TileWorkerData tileData = ref cm.TileWorkerData[n + totalTiles];

                tileData.BufStart = bufStart;
                tileData.BufEnd = bufStart + count - 1;
                tileData.DataEnd = data.Slice(data.Length);
                bufStart += count;
            }

            Ptr<Vp9Common> cmPtr = new(ref cm);

            Parallel.For(0, numWorkers, n =>
            {
                ref TileWorkerData tileData = ref cmPtr.Value.TileWorkerData[n + totalTiles];

                if (!DecodeTileCol(ref tileData, ref cmPtr.Value, ref tileBuffers))
                {
                    cmPtr.Value.Mb.Corrupted = true;
                }
            });

            for (; n > 0; --n)
            {
                if (bitReaderEnd.IsNull)
                {
                    ref TileWorkerData tileData = ref cm.TileWorkerData[n - 1 + totalTiles];
                    bitReaderEnd = tileData.DataEnd;
                }
            }

            for (n = 0; n < numWorkers; ++n)
            {
                ref TileWorkerData tileData = ref cm.TileWorkerData[n + totalTiles];
                AccumulateFrameCounts(ref cm.Counts.Value, ref tileData.Counts);
            }

            Debug.Assert(!bitReaderEnd.IsNull || cm.Mb.Corrupted);
            return bitReaderEnd;
        }

        private static int CompareTileBuffers(TileBuffer bufA, TileBuffer bufB)
        {
            return (bufA.Size < bufB.Size ? 1 : 0) - (bufA.Size > bufB.Size ? 1 : 0);
        }

        private static void AccumulateFrameCounts(ref Vp9BackwardUpdates accum, ref Vp9BackwardUpdates counts)
        {
            Span<uint> a = MemoryMarshal.Cast<Vp9BackwardUpdates, uint>(MemoryMarshal.CreateSpan(ref accum, 1));
            Span<uint> c = MemoryMarshal.Cast<Vp9BackwardUpdates, uint>(MemoryMarshal.CreateSpan(ref counts, 1));

            for (int i = 0; i < a.Length; i++)
            {
                a[i] += c[i];
            }
        }

        private static void ErrorHandler(Ptr<Vp9Common> data)
        {
            ref Vp9Common cm = ref data.Value;
            cm.Error.InternalError(CodecErr.CorruptFrame, "Truncated packet");
        }

        private static void FlushAllFbOnKey(ref Vp9Common cm)
        {
            if (cm.FrameType == FrameType.KeyFrame && cm.CurrentVideoFrame > 0)
            {
                ref Array12<RefCntBuffer> frameBufs = ref cm.BufferPool.Value.FrameBufs;
                ref BufferPool pool = ref cm.BufferPool.Value;

                for (int i = 0; i < Constants.FrameBuffers; ++i)
                {
                    if (i == cm.NewFbIdx)
                    {
                        continue;
                    }

                    frameBufs[i].RefCount = 0;
                    if (frameBufs[i].Released == 0)
                    {
                        FrameBuffers.ReleaseFrameBuffer(pool.CbPriv, ref frameBufs[i].RawFrameBuffer);
                        frameBufs[i].Released = 1;
                    }
                }
            }
        }

        private const int SyncCode0 = 0x49;
        private const int SyncCode1 = 0x83;
        private const int SyncCode2 = 0x42;

        private const int FrameMarker = 0x2;

        private static bool ReadSyncCode(ref ReadBitBuffer rb)
        {
            return rb.ReadLiteral(8) == SyncCode0 &&
                   rb.ReadLiteral(8) == SyncCode1 &&
                   rb.ReadLiteral(8) == SyncCode2;
        }

        private static void RefCntFb(ref Array12<RefCntBuffer> bufs, ref int idx, int newIdx)
        {
            int refIndex = idx;

            if (refIndex >= 0 && bufs[refIndex].RefCount > 0)
            {
                bufs[refIndex].RefCount--;
            }

            idx = newIdx;

            bufs[newIdx].RefCount++;
        }

        private static ulong ReadUncompressedHeader(MemoryAllocator allocator, ref Vp9Decoder pbi,
            ref ReadBitBuffer rb)
        {
            ref Vp9Common cm = ref pbi.Common;
            ref BufferPool pool = ref cm.BufferPool.Value;
            ref Array12<RefCntBuffer> frameBufs = ref pool.FrameBufs;
            int mask, refIndex = 0;
            ulong sz;

            cm.LastFrameType = cm.FrameType;
            cm.LastIntraOnly = cm.IntraOnly;

            if (rb.ReadLiteral(2) != FrameMarker)
            {
                cm.Error.InternalError(CodecErr.UnsupBitstream, "Invalid frame marker");
            }

            cm.Profile = rb.ReadProfile();
            if (cm.Profile >= BitstreamProfile.MaxProfiles)
            {
                cm.Error.InternalError(CodecErr.UnsupBitstream, "Unsupported bitstream profile");
            }

            cm.ShowExistingFrame = rb.ReadBit();
            if (cm.ShowExistingFrame != 0)
            {
                // Show an existing frame directly.
                int frameToShow = cm.RefFrameMap[rb.ReadLiteral(3)];
                if (frameToShow < 0 || frameBufs[frameToShow].RefCount < 1)
                {
                    cm.Error.InternalError(CodecErr.UnsupBitstream,
                        $"Buffer {frameToShow} does not contain a decoded frame");
                }

                RefCntFb(ref frameBufs, ref cm.NewFbIdx, frameToShow);
                pbi.RefreshFrameFlags = 0;
                cm.Lf.FilterLevel = 0;
                cm.ShowFrame = 1;

                return 0;
            }

            cm.FrameType = (FrameType)rb.ReadBit();
            cm.ShowFrame = rb.ReadBit();
            cm.ErrorResilientMode = rb.ReadBit();

            if (cm.FrameType == FrameType.KeyFrame)
            {
                if (!ReadSyncCode(ref rb))
                {
                    cm.Error.InternalError(CodecErr.UnsupBitstream, "Invalid frame sync code");
                }

                cm.ReadBitdepthColorspaceSampling(ref rb);
                pbi.RefreshFrameFlags = (1 << Constants.RefFrames) - 1;

                for (int i = 0; i < Constants.RefsPerFrame; ++i)
                {
                    cm.FrameRefs[i].Idx = RefBuffer.InvalidIdx;
                    cm.FrameRefs[i].Buf = default;
                }

                SetupFrameSize(allocator, ref cm, ref rb);
                if (pbi.NeedResync != 0)
                {
                    cm.RefFrameMap.AsSpan().Fill(-1);
                    FlushAllFbOnKey(ref cm);
                    pbi.NeedResync = 0;
                }
            }
            else
            {
                cm.IntraOnly = (cm.ShowFrame != 0 ? 0 : rb.ReadBit()) != 0;

                cm.ResetFrameContext = cm.ErrorResilientMode != 0 ? 0 : rb.ReadLiteral(2);

                if (cm.IntraOnly)
                {
                    if (!ReadSyncCode(ref rb))
                    {
                        cm.Error.InternalError(CodecErr.UnsupBitstream, "Invalid frame sync code");
                    }

                    if (cm.Profile > BitstreamProfile.Profile0)
                    {
                        cm.ReadBitdepthColorspaceSampling(ref rb);
                    }
                    else
                    {
                        // NOTE: The intra-only frame header does not include the specification
                        // of either the color format or color sub-sampling in profile 0. VP9
                        // specifies that the default color format should be YUV 4:2:0 in this
                        // case (normative).
                        cm.ColorSpace = VpxColorSpace.Bt601;
                        cm.ColorRange = VpxColorRange.Studio;
                        cm.SubsamplingY = cm.SubsamplingX = 1;
                        cm.BitDepth = BitDepth.Bits8;
                        cm.UseHighBitDepth = false;
                    }

                    pbi.RefreshFrameFlags = rb.ReadLiteral(Constants.RefFrames);
                    SetupFrameSize(allocator, ref cm, ref rb);
                    if (pbi.NeedResync != 0)
                    {
                        cm.RefFrameMap.AsSpan().Fill(-1);
                        pbi.NeedResync = 0;
                    }
                }
                else if (pbi.NeedResync != 1)
                {
                    /* Skip if need resync */
                    pbi.RefreshFrameFlags = rb.ReadLiteral(Constants.RefFrames);
                    for (int i = 0; i < Constants.RefsPerFrame; ++i)
                    {
                        int refr = rb.ReadLiteral(Constants.RefFramesLog2);
                        int idx = cm.RefFrameMap[refr];
                        ref RefBuffer refFrame = ref cm.FrameRefs[i];
                        refFrame.Idx = idx;
                        refFrame.Buf = frameBufs[idx].Buf;
                        cm.RefFrameSignBias[Constants.LastFrame + i] = (sbyte)rb.ReadBit();
                    }

                    SetupFrameSizeWithRefs(allocator, ref cm, ref rb);

                    cm.AllowHighPrecisionMv = rb.ReadBit() != 0;
                    cm.InterpFilter = ReadInterpFilter(ref rb);

                    for (int i = 0; i < Constants.RefsPerFrame; ++i)
                    {
                        ref RefBuffer refBuf = ref cm.FrameRefs[i];
                        refBuf.Sf.SetupScaleFactorsForFrame(
                            refBuf.Buf.YCropWidth,
                            refBuf.Buf.YCropHeight,
                            cm.Width,
                            cm.Height);
                    }
                }
            }

            cm.GetFrameNewBuffer().BitDepth = (uint)cm.BitDepth;
            cm.GetFrameNewBuffer().ColorSpace = cm.ColorSpace;
            cm.GetFrameNewBuffer().ColorRange = cm.ColorRange;
            cm.GetFrameNewBuffer().RenderWidth = cm.RenderWidth;
            cm.GetFrameNewBuffer().RenderHeight = cm.RenderHeight;

            if (pbi.NeedResync != 0)
            {
                cm.Error.InternalError(CodecErr.CorruptFrame,
                    "Keyframe / intra-only frame required to reset decoder state");
            }

            if (cm.ErrorResilientMode == 0)
            {
                cm.RefreshFrameContext = rb.ReadBit();
                cm.FrameParallelDecodingMode = rb.ReadBit();
                if (cm.FrameParallelDecodingMode == 0)
                {
                    cm.Counts.Value = new Vp9BackwardUpdates();
                }
            }
            else
            {
                cm.RefreshFrameContext = 0;
                cm.FrameParallelDecodingMode = 1;
            }

            // This flag will be overridden by the call to SetupPastIndependence
            // below, forcing the use of context 0 for those frame types.
            cm.FrameContextIdx = (uint)rb.ReadLiteral(Constants.FrameContextsLog2);

            // Generate next_ref_frame_map.
            for (mask = pbi.RefreshFrameFlags; mask != 0; mask >>= 1)
            {
                if ((mask & 1) != 0)
                {
                    cm.NextRefFrameMap[refIndex] = cm.NewFbIdx;
                    ++frameBufs[cm.NewFbIdx].RefCount;
                }
                else
                {
                    cm.NextRefFrameMap[refIndex] = cm.RefFrameMap[refIndex];
                }

                // Current thread holds the reference frame.
                if (cm.RefFrameMap[refIndex] >= 0)
                {
                    ++frameBufs[cm.RefFrameMap[refIndex]].RefCount;
                }

                ++refIndex;
            }

            for (; refIndex < Constants.RefFrames; ++refIndex)
            {
                cm.NextRefFrameMap[refIndex] = cm.RefFrameMap[refIndex];
                // Current thread holds the reference frame.
                if (cm.RefFrameMap[refIndex] >= 0)
                {
                    ++frameBufs[cm.RefFrameMap[refIndex]].RefCount;
                }
            }

            pbi.HoldRefBuf = 1;

            if (cm.FrameIsIntraOnly() || cm.ErrorResilientMode != 0)
            {
                EntropyMode.SetupPastIndependence(ref cm);
            }

            SetupLoopfilter(ref cm.Lf, ref rb);
            SetupQuantization(ref cm, ref cm.Mb, ref rb);
            cm.Seg.SetupSegmentation(ref cm.Fc.Value, ref rb);
            cm.SetupSegmentationDequant();

            cm.SetupTileInfo(ref rb);
            sz = (ulong)rb.ReadLiteral(16);

            if (sz == 0)
            {
                cm.Error.InternalError(CodecErr.CorruptFrame, "Invalid header size");
            }

            return sz;
        }

        private static bool ReadCompressedHeader(ref Vp9Decoder pbi, ArrayPtr<byte> data, ulong partitionSize)
        {
            ref Vp9Common cm = ref pbi.Common;
            ref MacroBlockD xd = ref cm.Mb;
            ref Vp9EntropyProbs fc = ref cm.Fc.Value;
            Reader r = new();

            if (r.Init(data, (int)partitionSize))
            {
                cm.Error.InternalError(CodecErr.MemError, "Failed to allocate bool decoder 0");
            }

            cm.TxMode = xd.Lossless ? TxMode.Only4X4 : r.ReadTxMode();
            if (cm.TxMode == TxMode.TxModeSelect)
            {
                ReadTxModeProbs(ref fc, ref r);
            }

            ReadCoefProbs(ref fc, cm.TxMode, ref r);

            for (int k = 0; k < Constants.SkipContexts; ++k)
            {
                r.DiffUpdateProb(ref fc.SkipProb[k]);
            }

            if (!cm.FrameIsIntraOnly())
            {
                ReadInterModeProbs(ref fc, ref r);

                if (cm.InterpFilter == Constants.Switchable)
                {
                    ReadSwitchableInterpProbs(ref fc, ref r);
                }

                for (int i = 0; i < Constants.IntraInterContexts; i++)
                {
                    r.DiffUpdateProb( ref fc.IntraInterProb[i]);
                }

                cm.ReferenceMode = cm.ReadFrameReferenceMode(ref r);
                if (cm.ReferenceMode != ReferenceMode.Single)
                {
                    cm.SetupCompoundReferenceMode();
                }

                cm.ReadFrameReferenceModeProbs(ref r);

                for (int j = 0; j < EntropyMode.BlockSizeGroups; j++)
                {
                    for (int i = 0; i < Constants.IntraModes - 1; ++i)
                    {
                        r.DiffUpdateProb( ref fc.YModeProb[j][i]);
                    }
                }

                for (int j = 0; j < Constants.PartitionContexts; ++j)
                {
                    for (int i = 0; i < Constants.PartitionTypes - 1; ++i)
                    {
                        r.DiffUpdateProb( ref fc.PartitionProb[j][i]);
                    }
                }

                ReadMvProbs(ref fc, cm.AllowHighPrecisionMv, ref r);
            }

            return r.HasError();
        }

        private static ref ReadBitBuffer InitReadBitBuffer(ref ReadBitBuffer rb, ReadOnlySpan<byte> data)
        {
            rb.BitOffset = 0;
            rb.BitBuffer = data;
            return ref rb;
        }

        public static unsafe void Decode(MemoryAllocator allocator,
            ref Vp9Decoder pbi,
            ArrayPtr<byte> data,
            out ArrayPtr<byte> pDataEnd,
            bool multithreaded = true)
        {
            ref Vp9Common cm = ref pbi.Common;
            ref MacroBlockD xd = ref cm.Mb;
            ReadBitBuffer rb = new();
            int contextUpdated = 0;
            Span<byte> clearData = stackalloc byte[80];
            ulong firstPartitionSize =
                ReadUncompressedHeader(allocator, ref pbi, ref InitReadBitBuffer(ref rb, data.AsSpan()));
            int tileRows = 1 << cm.Log2TileRows;
            int tileCols = 1 << cm.Log2TileCols;
            ref Surface newFb = ref cm.GetFrameNewBuffer();
            xd.CurBuf = newFb;

            if (firstPartitionSize == 0)
            {
                // showing a frame directly
                pDataEnd = data.Slice(cm.Profile <= BitstreamProfile.Profile2 ? 1 : 2);
                return;
            }

            data = data.Slice((int)rb.BytesRead());
            if (!ReadIsValid(data, (int)firstPartitionSize))
            {
                cm.Error.InternalError(CodecErr.CorruptFrame, "Truncated packet or corrupt header length");
            }

            cm.UsePrevFrameMvs =
                cm.ErrorResilientMode == 0 &&
                cm.Width == cm.LastWidth &&
                cm.Height == cm.LastHeight &&
                !cm.LastIntraOnly &&
                cm.LastShowFrame != 0 &&
                cm.LastFrameType != FrameType.KeyFrame;

            xd.SetupBlockPlanes(cm.SubsamplingX, cm.SubsamplingY);

            cm.Fc = new Ptr<Vp9EntropyProbs>(ref cm.FrameContexts[(int)cm.FrameContextIdx]);

            xd.Corrupted = false;
            newFb.Corrupted = ReadCompressedHeader(ref pbi, data, firstPartitionSize) ? 1 : 0;
            if (newFb.Corrupted != 0)
            {
                cm.Error.InternalError(CodecErr.CorruptFrame, "Decode failed. Frame data header is corrupted.");
            }

            if (cm.Lf.FilterLevel != 0 && cm.SkipLoopFilter == 0)
            {
                LoopFilter.LoopFilterFrameInit(ref cm, cm.Lf.FilterLevel);
            }

            int threadCount = multithreaded ? Math.Max(1, Environment.ProcessorCount / 2) : 0;

            if (cm.TileWorkerData.IsNull || tileCols * tileRows != cm.TotalTiles)
            {
                int numTileWorkers = (tileCols * tileRows) + threadCount;
                if (!cm.TileWorkerData.IsNull)
                {
                    allocator.Free(cm.TileWorkerData);
                }

                cm.CheckMemError( ref cm.TileWorkerData, allocator.Allocate<TileWorkerData>(numTileWorkers));
                cm.TotalTiles = tileRows * tileCols;
            }

            if (multithreaded)
            {
                pDataEnd = DecodeTilesMt(ref pbi.Common, data.Slice((int)firstPartitionSize), threadCount);

                LoopFilter.LoopFilterFrameMt(
                    ref cm.Mb.CurBuf,
                    ref cm,
                    ref cm.Mb,
                    cm.Lf.FilterLevel,
                    false,
                    false,
                    threadCount);
            }
            else
            {
                pDataEnd = DecodeTiles(ref pbi.Common, data.Slice((int)firstPartitionSize));

                LoopFilter.LoopFilterFrame(ref cm.Mb.CurBuf, ref cm, ref cm.Mb, cm.Lf.FilterLevel, false, false);
            }

            if (!xd.Corrupted)
            {
                if (cm.ErrorResilientMode == 0 && cm.FrameParallelDecodingMode == 0)
                {
                    cm.AdaptCoefProbs();

                    if (!cm.FrameIsIntraOnly())
                    {
                        cm.AdaptModeProbs();
                        cm.AdaptMvProbs(cm.AllowHighPrecisionMv);
                    }
                }
            }
            else
            {
                cm.Error.InternalError(CodecErr.CorruptFrame, "Decode failed. Frame data is corrupted.");
            }

            // Non frame parallel update frame context here.
            if (cm.RefreshFrameContext != 0 && contextUpdated == 0)
            {
                cm.FrameContexts[(int)cm.FrameContextIdx] = cm.Fc.Value;
            }
        }
    }
}