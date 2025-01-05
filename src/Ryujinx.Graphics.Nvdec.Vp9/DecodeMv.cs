using Ryujinx.Common.Memory;
using Ryujinx.Graphics.Nvdec.Vp9.Dsp;
using Ryujinx.Graphics.Nvdec.Vp9.Types;
using Ryujinx.Graphics.Video;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ryujinx.Graphics.Nvdec.Vp9
{
    internal static class DecodeMv
    {
        private const int MvrefNeighbours = 8;

        private static PredictionMode ReadIntraMode(ref Reader r, ReadOnlySpan<byte> p)
        {
            return (PredictionMode)r.ReadTree(Luts.Vp9IntraModeTree, p);
        }

        private static PredictionMode ReadIntraModeY(ref Vp9Common cm, ref MacroBlockD xd, ref Reader r, int sizeGroup)
        {
            PredictionMode yMode = ReadIntraMode(ref r, cm.Fc.Value.YModeProb[sizeGroup].AsSpan());
            if (!xd.Counts.IsNull)
            {
                ++xd.Counts.Value.YMode[sizeGroup][(int)yMode];
            }

            return yMode;
        }

        private static PredictionMode ReadIntraModeUv(ref Vp9Common cm, ref MacroBlockD xd, ref Reader r, byte yMode)
        {
            PredictionMode uvMode = ReadIntraMode(ref r, cm.Fc.Value.UvModeProb[yMode].AsSpan());
            if (!xd.Counts.IsNull)
            {
                ++xd.Counts.Value.UvMode[yMode][(int)uvMode];
            }

            return uvMode;
        }

        private static PredictionMode ReadInterMode(ref Vp9Common cm, ref MacroBlockD xd, ref Reader r, int ctx)
        {
            int mode = r.ReadTree(Luts.Vp9InterModeTree, cm.Fc.Value.InterModeProb[ctx].AsSpan());
            if (!xd.Counts.IsNull)
            {
                ++xd.Counts.Value.InterMode[ctx][mode];
            }

            return PredictionMode.NearestMv + mode;
        }

        private static int ReadSegmentId(ref Reader r, ref Array7<byte> segTreeProbs)
        {
            return r.ReadTree(Luts.Vp9SegmentTree, segTreeProbs.AsSpan());
        }

        private static ReadOnlySpan<byte> GetTxProbs(ref Vp9EntropyProbs fc, TxSize maxTxSize, int ctx)
        {
            switch (maxTxSize)
            {
                case TxSize.Tx8x8:
                    return fc.Tx8x8Prob[ctx].AsSpan();
                case TxSize.Tx16x16:
                    return fc.Tx16x16Prob[ctx].AsSpan();
                case TxSize.Tx32x32:
                    return fc.Tx32x32Prob[ctx].AsSpan();
                default:
                    Debug.Assert(false, "Invalid maxTxSize.");

                    return [];
            }
        }

        private static Span<uint> GetTxCounts(ref Vp9BackwardUpdates counts, TxSize maxTxSize, int ctx)
        {
            switch (maxTxSize)
            {
                case TxSize.Tx8x8:
                    return counts.Tx8x8[ctx].AsSpan();
                case TxSize.Tx16x16:
                    return counts.Tx16x16[ctx].AsSpan();
                case TxSize.Tx32x32:
                    return counts.Tx32x32[ctx].AsSpan();
                default:
                    Debug.Assert(false, "Invalid maxTxSize.");

                    return [];
            }
        }

        private static TxSize ReadSelectedTxSize(ref Vp9Common cm, ref MacroBlockD xd, TxSize maxTxSize, ref Reader r)
        {
            int ctx = xd.GetTxSizeContext();
            ReadOnlySpan<byte> txProbs = GetTxProbs(ref cm.Fc.Value, maxTxSize, ctx);
            TxSize txSize = (TxSize)r.Read(txProbs[0]);
            if (txSize != TxSize.Tx4x4 && maxTxSize >= TxSize.Tx16x16)
            {
                txSize += r.Read(txProbs[1]);
                if (txSize != TxSize.Tx8x8 && maxTxSize >= TxSize.Tx32x32)
                {
                    txSize += r.Read(txProbs[2]);
                }
            }

            if (!xd.Counts.IsNull)
            {
                ++GetTxCounts(ref xd.Counts.Value, maxTxSize, ctx)[(int)txSize];
            }

            return txSize;
        }

        private static TxSize ReadTxSize(ref Vp9Common cm, ref MacroBlockD xd, bool allowSelect, ref Reader r)
        {
            TxMode txMode = cm.TxMode;
            BlockSize bsize = xd.Mi[0].Value.SbType;
            TxSize maxTxSize = Luts.MaxTxSizeLookup[(int)bsize];
            if (allowSelect && txMode == TxMode.TxModeSelect && bsize >= BlockSize.Block8x8)
            {
                return ReadSelectedTxSize(ref cm, ref xd, maxTxSize, ref r);
            }

            return (TxSize)Math.Min((int)maxTxSize, (int)Luts.TxModeToBiggestTxSize[(int)txMode]);
        }

        private static int DecGetSegmentId(ref Vp9Common cm, ArrayPtr<byte> segmentIds, int miOffset, int xMis, int yMis)
        {
            int x, y, segmentId = int.MaxValue;

            for (y = 0; y < yMis; y++)
            {
                for (x = 0; x < xMis; x++)
                {
                    segmentId = Math.Min(segmentId, segmentIds[miOffset + y * cm.MiCols + x]);
                }
            }

            Debug.Assert(segmentId >= 0 && segmentId < Constants.MaxSegments);

            return segmentId;
        }

        private static void SetSegmentId(ref Vp9Common cm, int miOffset, int xMis, int yMis, int segmentId)
        {
            int x, y;

            Debug.Assert(segmentId >= 0 && segmentId < Constants.MaxSegments);

            for (y = 0; y < yMis; y++)
            {
                for (x = 0; x < xMis; x++)
                {
                    cm.CurrentFrameSegMap[miOffset + y * cm.MiCols + x] = (byte)segmentId;
                }
            }
        }

        private static void CopySegmentId(
            ref Vp9Common cm,
            ArrayPtr<byte> lastSegmentIds,
            ArrayPtr<byte> currentSegmentIds,
            int miOffset,
            int xMis,
            int yMis)
        {
            int x, y;

            for (y = 0; y < yMis; y++)
            {
                for (x = 0; x < xMis; x++)
                {
                    currentSegmentIds[miOffset + y * cm.MiCols + x] = (byte)(!lastSegmentIds.IsNull ? lastSegmentIds[miOffset + y * cm.MiCols + x] : 0);
                }
            }
        }

        private static int ReadIntraSegmentId(ref Vp9Common cm, int miOffset, int xMis, int yMis, ref Reader r)
        {
            ref Segmentation seg = ref cm.Seg;
            int segmentId;

            if (!seg.Enabled)
            {
                return 0; // Default for disabled segmentation
            }

            if (!seg.UpdateMap)
            {
                CopySegmentId(ref cm, cm.LastFrameSegMap, cm.CurrentFrameSegMap, miOffset, xMis, yMis);

                return 0;
            }

            segmentId = ReadSegmentId(ref r, ref cm.Fc.Value.SegTreeProb);
            SetSegmentId(ref cm, miOffset, xMis, yMis, segmentId);

            return segmentId;
        }

        private static int ReadInterSegmentId(
            ref Vp9Common cm,
            ref MacroBlockD xd,
            int miRow,
            int miCol,
            ref Reader r,
            int xMis,
            int yMis)
        {
            ref Segmentation seg = ref cm.Seg;
            ref ModeInfo mi = ref xd.Mi[0].Value;
            int predictedSegmentId, segmentId;
            int miOffset = miRow * cm.MiCols + miCol;

            if (!seg.Enabled)
            {
                return 0; // Default for disabled segmentation
            }

            predictedSegmentId = !cm.LastFrameSegMap.IsNull
                ? DecGetSegmentId(ref cm, cm.LastFrameSegMap, miOffset, xMis, yMis)
                : 0;

            if (!seg.UpdateMap)
            {
                CopySegmentId(ref cm, cm.LastFrameSegMap, cm.CurrentFrameSegMap, miOffset, xMis, yMis);

                return predictedSegmentId;
            }

            if (seg.TemporalUpdate)
            {
                byte predProb = Segmentation.GetPredProbSegId(ref cm.Fc.Value.SegPredProb, ref xd);
                mi.SegIdPredicted = (sbyte)r.Read(predProb);
                segmentId = mi.SegIdPredicted != 0 ? predictedSegmentId : ReadSegmentId(ref r, ref cm.Fc.Value.SegTreeProb);
            }
            else
            {
                segmentId = ReadSegmentId(ref r, ref cm.Fc.Value.SegTreeProb);
            }
            SetSegmentId(ref cm, miOffset, xMis, yMis, segmentId);

            return segmentId;
        }

        private static int ReadSkip(ref Vp9Common cm, ref MacroBlockD xd, int segmentId, ref Reader r)
        {
            if (cm.Seg.IsSegFeatureActive(segmentId, SegLvlFeatures.SegLvlSkip) != 0)
            {
                return 1;
            }

            int ctx = xd.GetSkipContext();
            int skip = r.Read(cm.Fc.Value.SkipProb[ctx]);
            if (!xd.Counts.IsNull)
            {
                ++xd.Counts.Value.Skip[ctx][skip];
            }

            return skip;
        }

        private static int ReadMvComponent(ref Reader r, ref Vp9EntropyProbs fc, int mvcomp, bool usehp)
        {
            int mag, d, fr, hp;
            bool sign = r.Read(fc.Sign[mvcomp]) != 0;
            MvClassType mvClass = (MvClassType)r.ReadTree(Luts.Vp9MvClassTree, fc.Classes[mvcomp].AsSpan());
            bool class0 = mvClass == MvClassType.MvClass0;

            // Integer part
            if (class0)
            {
                d = r.Read(fc.Class0[mvcomp][0]);
                mag = 0;
            }
            else
            {
                int i;
                int n = (int)mvClass + Constants.Class0Bits - 1; // Number of bits

                d = 0;
                for (i = 0; i < n; ++i)
                {
                    d |= r.Read(fc.Bits[mvcomp][i]) << i;
                }

                mag = Constants.Class0Size << ((int)mvClass + 2);
            }

            // Fractional part
            fr = r.ReadTree(Luts.Vp9MvFPTree, class0 ? fc.Class0Fp[mvcomp][d].AsSpan() : fc.Fp[mvcomp].AsSpan());

            // High precision part (if hp is not used, the default value of the hp is 1)
            hp = usehp ? r.Read(class0 ? fc.Class0Hp[mvcomp] : fc.Hp[mvcomp]) : 1;

            // Result
            mag += ((d << 3) | (fr << 1) | hp) + 1;

            return sign ? -mag : mag;
        }

        private static void ReadMv(
            ref Reader r,
            ref Mv mv,
            ref Mv refr,
            ref Vp9EntropyProbs fc,
            Ptr<Vp9BackwardUpdates> counts,
            bool allowHP)
        {
            MvJointType jointType = (MvJointType)r.ReadTree(Luts.Vp9MvJointTree, fc.Joints.AsSpan());
            bool useHP = allowHP && refr.UseMvHp();
            Mv diff = new();

            if (Mv.MvJointVertical(jointType))
            {
                diff.Row = (short)ReadMvComponent(ref r, ref fc, 0, useHP);
            }

            if (Mv.MvJointHorizontal(jointType))
            {
                diff.Col = (short)ReadMvComponent(ref r, ref fc, 1, useHP);
            }

            diff.IncMv(counts);

            mv.Row = (short)(refr.Row + diff.Row);
            mv.Col = (short)(refr.Col + diff.Col);
        }

        private static ReferenceMode ReadBlockReferenceMode(ref Vp9Common cm, ref MacroBlockD xd, ref Reader r)
        {
            if (cm.ReferenceMode == ReferenceMode.ReferenceModeSelect)
            {
                int ctx = PredCommon.GetReferenceModeContext(ref cm, ref xd);
                ReferenceMode mode = (ReferenceMode)r.Read(cm.Fc.Value.CompInterProb[ctx]);
                if (!xd.Counts.IsNull)
                {
                    ++xd.Counts.Value.CompInter[ctx][(int)mode];
                }

                return mode; // SingleReference or CompoundReference
            }

            return cm.ReferenceMode;
        }

        // Read the referncence frame
        private static void ReadRefFrames(
            ref Vp9Common cm,
            ref MacroBlockD xd,
            ref Reader r,
            int segmentId,
            ref Array2<sbyte> refFrame)
        {
            ref Vp9EntropyProbs fc = ref cm.Fc.Value;

            if (cm.Seg.IsSegFeatureActive(segmentId, SegLvlFeatures.SegLvlRefFrame) != 0)
            {
                refFrame[0] = (sbyte)cm.Seg.GetSegData(segmentId, SegLvlFeatures.SegLvlRefFrame);
                refFrame[1] = Constants.None;
            }
            else
            {
                ReferenceMode mode = ReadBlockReferenceMode(ref cm, ref xd, ref r);
                if (mode == ReferenceMode.CompoundReference)
                {
                    int idx = cm.RefFrameSignBias[cm.CompFixedRef];
                    int ctx = PredCommon.GetPredContextCompRefP(ref cm, ref xd);
                    int bit = r.Read(fc.CompRefProb[ctx]);
                    if (!xd.Counts.IsNull)
                    {
                        ++xd.Counts.Value.CompRef[ctx][bit];
                    }

                    refFrame[idx] = cm.CompFixedRef;
                    refFrame[idx == 0 ? 1 : 0] = cm.CompVarRef[bit];
                }
                else if (mode == ReferenceMode.SingleReference)
                {
                    int ctx0 = PredCommon.GetPredContextSingleRefP1(ref xd);
                    int bit0 = r.Read(fc.SingleRefProb[ctx0][0]);
                    if (!xd.Counts.IsNull)
                    {
                        ++xd.Counts.Value.SingleRef[ctx0][0][bit0];
                    }

                    if (bit0 != 0)
                    {
                        int ctx1 = PredCommon.GetPredContextSingleRefP2(ref xd);
                        int bit1 = r.Read(fc.SingleRefProb[ctx1][1]);
                        if (!xd.Counts.IsNull)
                        {
                            ++xd.Counts.Value.SingleRef[ctx1][1][bit1];
                        }

                        refFrame[0] = (sbyte)(bit1 != 0 ? Constants.AltRefFrame : Constants.GoldenFrame);
                    }
                    else
                    {
                        refFrame[0] = Constants.LastFrame;
                    }

                    refFrame[1] = Constants.None;
                }
                else
                {
                    Debug.Assert(false, "Invalid prediction mode.");
                }
            }
        }

        private static byte ReadSwitchableInterpFilter(ref Vp9Common cm, ref MacroBlockD xd, ref Reader r)
        {
            int ctx = xd.GetPredContextSwitchableInterp();
            byte type = (byte)r.ReadTree(Luts.Vp9SwitchableInterpTree, cm.Fc.Value.SwitchableInterpProb[ctx].AsSpan());
            if (!xd.Counts.IsNull)
            {
                ++xd.Counts.Value.SwitchableInterp[ctx][type];
            }

            return type;
        }

        private static void ReadIntraBlockModeInfo(ref Vp9Common cm, ref MacroBlockD xd, ref ModeInfo mi, ref Reader r)
        {
            BlockSize bsize = mi.SbType;
            int i;

            switch (bsize)
            {
                case BlockSize.Block4x4:
                    for (i = 0; i < 4; ++i)
                    {
                        mi.Bmi[i].Mode = ReadIntraModeY(ref cm, ref xd, ref r, 0);
                    }

                    mi.Mode = mi.Bmi[3].Mode;
                    break;
                case BlockSize.Block4x8:
                    mi.Bmi[0].Mode = mi.Bmi[2].Mode = ReadIntraModeY(ref cm, ref xd, ref r, 0);
                    mi.Bmi[1].Mode = mi.Bmi[3].Mode = mi.Mode = ReadIntraModeY(ref cm, ref xd, ref r, 0);
                    break;
                case BlockSize.Block8x4:
                    mi.Bmi[0].Mode = mi.Bmi[1].Mode = ReadIntraModeY(ref cm, ref xd, ref r, 0);
                    mi.Bmi[2].Mode = mi.Bmi[3].Mode = mi.Mode = ReadIntraModeY(ref cm, ref xd, ref r, 0);
                    break;
                default:
                    mi.Mode = ReadIntraModeY(ref cm, ref xd, ref r, Luts.SizeGroupLookup[(int)bsize]);
                    break;
            }

            mi.UvMode = ReadIntraModeUv(ref cm, ref xd, ref r, (byte)mi.Mode);

            // Initialize interp_filter here so we do not have to check for inter block
            // modes in GetPredContextSwitchableInterp()
            mi.InterpFilter = Constants.SwitchableFilters;

            mi.RefFrame[0] = Constants.IntraFrame;
            mi.RefFrame[1] = Constants.None;
        }

        private static bool IsMvValid(ref Mv mv)
        {
            return mv.Row > Constants.MvLow &&
                   mv.Row < Constants.MvUpp &&
                   mv.Col > Constants.MvLow &&
                   mv.Col < Constants.MvUpp;
        }

        private static void CopyMvPair(ref Array2<Mv> dst, ref Array2<Mv> src)
        {
            dst[0] = src[0];
            dst[1] = src[1];
        }

        private static void ZeroMvPair(ref Array2<Mv> dst)
        {
            dst[0] = new Mv();
            dst[1] = new Mv();
        }

        private static bool AssignMv(
            ref Vp9Common cm,
            ref MacroBlockD xd,
            PredictionMode mode,
            ref Array2<Mv> mv,
            ref Array2<Mv> refMv,
            ref Array2<Mv> nearNearestMv,
            int isCompound,
            bool allowHP,
            ref Reader r)
        {
            int i;
            bool ret = true;

            switch (mode)
            {
                case PredictionMode.NewMv:
                    {
                        for (i = 0; i < 1 + isCompound; ++i)
                        {
                            ReadMv(ref r, ref mv[i], ref refMv[i], ref cm.Fc.Value, xd.Counts, allowHP);
                            ret = ret && IsMvValid(ref mv[i]);
                        }
                        break;
                    }
                case PredictionMode.NearMv:
                case PredictionMode.NearestMv:
                    {
                        CopyMvPair(ref mv, ref nearNearestMv);
                        break;
                    }
                case PredictionMode.ZeroMv:
                    {
                        ZeroMvPair(ref mv);
                        break;
                    }
                default:
                    return false;
            }
            return ret;
        }

        private static bool ReadIsInterBlock(ref Vp9Common cm, ref MacroBlockD xd, int segmentId, ref Reader r)
        {
            if (cm.Seg.IsSegFeatureActive(segmentId, SegLvlFeatures.SegLvlRefFrame) != 0)
            {
                return cm.Seg.GetSegData(segmentId, SegLvlFeatures.SegLvlRefFrame) != Constants.IntraFrame;
            }

            int ctx = xd.GetIntraInterContext();
            bool isInter = r.Read(cm.Fc.Value.IntraInterProb[ctx]) != 0;
            if (!xd.Counts.IsNull)
            {
                ++xd.Counts.Value.IntraInter[ctx][isInter ? 1 : 0];
            }

            return isInter;
        }

        private static void DecFindBestRefMvs(bool allowHP, Span<Mv> mvlist, ref Mv bestMv, int refmvCount)
        {
            int i;

            // Make sure all the candidates are properly clamped etc
            for (i = 0; i < refmvCount; ++i)
            {
                mvlist[i].LowerMvPrecision(allowHP);
                bestMv = mvlist[i];
            }
        }

        private static bool AddMvRefListEb(Mv mv, ref int refMvCount, Span<Mv> mvRefList, bool earlyBreak)
        {
            if (refMvCount != 0)
            {
                if (Unsafe.As<Mv, int>(ref mv) != Unsafe.As<Mv, int>(ref mvRefList[0]))
                {
                    mvRefList[refMvCount] = mv;
                    refMvCount++;

                    return true;
                }
            }
            else
            {
                mvRefList[refMvCount++] = mv;
                if (earlyBreak)
                {
                    return true;
                }
            }

            return false;
        }

        // Performs mv sign inversion if indicated by the reference frame combination.
        private static Mv ScaleMv(ref ModeInfo mi, int refr, sbyte thisRefFrame, ref Array4<sbyte> refSignBias)
        {
            Mv mv = mi.Mv[refr];
            if (refSignBias[mi.RefFrame[refr]] != refSignBias[thisRefFrame])
            {
                mv.Row *= -1;
                mv.Col *= -1;
            }
            return mv;
        }

        private static bool IsDiffRefFrameAddMvEb(
            ref ModeInfo mbmi,
            sbyte refFrame,
            ref Array4<sbyte> refSignBias,
            ref int refmvCount,
            Span<Mv> mvRefList,
            bool earlyBreak)
        {
            if (mbmi.IsInterBlock())
            {
                if (mbmi.RefFrame[0] != refFrame)
                {
                    if (AddMvRefListEb(ScaleMv(ref mbmi, 0, refFrame, ref refSignBias), ref refmvCount, mvRefList, earlyBreak))
                    {
                        return true;
                    }
                }
                if (mbmi.HasSecondRef() && mbmi.RefFrame[1] != refFrame && Unsafe.As<Mv, int>(ref mbmi.Mv[1]) != Unsafe.As<Mv, int>(ref mbmi.Mv[0]))
                {
                    if (AddMvRefListEb(ScaleMv(ref mbmi, 1, refFrame, ref refSignBias), ref refmvCount, mvRefList, earlyBreak))
                    {
                        return true;
                    }
                }

            }
            return false;
        }

        // This function searches the neighborhood of a given MB/SB
        // to try and find candidate reference vectors.
        private static int DecFindMvRefs(
            ref Vp9Common cm,
            ref MacroBlockD xd,
            PredictionMode mode,
            sbyte refFrame,
            Span<Position> mvRefSearch,
            Span<Mv> mvRefList,
            int miRow,
            int miCol,
            int block,
            int isSub8X8)
        {
            ref Array4<sbyte> refSignBias = ref cm.RefFrameSignBias;
            int i, refmvCount = 0;
            bool differentRefFound = false;
            Ptr<MvRef> prevFrameMvs = cm.UsePrevFrameMvs ? new Ptr<MvRef>(ref cm.PrevFrameMvs[miRow * cm.MiCols + miCol]) : Ptr<MvRef>.Null;
            ref TileInfo tile = ref xd.Tile;
            // If mode is nearestmv or newmv (uses nearestmv as a reference) then stop
            // searching after the first mv is found.
            bool earlyBreak = mode != PredictionMode.NearMv;

            // Blank the reference vector list
            mvRefList[..Constants.MaxMvRefCandidates].Clear();

            i = 0;
            if (isSub8X8 != 0)
            {
                // If the size < 8x8 we get the mv from the bmi substructure for the
                // nearest two blocks.
                for (i = 0; i < 2; ++i)
                {
                    ref Position mvRef = ref mvRefSearch[i];
                    if (tile.IsInside(miCol, miRow, cm.MiRows, ref mvRef))
                    {
                        ref ModeInfo candidateMi = ref xd.Mi[mvRef.Col + mvRef.Row * xd.MiStride].Value;
                        differentRefFound = true;

                        if (candidateMi.RefFrame[0] == refFrame)
                        {
                            if (AddMvRefListEb(candidateMi.GetSubBlockMv(0, mvRef.Col, block), ref refmvCount, mvRefList, earlyBreak))
                            {
                                goto Done;
                            }
                        }
                        else if (candidateMi.RefFrame[1] == refFrame)
                        {
                            if (AddMvRefListEb(candidateMi.GetSubBlockMv(1, mvRef.Col, block), ref refmvCount, mvRefList, earlyBreak))
                            {
                                goto Done;
                            }
                        }
                    }
                }
            }

            // Check the rest of the neighbors in much the same way
            // as before except we don't need to keep track of sub blocks or
            // mode counts.
            for (; i < MvrefNeighbours; ++i)
            {
                ref Position mvRef = ref mvRefSearch[i];
                if (tile.IsInside(miCol, miRow, cm.MiRows, ref mvRef))
                {
                    ref ModeInfo candidate = ref xd.Mi[mvRef.Col + mvRef.Row * xd.MiStride].Value;
                    differentRefFound = true;

                    if (candidate.RefFrame[0] == refFrame)
                    {
                        if (AddMvRefListEb(candidate.Mv[0], ref refmvCount, mvRefList, earlyBreak))
                        {
                            goto Done;
                        }
                    }
                    else if (candidate.RefFrame[1] == refFrame)
                    {
                        if (AddMvRefListEb(candidate.Mv[1], ref refmvCount, mvRefList, earlyBreak))
                        {
                            goto Done;
                        }
                    }
                }
            }

            // Check the last frame's mode and mv info.
            if (!prevFrameMvs.IsNull)
            {
                if (prevFrameMvs.Value.RefFrame[0] == refFrame)
                {
                    if (AddMvRefListEb(prevFrameMvs.Value.Mv[0], ref refmvCount, mvRefList, earlyBreak))
                    {
                        goto Done;
                    }
                }
                else if (prevFrameMvs.Value.RefFrame[1] == refFrame)
                {
                    if (AddMvRefListEb(prevFrameMvs.Value.Mv[1], ref refmvCount, mvRefList, earlyBreak))
                    {
                        goto Done;
                    }
                }
            }

            // Since we couldn't find 2 mvs from the same reference frame
            // go back through the neighbors and find motion vectors from
            // different reference frames.
            if (differentRefFound)
            {
                for (i = 0; i < MvrefNeighbours; ++i)
                {
                    ref Position mvRef = ref mvRefSearch[i];
                    if (tile.IsInside(miCol, miRow, cm.MiRows, ref mvRef))
                    {
                        ref ModeInfo candidate = ref xd.Mi[mvRef.Col + mvRef.Row * xd.MiStride].Value;

                        // If the candidate is Intra we don't want to consider its mv.
                        if (IsDiffRefFrameAddMvEb(ref candidate, refFrame, ref refSignBias, ref refmvCount, mvRefList, earlyBreak))
                        {
                            goto Done;
                        }
                    }
                }
            }

            // Since we still don't have a candidate we'll try the last frame.
            if (!prevFrameMvs.IsNull)
            {
                if (prevFrameMvs.Value.RefFrame[0] != refFrame && prevFrameMvs.Value.RefFrame[0] > Constants.IntraFrame)
                {
                    Mv mv = prevFrameMvs.Value.Mv[0];
                    if (refSignBias[prevFrameMvs.Value.RefFrame[0]] != refSignBias[refFrame])
                    {
                        mv.Row *= -1;
                        mv.Col *= -1;
                    }
                    if (AddMvRefListEb(mv, ref refmvCount, mvRefList, earlyBreak))
                    {
                        goto Done;
                    }
                }

                if (prevFrameMvs.Value.RefFrame[1] > Constants.IntraFrame &&
                    prevFrameMvs.Value.RefFrame[1] != refFrame &&
                    Unsafe.As<Mv, int>(ref prevFrameMvs.Value.Mv[1]) != Unsafe.As<Mv, int>(ref prevFrameMvs.Value.Mv[0]))
                {
                    Mv mv = prevFrameMvs.Value.Mv[1];
                    if (refSignBias[prevFrameMvs.Value.RefFrame[1]] != refSignBias[refFrame])
                    {
                        mv.Row *= -1;
                        mv.Col *= -1;
                    }
                    if (AddMvRefListEb(mv, ref refmvCount, mvRefList, earlyBreak))
                    {
                        goto Done;
                    }
                }
            }

            if (mode == PredictionMode.NearMv)
            {
                refmvCount = Constants.MaxMvRefCandidates;
            }
            else
            {
                // We only care about the nearestmv for the remaining modes
                refmvCount = 1;
            }

        Done:
            // Clamp vectors
            for (i = 0; i < refmvCount; ++i)
            {
                mvRefList[i].ClampMvRef(ref xd);
            }

            return refmvCount;
        }

        private static void AppendSub8x8MvsForIdx(
            ref Vp9Common cm,
            ref MacroBlockD xd,
            Span<Position> mvRefSearch,
            PredictionMode bMode,
            int block,
            int refr,
            int miRow,
            int miCol,
            ref Mv bestSub8x8)
        {
            Span<Mv> mvList = stackalloc Mv[Constants.MaxMvRefCandidates];
            ref ModeInfo mi = ref xd.Mi[0].Value;
            ref Array4<BModeInfo> bmi = ref mi.Bmi;
            int n;
            int refmvCount;

            Debug.Assert(Constants.MaxMvRefCandidates == 2);

            refmvCount = DecFindMvRefs(ref cm, ref xd, bMode, mi.RefFrame[refr], mvRefSearch, mvList, miRow, miCol, block, 1);

            switch (block)
            {
                case 0:
                    bestSub8x8 = mvList[refmvCount - 1];
                    break;
                case 1:
                case 2:
                    if (bMode == PredictionMode.NearestMv)
                    {
                        bestSub8x8 = bmi[0].Mv[refr];
                    }
                    else
                    {
                        bestSub8x8 = new Mv();
                        for (n = 0; n < refmvCount; ++n)
                        {
                            if (Unsafe.As<Mv, int>(ref bmi[0].Mv[refr]) != Unsafe.As<Mv, int>(ref mvList[n]))
                            {
                                bestSub8x8 = mvList[n];
                                break;
                            }
                        }
                    }
                    break;
                case 3:
                    if (bMode == PredictionMode.NearestMv)
                    {
                        bestSub8x8 = bmi[2].Mv[refr];
                    }
                    else
                    {
                        Span<Mv> candidates = [bmi[1].Mv[refr], bmi[0].Mv[refr], mvList[0], mvList[1]];
                        bestSub8x8 = new Mv();
                        for (n = 0; n < 2 + Constants.MaxMvRefCandidates; ++n)
                        {
                            if (Unsafe.As<Mv, int>(ref bmi[2].Mv[refr]) != Unsafe.As<Mv, int>(ref candidates[n]))
                            {
                                bestSub8x8 = candidates[n];
                                break;
                            }
                        }
                    }
                    break;
                default:
                    Debug.Assert(false, "Invalid block index.");
                    break;
            }
        }

        private static byte GetModeContext(ref Vp9Common cm, ref MacroBlockD xd, Span<Position> mvRefSearch, int miRow, int miCol)
        {
            int i;
            int contextCounter = 0;
            ref TileInfo tile = ref xd.Tile;

            // Get mode count from nearest 2 blocks
            for (i = 0; i < 2; ++i)
            {
                ref Position mvRef = ref mvRefSearch[i];
                if (tile.IsInside(miCol, miRow, cm.MiRows, ref mvRef))
                {
                    ref ModeInfo candidate = ref xd.Mi[mvRef.Col + mvRef.Row * xd.MiStride].Value;
                    // Keep counts for entropy encoding.
                    contextCounter += Luts.Mode2Counter[(int)candidate.Mode];
                }
            }

            return (byte)Luts.CounterToContext[contextCounter];
        }

        private static void ReadInterBlockModeInfo(
            ref Vp9Common cm,
            ref MacroBlockD xd,
            ref ModeInfo mi,
            int miRow,
            int miCol,
            ref Reader r)
        {
            BlockSize bsize = mi.SbType;
            bool allowHP = cm.AllowHighPrecisionMv;
            Array2<Mv> bestRefMvs = new();
            int refr, isCompound;
            byte interModeCtx;
            Span<Position> mvRefSearch = Luts.MvRefBlocks[(int)bsize];

            ReadRefFrames(ref cm, ref xd, ref r, mi.SegmentId, ref mi.RefFrame);
            isCompound = mi.HasSecondRef() ? 1 : 0;
            interModeCtx = GetModeContext(ref cm, ref xd, mvRefSearch, miRow, miCol);

            if (cm.Seg.IsSegFeatureActive(mi.SegmentId, SegLvlFeatures.SegLvlSkip) != 0)
            {
                mi.Mode = PredictionMode.ZeroMv;
                if (bsize < BlockSize.Block8x8)
                {
                    xd.ErrorInfo.Value.InternalError(CodecErr.CodecUnsupBitstream, "Invalid usage of segement feature on small blocks");

                    return;
                }
            }
            else
            {
                if (bsize >= BlockSize.Block8x8)
                {
                    mi.Mode = ReadInterMode(ref cm, ref xd, ref r, interModeCtx);
                }
                else
                {
                    // Sub 8x8 blocks use the nearestmv as a ref_mv if the bMode is NewMv.
                    // Setting mode to NearestMv forces the search to stop after the nearestmv
                    // has been found. After bModes have been read, mode will be overwritten
                    // by the last bMode.
                    mi.Mode = PredictionMode.NearestMv;
                }

                if (mi.Mode != PredictionMode.ZeroMv)
                {
                    Span<Mv> tmpMvs = stackalloc Mv[Constants.MaxMvRefCandidates];

                    for (refr = 0; refr < 1 + isCompound; ++refr)
                    {
                        sbyte frame = mi.RefFrame[refr];
                        int refmvCount;

                        refmvCount = DecFindMvRefs(ref cm, ref xd, mi.Mode, frame, mvRefSearch, tmpMvs, miRow, miCol, -1, 0);

                        DecFindBestRefMvs(allowHP, tmpMvs, ref bestRefMvs[refr], refmvCount);
                    }
                }
            }

            mi.InterpFilter = (cm.InterpFilter == Constants.Switchable) ? ReadSwitchableInterpFilter(ref cm, ref xd, ref r) : cm.InterpFilter;

            if (bsize < BlockSize.Block8x8)
            {
                int num4X4W = 1 << xd.BmodeBlocksWl;
                int num4X4H = 1 << xd.BmodeBlocksHl;
                int idx, idy;
                PredictionMode bMode = 0;
                Array2<Mv> bestSub8x8 = new();
                const uint InvalidMv = 0x80008000;
                // Initialize the 2nd element as even though it won't be used meaningfully
                // if isCompound is false.
                Unsafe.As<Mv, uint>(ref bestSub8x8[1]) = InvalidMv;
                for (idy = 0; idy < 2; idy += num4X4H)
                {
                    for (idx = 0; idx < 2; idx += num4X4W)
                    {
                        int j = idy * 2 + idx;
                        bMode = ReadInterMode(ref cm, ref xd, ref r, interModeCtx);

                        if (bMode == PredictionMode.NearestMv || bMode == PredictionMode.NearMv)
                        {
                            for (refr = 0; refr < 1 + isCompound; ++refr)
                            {
                                AppendSub8x8MvsForIdx(ref cm, ref xd, mvRefSearch, bMode, j, refr, miRow, miCol, ref bestSub8x8[refr]);
                            }
                        }

                        if (!AssignMv(ref cm, ref xd, bMode, ref mi.Bmi[j].Mv, ref bestRefMvs, ref bestSub8x8, isCompound, allowHP, ref r))
                        {
                            xd.Corrupted |= true;
                            break;
                        }

                        if (num4X4H == 2)
                        {
                            mi.Bmi[j + 2] = mi.Bmi[j];
                        }

                        if (num4X4W == 2)
                        {
                            mi.Bmi[j + 1] = mi.Bmi[j];
                        }
                    }
                }

                mi.Mode = bMode;

                CopyMvPair(ref mi.Mv, ref mi.Bmi[3].Mv);
            }
            else
            {
                xd.Corrupted |= !AssignMv(ref cm, ref xd, mi.Mode, ref mi.Mv, ref bestRefMvs, ref bestRefMvs, isCompound, allowHP, ref r);
            }
        }

        private static void ReadInterFrameModeInfo(
            ref Vp9Common cm,
            ref MacroBlockD xd,
            int miRow,
            int miCol,
            ref Reader r,
            int xMis,
            int yMis)
        {
            ref ModeInfo mi = ref xd.Mi[0].Value;
            bool interBlock;

            mi.SegmentId = (sbyte)ReadInterSegmentId(ref cm, ref xd, miRow, miCol, ref r, xMis, yMis);
            mi.Skip = (sbyte)ReadSkip(ref cm, ref xd, mi.SegmentId, ref r);
            interBlock = ReadIsInterBlock(ref cm, ref xd, mi.SegmentId, ref r);
            mi.TxSize = ReadTxSize(ref cm, ref xd, mi.Skip == 0 || !interBlock, ref r);

            if (interBlock)
            {
                ReadInterBlockModeInfo(ref cm, ref xd, ref mi, miRow, miCol, ref r);
            }
            else
            {
                ReadIntraBlockModeInfo(ref cm, ref xd, ref mi, ref r);
            }
        }

        private static PredictionMode LeftBlockMode(Ptr<ModeInfo> curMi, Ptr<ModeInfo> leftMi, int b)
        {
            if (b == 0 || b == 2)
            {
                if (leftMi.IsNull || leftMi.Value.IsInterBlock())
                {
                    return PredictionMode.DcPred;
                }

                return leftMi.Value.GetYMode(b + 1);
            }

            Debug.Assert(b == 1 || b == 3);

            return curMi.Value.Bmi[b - 1].Mode;
        }

        private static PredictionMode AboveBlockMode(Ptr<ModeInfo> curMi, Ptr<ModeInfo> aboveMi, int b)
        {
            if (b == 0 || b == 1)
            {
                if (aboveMi.IsNull || aboveMi.Value.IsInterBlock())
                {
                    return PredictionMode.DcPred;
                }

                return aboveMi.Value.GetYMode(b + 2);
            }

            Debug.Assert(b == 2 || b == 3);

            return curMi.Value.Bmi[b - 2].Mode;
        }

        private static ReadOnlySpan<byte> GetYModeProbs(
            ref Vp9EntropyProbs fc,
            Ptr<ModeInfo> mi,
            Ptr<ModeInfo> aboveMi,
            Ptr<ModeInfo> leftMi,
            int block)
        {
            PredictionMode above = AboveBlockMode(mi, aboveMi, block);
            PredictionMode left = LeftBlockMode(mi, leftMi, block);

            return fc.KfYModeProb[(int)above][(int)left].AsSpan();
        }

        private static void ReadIntraFrameModeInfo(
            ref Vp9Common cm,
            ref MacroBlockD xd,
            int miRow,
            int miCol,
            ref Reader r,
            int xMis,
            int yMis)
        {
            Ptr<ModeInfo> mi = xd.Mi[0];
            Ptr<ModeInfo> aboveMi = xd.AboveMi;
            Ptr<ModeInfo> leftMi = xd.LeftMi;
            BlockSize bsize = mi.Value.SbType;
            int i;
            int miOffset = miRow * cm.MiCols + miCol;

            mi.Value.SegmentId = (sbyte)ReadIntraSegmentId(ref cm, miOffset, xMis, yMis, ref r);
            mi.Value.Skip = (sbyte)ReadSkip(ref cm, ref xd, mi.Value.SegmentId, ref r);
            mi.Value.TxSize = ReadTxSize(ref cm, ref xd, true, ref r);
            mi.Value.RefFrame[0] = Constants.IntraFrame;
            mi.Value.RefFrame[1] = Constants.None;

            switch (bsize)
            {
                case BlockSize.Block4x4:
                    for (i = 0; i < 4; ++i)
                    {
                        mi.Value.Bmi[i].Mode =
                            ReadIntraMode(ref r, GetYModeProbs(ref cm.Fc.Value, mi, aboveMi, leftMi, i));
                    }

                    mi.Value.Mode = mi.Value.Bmi[3].Mode;
                    break;
                case BlockSize.Block4x8:
                    mi.Value.Bmi[0].Mode = mi.Value.Bmi[2].Mode =
                        ReadIntraMode(ref r, GetYModeProbs(ref cm.Fc.Value, mi, aboveMi, leftMi, 0));
                    mi.Value.Bmi[1].Mode = mi.Value.Bmi[3].Mode = mi.Value.Mode =
                        ReadIntraMode(ref r, GetYModeProbs(ref cm.Fc.Value, mi, aboveMi, leftMi, 1));
                    break;
                case BlockSize.Block8x4:
                    mi.Value.Bmi[0].Mode = mi.Value.Bmi[1].Mode =
                        ReadIntraMode(ref r, GetYModeProbs(ref cm.Fc.Value, mi, aboveMi, leftMi, 0));
                    mi.Value.Bmi[2].Mode = mi.Value.Bmi[3].Mode = mi.Value.Mode =
                        ReadIntraMode(ref r, GetYModeProbs(ref cm.Fc.Value, mi, aboveMi, leftMi, 2));
                    break;
                default:
                    mi.Value.Mode = ReadIntraMode(ref r, GetYModeProbs(ref cm.Fc.Value, mi, aboveMi, leftMi, 0));
                    break;
            }

            mi.Value.UvMode = ReadIntraMode(ref r, cm.Fc.Value.KfUvModeProb[(int)mi.Value.Mode].AsSpan());
        }

        private static void CopyRefFramePair(ref Array2<sbyte> dst, ref Array2<sbyte> src)
        {
            dst[0] = src[0];
            dst[1] = src[1];
        }

        public static void ReadModeInfo(
            ref TileWorkerData twd,
            ref Vp9Common cm,
            int miRow,
            int miCol,
            int xMis,
            int yMis)
        {
            ref Reader r = ref twd.BitReader;
            ref MacroBlockD xd = ref twd.Xd;
            ref ModeInfo mi = ref xd.Mi[0].Value;
            ArrayPtr<MvRef> frameMvs = cm.CurFrameMvs.Slice(miRow * cm.MiCols + miCol);
            int w, h;

            if (cm.FrameIsIntraOnly())
            {
                ReadIntraFrameModeInfo(ref cm, ref xd, miRow, miCol, ref r, xMis, yMis);
            }
            else
            {
                ReadInterFrameModeInfo(ref cm, ref xd, miRow, miCol, ref r, xMis, yMis);

                for (h = 0; h < yMis; ++h)
                {
                    for (w = 0; w < xMis; ++w)
                    {
                        ref MvRef mv = ref frameMvs[w];
                        CopyRefFramePair(ref mv.RefFrame, ref mi.RefFrame);
                        CopyMvPair(ref mv.Mv, ref mi.Mv);
                    }
                    frameMvs = frameMvs.Slice(cm.MiCols);
                }
            }
        }
    }
}
