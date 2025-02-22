using Ryujinx.Common.Memory;
using Ryujinx.Graphics.Nvdec.Vp9.Common;
using Ryujinx.Graphics.Video;

namespace Ryujinx.Graphics.Nvdec.Vp9.Types
{
    internal struct MacroBlockD
    {
        public Array3<MacroBlockDPlane> Plane;
        public byte BmodeBlocksWl;
        public byte BmodeBlocksHl;

        public Ptr<Vp9BackwardUpdates> Counts;
        public TileInfo Tile;

        public int MiStride;

        // Grid of 8x8 cells is placed over the block.
        // If some of them belong to the same mbtree-block
        // they will just have same mi[i][j] value
        public ArrayPtr<Ptr<ModeInfo>> Mi;
        public Ptr<ModeInfo> LeftMi;
        public Ptr<ModeInfo> AboveMi;

        public uint MaxBlocksWide;
        public uint MaxBlocksHigh;

        public ArrayPtr<Array3<byte>> PartitionProbs;

        /* Distance of MB away from frame edges */
        public int MbToLeftEdge;
        public int MbToRightEdge;
        public int MbToTopEdge;
        public int MbToBottomEdge;

        public Ptr<Vp9EntropyProbs> Fc;

        /* pointers to reference frames */
        public Array2<Ptr<RefBuffer>> BlockRefs;

        /* pointer to current frame */
        public Surface CurBuf;

        public Array3<ArrayPtr<sbyte>> AboveContext;
        public Array3<Array16<sbyte>> LeftContext;

        public ArrayPtr<sbyte> AboveSegContext;
        public Array8<sbyte> LeftSegContext;

        /* Bit depth: 8, 10, 12 */
        public int Bd;

        public bool Lossless;
        public bool Corrupted;

        public Ptr<InternalErrorInfo> ErrorInfo;

        public int GetPredContextSegId()
        {
            sbyte aboveSip = !AboveMi.IsNull ? AboveMi.Value.SegIdPredicted : (sbyte)0;
            sbyte leftSip = !LeftMi.IsNull ? LeftMi.Value.SegIdPredicted : (sbyte)0;

            return aboveSip + leftSip;
        }

        public int GetSkipContext()
        {
            int aboveSkip = !AboveMi.IsNull ? AboveMi.Value.Skip : 0;
            int leftSkip = !LeftMi.IsNull ? LeftMi.Value.Skip : 0;
            return aboveSkip + leftSkip;
        }

        public int GetPredContextSwitchableInterp()
        {
            // Note:
            // The mode info data structure has a one element border above and to the
            // left of the entries corresponding to real macroblocks.
            // The prediction flags in these dummy entries are initialized to 0.
            int leftType = !LeftMi.IsNull ? LeftMi.Value.InterpFilter : Constants.SwitchableFilters;
            int aboveType = !AboveMi.IsNull ? AboveMi.Value.InterpFilter : Constants.SwitchableFilters;

            if (leftType == aboveType)
            {
                return leftType;
            }

            if (leftType == Constants.SwitchableFilters)
            {
                return aboveType;
            }

            if (aboveType == Constants.SwitchableFilters)
            {
                return leftType;
            }

            return Constants.SwitchableFilters;
        }

        // The mode info data structure has a one element border above and to the
        // left of the entries corresponding to real macroblocks.
        // The prediction flags in these dummy entries are initialized to 0.
        // 0 - inter/inter, inter/--, --/inter, --/--
        // 1 - intra/inter, inter/intra
        // 2 - intra/--, --/intra
        // 3 - intra/intra
        public int GetIntraInterContext()
        {
            if (!AboveMi.IsNull && !LeftMi.IsNull)
            {
                // Both edges available
                bool aboveIntra = !AboveMi.Value.IsInterBlock();
                bool leftIntra = !LeftMi.Value.IsInterBlock();
                return leftIntra && aboveIntra ? 3 : leftIntra || aboveIntra ? 1 : 0;
            }

            if (!AboveMi.IsNull || !LeftMi.IsNull)
            {
                // One edge available
                return 2 * (!(!AboveMi.IsNull ? AboveMi.Value : LeftMi.Value).IsInterBlock() ? 1 : 0);
            }

            return 0;
        }

        // Returns a context number for the given MB prediction signal
        // The mode info data structure has a one element border above and to the
        // left of the entries corresponding to real blocks.
        // The prediction flags in these dummy entries are initialized to 0.
        public int GetTxSizeContext()
        {
            int maxTxSize = (int)Luts.MaxTxSizeLookup[(int)Mi[0].Value.SbType];
            int aboveCtx = !AboveMi.IsNull && AboveMi.Value.Skip == 0 ? (int)AboveMi.Value.TxSize : maxTxSize;
            int leftCtx = !LeftMi.IsNull && LeftMi.Value.Skip == 0 ? (int)LeftMi.Value.TxSize : maxTxSize;
            if (LeftMi.IsNull)
            {
                leftCtx = aboveCtx;
            }

            if (AboveMi.IsNull)
            {
                aboveCtx = leftCtx;
            }

            return aboveCtx + leftCtx > maxTxSize ? 1 : 0;
        }

        public void SetupBlockPlanes(int ssX, int ssY)
        {
            for (int i = 0; i < Constants.MaxMbPlane; i++)
            {
                Plane[i].SubsamplingX = i != 0 ? ssX : 0;
                Plane[i].SubsamplingY = i != 0 ? ssY : 0;
            }
        }

        public void SetSkipContext(int miRow, int miCol)
        {
            int aboveIdx = miCol * 2;
            int leftIdx = (miRow * 2) & 15;

            for (int i = 0; i < Constants.MaxMbPlane; ++i)
            {
                ref MacroBlockDPlane pd = ref Plane[i];
                pd.AboveContext = AboveContext[i].Slice(aboveIdx >> pd.SubsamplingX);
                pd.LeftContext = new ArrayPtr<sbyte>(ref LeftContext[i][leftIdx >> pd.SubsamplingY],
                    16 - (leftIdx >> pd.SubsamplingY));
            }
        }

        internal void SetMiRowCol(ref TileInfo tile, int miRow, int bh, int miCol, int bw, int miRows, int miCols)
        {
            MbToTopEdge = -(miRow * Constants.MiSize * 8);
            MbToBottomEdge = (miRows - bh - miRow) * Constants.MiSize * 8;
            MbToLeftEdge = -(miCol * Constants.MiSize * 8);
            MbToRightEdge = (miCols - bw - miCol) * Constants.MiSize * 8;

            // Are edges available for intra prediction?
            AboveMi = miRow != 0 ? Mi[-MiStride] : Ptr<ModeInfo>.Null;
            LeftMi = miCol > tile.MiColStart ? Mi[-1] : Ptr<ModeInfo>.Null;
        }

        public unsafe void DecResetSkipContext()
        {
            for (int i = 0; i < Constants.MaxMbPlane; i++)
            {
                ref MacroBlockDPlane pd = ref Plane[i];
                MemoryUtil.Fill(pd.AboveContext.ToPointer(), (sbyte)0, pd.N4W);
                MemoryUtil.Fill(pd.LeftContext.ToPointer(), (sbyte)0, pd.N4H);
            }
        }
    }
}