using Ryujinx.Common.Memory;
using Ryujinx.Graphics.Nvdec.Vp9.Common;
using Ryujinx.Graphics.Nvdec.Vp9.Dsp;
using Ryujinx.Graphics.Video;
using System;

namespace Ryujinx.Graphics.Nvdec.Vp9.Types
{
    internal struct Vp9Common
    {
        public MacroBlockD Mb;

        public ArrayPtr<TileWorkerData> TileWorkerData;
        public int TotalTiles;

        public InternalErrorInfo Error;

        public VpxColorSpace ColorSpace;
        public VpxColorRange ColorRange;

        public int Width;
        public int Height;

        public int RenderWidth;
        public int RenderHeight;

        public int LastWidth;
        public int LastHeight;

        public int SubsamplingX;
        public int SubsamplingY;

        public bool UseHighBitDepth;

        public ArrayPtr<MvRef> PrevFrameMvs;
        public ArrayPtr<MvRef> CurFrameMvs;

        public Ptr<Surface> FrameToShow;
        public Ptr<RefCntBuffer> PrevFrame;

        public Ptr<RefCntBuffer> CurFrame;

        public Array8<int> RefFrameMap; /* maps fb_idx to reference slot */

        // Prepare ref_frame_map for the next frame.
        // Only used in frame parallel decode.
        public Array8<int> NextRefFrameMap;

        public Array3<RefBuffer> FrameRefs;

        public int NewFbIdx;

        public int CurShowFrameFbIdx;

        public FrameType LastFrameType;
        public FrameType FrameType;

        public int ShowFrame;
        public int LastShowFrame;
        public int ShowExistingFrame;

        // Flag signaling that the frame is encoded using only Intra modes.
        public bool IntraOnly;
        public bool LastIntraOnly;

        public bool AllowHighPrecisionMv;

        public int ResetFrameContext;

        // MBs, MbRows/Cols is in 16-pixel units; MiRows/Cols is in
        // ModeInfo (8-pixel) units.
        public int MBs;
        public int MbRows, MiRows;
        public int MbCols, MiCols;
        public int MiStride;

        /* Profile settings */
        public TxMode TxMode;

        public int BaseQindex;
        public int YDcDeltaQ;
        public int UvDcDeltaQ;
        public int UvAcDeltaQ;
        public Array8<Array2<short>> YDequant;
        public Array8<Array2<short>> UvDequant;

        /* We allocate a ModeInfo struct for each macroblock, together with
           an extra row on top and column on the left to simplify prediction. */
        public int MiAllocSize;
        public ArrayPtr<ModeInfo> Mip; /* Base of allocated array */
        public ArrayPtr<ModeInfo> Mi; /* Corresponds to upper left visible macroblock */

        // prev_mip and prev_mi will only be allocated in VP9 encoder.
        public Ptr<ModeInfo> PrevMip; /* MODE_INFO array 'mip' from last decoded frame */
        public Ptr<ModeInfo> PrevMi; /* 'mi' from last frame (points into prev_mip) */

        public ArrayPtr<Ptr<ModeInfo>> MiGridBase;
        public ArrayPtr<Ptr<ModeInfo>> MiGridVisible;

        // Whether to use previous frame's motion vectors for prediction.
        public bool UsePrevFrameMvs;

        // Persistent mb segment id map used in prediction.
        public int SegMapIdx;
        public int PrevSegMapIdx;

        public Array2<ArrayPtr<byte>> SegMapArray;
        public ArrayPtr<byte> LastFrameSegMap;
        public ArrayPtr<byte> CurrentFrameSegMap;

        public byte InterpFilter;

        public LoopFilterInfoN LfInfo;

        public int RefreshFrameContext; /* Two state 0 = NO, 1 = YES */

        public Array4<sbyte> RefFrameSignBias; /* Two state 0, 1 */

        public LoopFilter Lf;
        public Segmentation Seg;

        // Context probabilities for reference frame prediction
        public sbyte CompFixedRef;
        public Array2<sbyte> CompVarRef;
        public ReferenceMode ReferenceMode;

        public Ptr<Vp9EntropyProbs> Fc;
        public ArrayPtr<Vp9EntropyProbs> FrameContexts; // FRAME_CONTEXTS
        public uint FrameContextIdx; /* Context to use/update */
        public Ptr<Vp9BackwardUpdates> Counts;

        public uint CurrentVideoFrame;
        public BitstreamProfile Profile;

        public BitDepth BitDepth;
        public BitDepth DequantBitDepth; // bit_depth of current dequantizer

        public int ErrorResilientMode;
        public int FrameParallelDecodingMode;

        public int Log2TileCols, Log2TileRows;

        public int ByteAlignment;
        public int SkipLoopFilter;

        public Ptr<BufferPool> BufferPool;

        public ArrayPtr<sbyte> AboveSegContext;
        public ArrayPtr<sbyte> AboveContext;

        public bool FrameIsIntraOnly()
        {
            return FrameType == FrameType.KeyFrame || IntraOnly;
        }

        public bool CompoundReferenceAllowed()
        {
            for (int i = 1; i < Constants.RefsPerFrame; ++i)
            {
                if (RefFrameSignBias[i + 1] != RefFrameSignBias[1])
                {
                    return true;
                }
            }

            return false;
        }

        public ref Surface GetFrameNewBuffer()
        {
            return ref BufferPool.Value.FrameBufs[NewFbIdx].Buf;
        }

        public int GetFreeFb()
        {
            ref Array12<RefCntBuffer> frameBufs = ref BufferPool.Value.FrameBufs;

            int i;

            for (i = 0; i < Constants.FrameBuffers; ++i)
            {
                if (frameBufs[i].RefCount == 0)
                {
                    break;
                }
            }

            if (i != Constants.FrameBuffers)
            {
                frameBufs[i].RefCount = 1;
            }
            else
            {
                // Reset i to be INVALID_IDX to indicate no free buffer found.
                i = RefBuffer.InvalidIdx;
            }

            return i;
        }

        public void SwapCurrentAndLastSegMap()
        {
            // Swap indices.
            (SegMapIdx, PrevSegMapIdx) = (PrevSegMapIdx, SegMapIdx);

            CurrentFrameSegMap = SegMapArray[SegMapIdx];
            LastFrameSegMap = SegMapArray[PrevSegMapIdx];
        }

        private static int CalcMiSize(int len)
        {
            // Len is in mi units.
            return len + Constants.MiBlockSize;
        }

        public void SetMbMi(int width, int height)
        {
            int alignedWidth = BitUtils.AlignPowerOfTwo(width, Constants.MiSizeLog2);
            int alignedHeight = BitUtils.AlignPowerOfTwo(height, Constants.MiSizeLog2);

            MiCols = alignedWidth >> Constants.MiSizeLog2;
            MiRows = alignedHeight >> Constants.MiSizeLog2;
            MiStride = CalcMiSize(MiCols);

            MbCols = (MiCols + 1) >> 1;
            MbRows = (MiRows + 1) >> 1;
            MBs = MbRows * MbCols;
        }

        public void AllocTileWorkerData(MemoryAllocator allocator, int tileCols, int tileRows, int maxThreads)
        {
            TileWorkerData =
                allocator.Allocate<TileWorkerData>((tileCols * tileRows) + (maxThreads > 1 ? maxThreads : 0));
        }

        public void FreeTileWorkerData(MemoryAllocator allocator)
        {
            allocator.Free(TileWorkerData);
        }

        private void AllocSegMap(MemoryAllocator allocator, int segMapSize)
        {
            for (int i = 0; i < Constants.NumPingPongBuffers; ++i)
            {
                SegMapArray[i] = allocator.Allocate<byte>(segMapSize);
            }

            // Init the index.
            SegMapIdx = 0;
            PrevSegMapIdx = 1;

            CurrentFrameSegMap = SegMapArray[SegMapIdx];
            LastFrameSegMap = SegMapArray[PrevSegMapIdx];
        }

        private void FreeSegMap(MemoryAllocator allocator)
        {
            for (int i = 0; i < Constants.NumPingPongBuffers; ++i)
            {
                allocator.Free(SegMapArray[i]);
                SegMapArray[i] = ArrayPtr<byte>.Null;
            }

            CurrentFrameSegMap = ArrayPtr<byte>.Null;
            LastFrameSegMap = ArrayPtr<byte>.Null;
        }

        private void DecAllocMi(MemoryAllocator allocator, int miSize)
        {
            Mip = allocator.Allocate<ModeInfo>(miSize);
            MiGridBase = allocator.Allocate<Ptr<ModeInfo>>(miSize);
        }

        private void DecFreeMi(MemoryAllocator allocator)
        {
            allocator.Free(Mip);
            Mip = ArrayPtr<ModeInfo>.Null;
            allocator.Free(MiGridBase);
            MiGridBase = ArrayPtr<Ptr<ModeInfo>>.Null;
        }

        public void FreeContextBuffers(MemoryAllocator allocator)
        {
            DecFreeMi(allocator);
            FreeSegMap(allocator);
            allocator.Free(AboveContext);
            AboveContext = ArrayPtr<sbyte>.Null;
            allocator.Free(AboveSegContext);
            AboveSegContext = ArrayPtr<sbyte>.Null;
            allocator.Free(Lf.Lfm);
            Lf.Lfm = ArrayPtr<LoopFilterMask>.Null;
            allocator.Free(CurFrameMvs);
            CurFrameMvs = ArrayPtr<MvRef>.Null;

            if (UsePrevFrameMvs)
            {
                allocator.Free(PrevFrameMvs);
                PrevFrameMvs = ArrayPtr<MvRef>.Null;
            }
        }

        private void AllocLoopFilter(MemoryAllocator allocator)
        {
            // Each lfm holds bit masks for all the 8x8 blocks in a 64x64 region. The
            // stride and rows are rounded up / truncated to a multiple of 8.
            Lf.LfmStride = (MiCols + (Constants.MiBlockSize - 1)) >> 3;
            Lf.Lfm = allocator.Allocate<LoopFilterMask>(((MiRows + (Constants.MiBlockSize - 1)) >> 3) * Lf.LfmStride);
        }

        public bool AllocContextBuffers(MemoryAllocator allocator, int width, int height)
        {
            SetMbMi(width, height);
            int newMiSize = MiStride * CalcMiSize(MiRows);
            if (newMiSize != 0)
            {
                DecAllocMi(allocator, newMiSize);
            }

            if (MiRows * MiCols != 0)
            {
                // Create the segmentation map structure and set to 0.
                AllocSegMap(allocator, MiRows * MiCols);
            }

            if (MiCols != 0)
            {
                AboveContext = allocator.Allocate<sbyte>(2 * TileInfo.MiColsAlignedToSb(MiCols) * Constants.MaxMbPlane);
                AboveSegContext = allocator.Allocate<sbyte>(TileInfo.MiColsAlignedToSb(MiCols));
            }

            AllocLoopFilter(allocator);

            CurFrameMvs = allocator.Allocate<MvRef>(MiRows * MiCols);
            // Using the same size as the current frame is fine here,
            // as this is never true when we have a resolution change.
            if (UsePrevFrameMvs)
            {
                PrevFrameMvs = allocator.Allocate<MvRef>(MiRows * MiCols);
            }

            return false;
        }

        private unsafe void DecSetupMi()
        {
            Mi = Mip.Slice(MiStride + 1);
            MiGridVisible = MiGridBase.Slice(MiStride + 1);
            MemoryUtil.Fill(MiGridBase.ToPointer(), Ptr<ModeInfo>.Null, MiStride * (MiRows + 1));
        }

        public unsafe void InitContextBuffers()
        {
            DecSetupMi();
            if (!LastFrameSegMap.IsNull)
            {
                MemoryUtil.Fill(LastFrameSegMap.ToPointer(), (byte)0, MiRows * MiCols);
            }
        }

        private void SetPartitionProbs(ref MacroBlockD xd)
        {
            xd.PartitionProbs = FrameIsIntraOnly()
                ? new ArrayPtr<Array3<byte>>(ref Fc.Value.KfPartitionProb[0], 16)
                : new ArrayPtr<Array3<byte>>(ref Fc.Value.PartitionProb[0], 16);
        }

        internal void InitMacroBlockD(ref MacroBlockD xd, ArrayPtr<int> dqcoeff)
        {
            for (int i = 0; i < Constants.MaxMbPlane; ++i)
            {
                xd.Plane[i].DqCoeff = dqcoeff;
                xd.AboveContext[i] = AboveContext.Slice(i * 2 * TileInfo.MiColsAlignedToSb(MiCols));

                if (i == 0)
                {
                    MemoryUtil.Copy(ref xd.Plane[i].SegDequant, ref YDequant);
                }
                else
                {
                    MemoryUtil.Copy(ref xd.Plane[i].SegDequant, ref UvDequant);
                }

                xd.Fc = new Ptr<Vp9EntropyProbs>(ref Fc.Value);
            }

            xd.AboveSegContext = AboveSegContext;
            xd.MiStride = MiStride;
            xd.ErrorInfo = new Ptr<InternalErrorInfo>(ref Error);

            SetPartitionProbs(ref xd);
        }

        public void SetupSegmentationDequant()
        {
            // Build y/uv dequant values based on segmentation.
            if (Seg.Enabled)
            {
                for (int i = 0; i < Constants.MaxSegments; ++i)
                {
                    int qindex = Seg.GetQIndex(i, BaseQindex);
                    YDequant[i][0] = QuantCommon.DcQuant(qindex, YDcDeltaQ, BitDepth);
                    YDequant[i][1] = QuantCommon.AcQuant(qindex, 0, BitDepth);
                    UvDequant[i][0] = QuantCommon.DcQuant(qindex, UvDcDeltaQ, BitDepth);
                    UvDequant[i][1] = QuantCommon.AcQuant(qindex, UvAcDeltaQ, BitDepth);
                }
            }
            else
            {
                int qindex = BaseQindex;
                // When segmentation is disabled, only the first value is used.  The
                // remaining are don't cares.
                YDequant[0][0] = QuantCommon.DcQuant(qindex, YDcDeltaQ, BitDepth);
                YDequant[0][1] = QuantCommon.AcQuant(qindex, 0, BitDepth);
                UvDequant[0][0] = QuantCommon.DcQuant(qindex, UvDcDeltaQ, BitDepth);
                UvDequant[0][1] = QuantCommon.AcQuant(qindex, UvAcDeltaQ, BitDepth);
            }
        }

        public void SetupScaleFactors()
        {
            for (int i = 0; i < Constants.RefsPerFrame; ++i)
            {
                ref RefBuffer refBuf = ref FrameRefs[i];
                refBuf.Sf.SetupScaleFactorsForFrame(refBuf.Buf.Width, refBuf.Buf.Height, Width, Height);
            }
        }

        public void ReadFrameReferenceModeProbs(ref Reader r)
        {
            ref Vp9EntropyProbs fc = ref Fc.Value;


            if (ReferenceMode == ReferenceMode.Select)
            {
                for (int i = 0; i < Constants.CompInterContexts; ++i)
                {
                    r.DiffUpdateProb(ref fc.CompInterProb[i]);
                }
            }

            if (ReferenceMode != ReferenceMode.Compound)
            {
                for (int i = 0; i < Constants.RefContexts; ++i)
                {
                    r.DiffUpdateProb(ref fc.SingleRefProb[i][0]);
                    r.DiffUpdateProb(ref fc.SingleRefProb[i][1]);
                }
            }

            if (ReferenceMode != ReferenceMode.Single)
            {
                for (int i = 0; i < Constants.RefContexts; ++i)
                {
                    r.DiffUpdateProb(ref fc.CompRefProb[i]);
                }
            }
        }

        public ReferenceMode ReadFrameReferenceMode(ref Reader r)
        {
            if (CompoundReferenceAllowed())
            {
                return r.ReadBit() != 0
                    ? r.ReadBit() != 0 ? ReferenceMode.Select : ReferenceMode.Compound
                    : ReferenceMode.Single;
            }

            return ReferenceMode.Single;
        }

        public void SetupCompoundReferenceMode()
        {
            if (RefFrameSignBias[Constants.LastFrame] == RefFrameSignBias[Constants.GoldenFrame])
            {
                CompFixedRef = Constants.AltRefFrame;
                CompVarRef[0] = Constants.LastFrame;
                CompVarRef[1] = Constants.GoldenFrame;
            }
            else if (RefFrameSignBias[Constants.LastFrame] == RefFrameSignBias[Constants.AltRefFrame])
            {
                CompFixedRef = Constants.GoldenFrame;
                CompVarRef[0] = Constants.LastFrame;
                CompVarRef[1] = Constants.AltRefFrame;
            }
            else
            {
                CompFixedRef = Constants.LastFrame;
                CompVarRef[0] = Constants.GoldenFrame;
                CompVarRef[1] = Constants.AltRefFrame;
            }
        }

        public void InitMvProbs()
        {
            Fc.Value.Joints[0] = 32;
            Fc.Value.Joints[1] = 64;
            Fc.Value.Joints[2] = 96;

            Fc.Value.Sign[0] = 128;
            Fc.Value.Classes[0][0] = 224;
            Fc.Value.Classes[0][1] = 144;
            Fc.Value.Classes[0][2] = 192;
            Fc.Value.Classes[0][3] = 168;
            Fc.Value.Classes[0][4] = 192;
            Fc.Value.Classes[0][5] = 176;
            Fc.Value.Classes[0][6] = 192;
            Fc.Value.Classes[0][7] = 198;
            Fc.Value.Classes[0][8] = 198;
            Fc.Value.Classes[0][9] = 245;
            Fc.Value.Class0[0][0] = 216;
            Fc.Value.Bits[0][0] = 136;
            Fc.Value.Bits[0][1] = 140;
            Fc.Value.Bits[0][2] = 148;
            Fc.Value.Bits[0][3] = 160;
            Fc.Value.Bits[0][4] = 176;
            Fc.Value.Bits[0][5] = 192;
            Fc.Value.Bits[0][6] = 224;
            Fc.Value.Bits[0][7] = 234;
            Fc.Value.Bits[0][8] = 234;
            Fc.Value.Bits[0][9] = 240;
            Fc.Value.Class0Fp[0][0][0] = 128;
            Fc.Value.Class0Fp[0][0][1] = 128;
            Fc.Value.Class0Fp[0][0][2] = 64;
            Fc.Value.Class0Fp[0][1][0] = 96;
            Fc.Value.Class0Fp[0][1][1] = 112;
            Fc.Value.Class0Fp[0][1][2] = 64;
            Fc.Value.Fp[0][0] = 64;
            Fc.Value.Fp[0][1] = 96;
            Fc.Value.Fp[0][2] = 64;
            Fc.Value.Class0Hp[0] = 160;
            Fc.Value.Hp[0] = 128;

            Fc.Value.Sign[1] = 128;
            Fc.Value.Classes[1][0] = 216;
            Fc.Value.Classes[1][1] = 128;
            Fc.Value.Classes[1][2] = 176;
            Fc.Value.Classes[1][3] = 160;
            Fc.Value.Classes[1][4] = 176;
            Fc.Value.Classes[1][5] = 176;
            Fc.Value.Classes[1][6] = 192;
            Fc.Value.Classes[1][7] = 198;
            Fc.Value.Classes[1][8] = 198;
            Fc.Value.Classes[1][9] = 208;
            Fc.Value.Class0[1][0] = 208;
            Fc.Value.Bits[1][0] = 136;
            Fc.Value.Bits[1][1] = 140;
            Fc.Value.Bits[1][2] = 148;
            Fc.Value.Bits[1][3] = 160;
            Fc.Value.Bits[1][4] = 176;
            Fc.Value.Bits[1][5] = 192;
            Fc.Value.Bits[1][6] = 224;
            Fc.Value.Bits[1][7] = 234;
            Fc.Value.Bits[1][8] = 234;
            Fc.Value.Bits[1][9] = 240;
            Fc.Value.Class0Fp[1][0][0] = 128;
            Fc.Value.Class0Fp[1][0][1] = 128;
            Fc.Value.Class0Fp[1][0][2] = 64;
            Fc.Value.Class0Fp[1][1][0] = 96;
            Fc.Value.Class0Fp[1][1][1] = 112;
            Fc.Value.Class0Fp[1][1][2] = 64;
            Fc.Value.Fp[1][0] = 64;
            Fc.Value.Fp[1][1] = 96;
            Fc.Value.Fp[1][2] = 64;
            Fc.Value.Class0Hp[1] = 160;
            Fc.Value.Hp[1] = 128;
        }

        public void AdaptMvProbs(bool allowHp)
        {
            ref Vp9EntropyProbs fc = ref Fc.Value;
            ref Vp9EntropyProbs preFc = ref FrameContexts[(int)FrameContextIdx];
            ref Vp9BackwardUpdates counts = ref Counts.Value;

            Prob.VpxTreeMergeProbs(
                EntropyMv.JointTree,
                preFc.Joints.AsSpan(),
                counts.Joints.AsSpan(),
                fc.Joints.AsSpan());

            for (int i = 0; i < 2; ++i)
            {
                fc.Sign[i] = Prob.ModeMvMergeProbs(preFc.Sign[i], ref counts.Sign[i]);
                Prob.VpxTreeMergeProbs(
                    EntropyMv.ClassTree,
                    preFc.Classes[i].AsSpan(),
                    counts.Classes[i].AsSpan(),
                    fc.Classes[i].AsSpan());
                Prob.VpxTreeMergeProbs(
                    EntropyMv.Class0Tree,
                    preFc.Class0[i].AsSpan(),
                    counts.Class0[i].AsSpan(),
                    fc.Class0[i].AsSpan());

                for (int j = 0; j < EntropyMv.OffsetBits; ++j)
                {
                    fc.Bits[i][j] = Prob.ModeMvMergeProbs(preFc.Bits[i][j], ref counts.Bits[i][j]);
                }

                for (int j = 0; j < EntropyMv.Class0Size; ++j)
                {
                    Prob.VpxTreeMergeProbs(
                        EntropyMv.FpTree,
                        preFc.Class0Fp[i][j].AsSpan(),
                        counts.Class0Fp[i][j].AsSpan(),
                        fc.Class0Fp[i][j].AsSpan());
                }

                Prob.VpxTreeMergeProbs(EntropyMv.FpTree, preFc.Fp[i].AsSpan(), counts.Fp[i].AsSpan(),
                    fc.Fp[i].AsSpan());

                if (allowHp)
                {
                    fc.Class0Hp[i] = Prob.ModeMvMergeProbs(preFc.Class0Hp[i], ref counts.Class0Hp[i]);
                    fc.Hp[i] = Prob.ModeMvMergeProbs(preFc.Hp[i], ref counts.Hp[i]);
                }
            }
        }

        public void ResizeContextBuffers(MemoryAllocator allocator, int width, int height)
        {
            if (Width != width || Height != height)
            {
                int newMiRows = BitUtils.AlignPowerOfTwo(height, Constants.MiSizeLog2) >> Constants.MiSizeLog2;
                int newMiCols = BitUtils.AlignPowerOfTwo(width, Constants.MiSizeLog2) >> Constants.MiSizeLog2;

                // Allocations in AllocContextBuffers() depend on individual
                // dimensions as well as the overall size.
                if (newMiCols > MiCols || newMiRows > MiRows)
                {
                    if (AllocContextBuffers(allocator, width, height))
                    {
                        // The Mi* values have been cleared and any existing context
                        // buffers have been freed. Clear Width and Height to be
                        // consistent and to force a realloc next time.
                        Width = 0;
                        Height = 0;
                        Error.InternalError(CodecErr.MemError, "Failed to allocate context buffers");
                    }
                }
                else
                {
                    SetMbMi(width, height);
                }

                InitContextBuffers();
                Width = width;
                Height = height;
            }

            if (CurFrameMvs.IsNull ||
                MiRows > CurFrame.Value.MiRows ||
                MiCols > CurFrame.Value.MiCols)
            {
                ResizeMvBuffer(allocator);
            }
        }

        public void CheckMemError<T>(ref ArrayPtr<T> lval, ArrayPtr<T> expr)
            where T : unmanaged
        {
            lval = expr;
            if (lval.IsNull)
            {
                Error.InternalError(CodecErr.MemError, "Failed to allocate");
            }
        }

        private void ResizeMvBuffer(MemoryAllocator allocator)
        {
            allocator.Free(CurFrameMvs);
            CurFrame.Value.MiRows = MiRows;
            CurFrame.Value.MiCols = MiCols;
            CheckMemError(ref CurFrameMvs, allocator.Allocate<MvRef>(MiRows * MiCols));
        }

        public void CheckMemError<T>(ref Ptr<T> lval, Ptr<T> expr) where T : unmanaged
        {
            lval = expr;
            if (lval.IsNull)
            {
                Error.InternalError(CodecErr.MemError, "Failed to allocate");
            }
        }

        public void SetupTileInfo(ref ReadBitBuffer rb)
        {
            int minLog2TileCols = 0, maxLog2TileCols = 0, maxOnes;
            TileInfo.GetTileNBits(MiCols, out minLog2TileCols, out maxLog2TileCols);

            // columns
            maxOnes = maxLog2TileCols - minLog2TileCols;
            Log2TileCols = minLog2TileCols;
            while (maxOnes-- != 0 && rb.ReadBit() != 0)
            {
                Log2TileCols++;
            }

            if (Log2TileCols > 6)
            {
                Error.InternalError(CodecErr.CorruptFrame, "Invalid number of tile columns");
            }

            // rows
            Log2TileRows = rb.ReadBit();
            if (Log2TileRows != 0)
            {
                Log2TileRows += rb.ReadBit();
            }
        }

        public void ReadBitdepthColorspaceSampling(ref ReadBitBuffer rb)
        {
            if (Profile >= BitstreamProfile.Profile2)
            {
                BitDepth = rb.ReadBit() != 0 ? BitDepth.Bits12 : BitDepth.Bits10;
                UseHighBitDepth = true;
            }
            else
            {
                BitDepth = BitDepth.Bits8;
                UseHighBitDepth = false;
            }

            ColorSpace = (VpxColorSpace)rb.ReadLiteral(3);
            if (ColorSpace != VpxColorSpace.Srgb)
            {
                ColorRange = (VpxColorRange)rb.ReadBit();
                if (Profile == BitstreamProfile.Profile1 || Profile == BitstreamProfile.Profile3)
                {
                    SubsamplingX = rb.ReadBit();
                    SubsamplingY = rb.ReadBit();
                    if (SubsamplingX == 1 && SubsamplingY == 1)
                    {
                        Error.InternalError(CodecErr.UnsupBitstream,
                            "4:2:0 color not supported in profile 1 or 3");
                    }

                    if (rb.ReadBit() != 0)
                    {
                        Error.InternalError(CodecErr.UnsupBitstream, "Reserved bit set");
                    }
                }
                else
                {
                    SubsamplingY = SubsamplingX = 1;
                }
            }
            else
            {
                ColorRange = VpxColorRange.Full;
                if (Profile == BitstreamProfile.Profile1 || Profile == BitstreamProfile.Profile3)
                {
                    // Note if colorspace is SRGB then 4:4:4 chroma sampling is assumed.
                    // 4:2:2 or 4:4:0 chroma sampling is not allowed.
                    SubsamplingY = SubsamplingX = 0;
                    if (rb.ReadBit() != 0)
                    {
                        Error.InternalError(CodecErr.UnsupBitstream, "Reserved bit set");
                    }
                }
                else
                {
                    Error.InternalError(CodecErr.UnsupBitstream, "4:4:4 color not supported in profile 0 or 2");
                }
            }
        }

        public void AdaptModeProbs()
        {
            ref Vp9EntropyProbs fc = ref Fc.Value;
            ref Vp9EntropyProbs preFc = ref FrameContexts[(int)FrameContextIdx];
            ref Vp9BackwardUpdates counts = ref Counts.Value;

            for (int i = 0; i < Constants.IntraInterContexts; i++)
            {
                fc.IntraInterProb[i] = Prob.ModeMvMergeProbs(preFc.IntraInterProb[i], ref counts.IntraInter[i]);
            }

            for (int i = 0; i < Constants.CompInterContexts; i++)
            {
                fc.CompInterProb[i] = Prob.ModeMvMergeProbs(preFc.CompInterProb[i], ref counts.CompInter[i]);
            }

            for (int i = 0; i < Constants.RefContexts; i++)
            {
                fc.CompRefProb[i] = Prob.ModeMvMergeProbs(preFc.CompRefProb[i], ref counts.CompRef[i]);
            }

            for (int i = 0; i < Constants.RefContexts; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    fc.SingleRefProb[i][j] =
                        Prob.ModeMvMergeProbs(preFc.SingleRefProb[i][j], ref counts.SingleRef[i][j]);
                }
            }

            for (int i = 0; i < Constants.InterModeContexts; i++)
            {
                Prob.VpxTreeMergeProbs(
                    EntropyMode.InterModeTree,
                    preFc.InterModeProb[i].AsSpan(),
                    counts.InterMode[i].AsSpan(),
                    fc.InterModeProb[i].AsSpan());
            }

            for (int i = 0; i < EntropyMode.BlockSizeGroups; i++)
            {
                Prob.VpxTreeMergeProbs(
                    EntropyMode.IntraModeTree,
                    preFc.YModeProb[i].AsSpan(),
                    counts.YMode[i].AsSpan(),
                    fc.YModeProb[i].AsSpan());
            }

            for (int i = 0; i < Constants.IntraModes; ++i)
            {
                Prob.VpxTreeMergeProbs(
                    EntropyMode.IntraModeTree,
                    preFc.UvModeProb[i].AsSpan(),
                    counts.UvMode[i].AsSpan(),
                    fc.UvModeProb[i].AsSpan());
            }

            for (int i = 0; i < Constants.PartitionContexts; i++)
            {
                Prob.VpxTreeMergeProbs(
                    EntropyMode.PartitionTree,
                    preFc.PartitionProb[i].AsSpan(),
                    counts.Partition[i].AsSpan(),
                    fc.PartitionProb[i].AsSpan());
            }

            if (InterpFilter == Constants.Switchable)
            {
                for (int i = 0; i < Constants.SwitchableFilterContexts; i++)
                {
                    Prob.VpxTreeMergeProbs(
                        EntropyMode.SwitchableInterpTree,
                        preFc.SwitchableInterpProb[i].AsSpan(),
                        counts.SwitchableInterp[i].AsSpan(),
                        fc.SwitchableInterpProb[i].AsSpan());
                }
            }

            if (TxMode == TxMode.TxModeSelect)
            {
                Array1<Array2<uint>> branchCt8X8P = new();
                Array2<Array2<uint>> branchCt16X16P = new();
                Array3<Array2<uint>> branchCt32X32P = new();

                for (int i = 0; i < EntropyMode.TxSizeContexts; ++i)
                {
                    EntropyMode.TxCountsToBranchCounts8X8(counts.Tx8x8[i].AsSpan(), ref branchCt8X8P);
                    for (int j = 0; j < (int)TxSize.TxSizes - 3; ++j)
                    {
                        fc.Tx8x8Prob[i][j] = Prob.ModeMvMergeProbs(preFc.Tx8x8Prob[i][j], ref branchCt8X8P[j]);
                    }

                    EntropyMode.TxCountsToBranchCounts16X16(counts.Tx16x16[i].AsSpan(), ref branchCt16X16P);
                    for (int j = 0; j < (int)TxSize.TxSizes - 2; ++j)
                    {
                        fc.Tx16x16Prob[i][j] =
                            Prob.ModeMvMergeProbs(preFc.Tx16x16Prob[i][j], ref branchCt16X16P[j]);
                    }

                    EntropyMode.TxCountsToBranchCounts32X32(counts.Tx32x32[i].AsSpan(), ref branchCt32X32P);
                    for (int j = 0; j < (int)TxSize.TxSizes - 1; ++j)
                    {
                        fc.Tx32x32Prob[i][j] =
                            Prob.ModeMvMergeProbs(preFc.Tx32x32Prob[i][j], ref branchCt32X32P[j]);
                    }
                }
            }

            for (int i = 0; i < Constants.SkipContexts; ++i)
            {
                fc.SkipProb[i] = Prob.ModeMvMergeProbs(preFc.SkipProb[i], ref counts.Skip[i]);
            }
        }

        public void AdaptCoefProbs()
        {
            byte t;
            uint countSat, updateFactor;

            if (FrameIsIntraOnly())
            {
                updateFactor = Entropy.CoefMaxUpdateFactorKey;
                countSat = Entropy.CoefCountSatKey;
            }
            else if (LastFrameType == FrameType.KeyFrame)
            {
                updateFactor = Entropy.CoefMaxUpdateFactorAfterKey; /* adapt quickly */
                countSat = Entropy.CoefCountSatAfterKey;
            }
            else
            {
                updateFactor = Entropy.CoefMaxUpdateFactor;
                countSat = Entropy.CoefCountSat;
            }

            for (t = (int)TxSize.Tx4X4; t <= (int)TxSize.Tx32X32; t++)
            {
                AdaptCoefProbs(t, countSat, updateFactor);
            }
        }

        public void SetMvs(ReadOnlySpan<Vp9MvRef> mvs)
        {
            if (mvs.Length > PrevFrameMvs.Length)
            {
                throw new ArgumentException(
                    $"Size mismatch, expected: {PrevFrameMvs.Length}, but got: {mvs.Length}.");
            }

            for (int i = 0; i < mvs.Length; i++)
            {
                ref MvRef mv = ref PrevFrameMvs[i];

                mv.Mv[0].Row = mvs[i].Mvs[0].Row;
                mv.Mv[0].Col = mvs[i].Mvs[0].Col;
                mv.Mv[1].Row = mvs[i].Mvs[1].Row;
                mv.Mv[1].Col = mvs[i].Mvs[1].Col;

                mv.RefFrame[0] = (sbyte)mvs[i].RefFrames[0];
                mv.RefFrame[1] = (sbyte)mvs[i].RefFrames[1];
            }
        }

        public void GetMvs(Span<Vp9MvRef> mvs)
        {
            if (mvs.Length > CurFrameMvs.Length)
            {
                throw new ArgumentException(
                    $"Size mismatch, expected: {CurFrameMvs.Length}, but got: {mvs.Length}.");
            }

            for (int i = 0; i < mvs.Length; i++)
            {
                ref MvRef mv = ref CurFrameMvs[i];

                mvs[i].Mvs[0].Row = mv.Mv[0].Row;
                mvs[i].Mvs[0].Col = mv.Mv[0].Col;
                mvs[i].Mvs[1].Row = mv.Mv[1].Row;
                mvs[i].Mvs[1].Col = mv.Mv[1].Col;

                mvs[i].RefFrames[0] = mv.RefFrame[0];
                mvs[i].RefFrames[1] = mv.RefFrame[1];
            }
        }

        private void AdaptCoefProbs(byte txSize, uint countSat, uint updateFactor)
        {
            ref Vp9EntropyProbs preFc = ref FrameContexts[(int)FrameContextIdx];
            ref Array2<Array2<Array6<Array6<Array3<byte>>>>> probs = ref Fc.Value.CoefProbs[txSize];
            ref Array2<Array2<Array6<Array6<Array3<byte>>>>> preProbs = ref preFc.CoefProbs[txSize];
            ref Array2<Array2<Array6<Array6<Array4<uint>>>>> counts = ref Counts.Value.Coef[txSize];
            ref Array2<Array2<Array6<Array6<uint>>>> eobCounts = ref Counts.Value.EobBranch[txSize];

            for (int i = 0; i < Constants.PlaneTypes; ++i)
            {
                for (int j = 0; j < Entropy.RefTypes; ++j)
                {
                    for (int k = 0; k < Entropy.CoefBands; ++k)
                    {
                        for (int l = 0; l < Entropy.BAND_COEFF_CONTEXTS(k); ++l)
                        {
                            int n0 = (int)counts[i][j][k][l][Entropy.ZeroToken];
                            int n1 = (int)counts[i][j][k][l][Entropy.OneToken];
                            int n2 = (int)counts[i][j][k][l][Entropy.TwoToken];
                            int neob = (int)counts[i][j][k][l][Entropy.EobModelToken];
                            Array3<Array2<uint>> branchCt = new();
                            branchCt[0][0] = (uint)neob;
                            branchCt[0][1] = (uint)(eobCounts[i][j][k][l] - neob);
                            branchCt[1][0] = (uint)n0;
                            branchCt[1][1] = (uint)(n1 + n2);
                            branchCt[2][0] = (uint)n1;
                            branchCt[2][1] = (uint)n2;
                            for (int m = 0; m < Entropy.UnconstrainedNodes; ++m)
                            {
                                probs[i][j][k][l][m] = Prob.MergeProbs(preProbs[i][j][k][l][m], ref branchCt[m],
                                    countSat, updateFactor);
                            }
                        }
                    }
                }
            }
        }

        public void DefaultCoefProbs()
        {
            Entropy.CopyProbs(ref Fc.Value.CoefProbs[(int)TxSize.Tx4X4], Entropy.DefaultCoefProbs4X4);
            Entropy.CopyProbs(ref Fc.Value.CoefProbs[(int)TxSize.Tx8X8], Entropy.DefaultCoefProbs8X8);
            Entropy.CopyProbs(ref Fc.Value.CoefProbs[(int)TxSize.Tx16X16], Entropy.DefaultCoefProbs16X16);
            Entropy.CopyProbs(ref Fc.Value.CoefProbs[(int)TxSize.Tx32X32], Entropy.DefaultCoefProbs32X32);
        }
    }
}