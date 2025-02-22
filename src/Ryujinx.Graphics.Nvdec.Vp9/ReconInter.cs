using Ryujinx.Common.Memory;
using Ryujinx.Graphics.Nvdec.Vp9.Types;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static Ryujinx.Graphics.Nvdec.Vp9.Dsp.Filter;

namespace Ryujinx.Graphics.Nvdec.Vp9
{
    internal static class ReconInter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void InterPredictor(
            byte* src,
            int srcStride,
            byte* dst,
            int dstStride,
            int subpelX,
            int subpelY,
            ref ScaleFactors sf,
            int w,
            int h,
            int refr,
            Array8<short>[] kernel,
            int xs,
            int ys)
        {
            sf.InterPredict(
                subpelX != 0 ? 1 : 0,
                subpelY != 0 ? 1 : 0,
                refr,
                src,
                srcStride,
                dst,
                dstStride,
                subpelX,
                subpelY,
                w,
                h,
                kernel,
                xs,
                ys);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void HighbdInterPredictor(
            ushort* src,
            int srcStride,
            ushort* dst,
            int dstStride,
            int subpelX,
            int subpelY,
            ref ScaleFactors sf,
            int w,
            int h,
            int refr,
            Array8<short>[] kernel,
            int xs,
            int ys,
            int bd)
        {
            sf.HighbdInterPredict(
                subpelX != 0 ? 1 : 0,
                subpelY != 0 ? 1 : 0,
                refr,
                src,
                srcStride,
                dst,
                dstStride,
                subpelX,
                subpelY,
                w,
                h,
                kernel,
                xs,
                ys,
                bd);
        }

        public static int RoundMvCompQ4(int value)
        {
            return (value < 0 ? value - 2 : value + 2) / 4;
        }

        public static int RoundMvCompQ2(int value)
        {
            return (value < 0 ? value - 1 : value + 1) / 2;
        }

        public static Mv ClampMvToUmvBorderSb(ref MacroBlockD xd, ref Mv srcMv, int bw, int bh, int ssX, int ssY)
        {
            // If the MV points so far into the UMV border that no visible pixels
            // are used for reconstruction, the subpel part of the MV can be
            // discarded and the MV limited to 16 pixels with equivalent results.
            int spelLeft = (Constants.InterpExtend + bw) << SubpelBits;
            int spelRight = spelLeft - SubpelShifts;
            int spelTop = (Constants.InterpExtend + bh) << SubpelBits;
            int spelBottom = spelTop - SubpelShifts;
            Mv clampedMv = new()
            {
                Row = (short)(srcMv.Row * (1 << (1 - ssY))), Col = (short)(srcMv.Col * (1 << (1 - ssX)))
            };

            Debug.Assert(ssX <= 1);
            Debug.Assert(ssY <= 1);

            clampedMv.Clamp(
                (xd.MbToLeftEdge * (1 << (1 - ssX))) - spelLeft,
                (xd.MbToRightEdge * (1 << (1 - ssX))) + spelRight,
                (xd.MbToTopEdge * (1 << (1 - ssY))) - spelTop,
                (xd.MbToBottomEdge * (1 << (1 - ssY))) + spelBottom);

            return clampedMv;
        }

        public static Mv AverageSplitMvs(ref MacroBlockDPlane pd, ref ModeInfo mi, int refr, int block)
        {
            int ssIdx = ((pd.SubsamplingX > 0 ? 1 : 0) << 1) | (pd.SubsamplingY > 0 ? 1 : 0);
            Mv res = new();
            switch (ssIdx)
            {
                case 0:
                    res = mi.Bmi[block].Mv[refr];
                    break;
                case 1:
                    res = mi.MvPredQ2(refr, block, block + 2);
                    break;
                case 2:
                    res = mi.MvPredQ2(refr, block, block + 1);
                    break;
                case 3:
                    res = mi.MvPredQ4(refr);
                    break;
                default:
                    Debug.Assert(ssIdx <= 3 && ssIdx >= 0);
                    break;
            }

            return res;
        }

        private static int ScaledBufferOffset(int xOffset, int yOffset, int stride, Ptr<ScaleFactors> sf)
        {
            int x = !sf.IsNull ? sf.Value.ScaleValueX(xOffset) : xOffset;
            int y = !sf.IsNull ? sf.Value.ScaleValueY(yOffset) : yOffset;
            return (y * stride) + x;
        }

        private static void SetupPredPlanes(
            ref Buf2D dst,
            ArrayPtr<byte> src,
            int stride,
            int miRow,
            int miCol,
            Ptr<ScaleFactors> scale,
            int subsamplingX,
            int subsamplingY)
        {
            int x = (Constants.MiSize * miCol) >> subsamplingX;
            int y = (Constants.MiSize * miRow) >> subsamplingY;
            dst.Buf = src.Slice(ScaledBufferOffset(x, y, stride, scale));
            dst.Stride = stride;
        }

        public static void SetupDstPlanes(
            ref Array3<MacroBlockDPlane> planes,
            ref Surface src,
            int miRow,
            int miCol)
        {
            Span<ArrayPtr<byte>> buffers = stackalloc ArrayPtr<byte>[Constants.MaxMbPlane];
            buffers[0] = src.YBuffer;
            buffers[1] = src.UBuffer;
            buffers[2] = src.VBuffer;
            Span<int> strides = stackalloc int[Constants.MaxMbPlane];
            strides[0] = src.Stride;
            strides[1] = src.UvStride;
            strides[2] = src.UvStride;

            for (int i = 0; i < Constants.MaxMbPlane; ++i)
            {
                ref MacroBlockDPlane pd = ref planes[i];
                SetupPredPlanes(ref pd.Dst, buffers[i], strides[i], miRow, miCol, Ptr<ScaleFactors>.Null,
                    pd.SubsamplingX, pd.SubsamplingY);
            }
        }

        public static void SetupPrePlanes(
            ref MacroBlockD xd,
            int idx,
            ref Surface src,
            int miRow,
            int miCol,
            Ptr<ScaleFactors> sf)
        {
            if (!src.YBuffer.IsNull && !src.UBuffer.IsNull && !src.VBuffer.IsNull)
            {
                Span<ArrayPtr<byte>> buffers = stackalloc ArrayPtr<byte>[Constants.MaxMbPlane];
                buffers[0] = src.YBuffer;
                buffers[1] = src.UBuffer;
                buffers[2] = src.VBuffer;
                Span<int> strides = stackalloc int[Constants.MaxMbPlane];
                strides[0] = src.Stride;
                strides[1] = src.UvStride;
                strides[2] = src.UvStride;

                for (int i = 0; i < Constants.MaxMbPlane; ++i)
                {
                    ref MacroBlockDPlane pd = ref xd.Plane[i];
                    SetupPredPlanes(ref pd.Pre[idx], buffers[i], strides[i], miRow, miCol, sf, pd.SubsamplingX,
                        pd.SubsamplingY);
                }
            }
        }
    }
}