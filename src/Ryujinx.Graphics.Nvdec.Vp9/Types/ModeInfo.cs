using Ryujinx.Common.Memory;
using System.Diagnostics;

namespace Ryujinx.Graphics.Nvdec.Vp9.Types
{
    internal struct ModeInfo
    {
        // Common for both Inter and Intra blocks
        public BlockSize SbType;
        public PredictionMode Mode;
        public TxSize TxSize;
        public sbyte Skip;
        public sbyte SegmentId;
        public sbyte SegIdPredicted; // Valid only when TemporalUpdate is enabled

        // Only for Intra blocks
        public PredictionMode UvMode;

        // Only for Inter blocks
        public byte InterpFilter;

        // if ref_frame[idx] is equal to AltRefFrame then
        // MacroBlockD.BlockRef[idx] is an altref
        public Array2<sbyte> RefFrame;

        public Array2<Mv> Mv;

        public Array4<BModeInfo> Bmi;

        public PredictionMode GetYMode(int block)
        {
            return SbType < BlockSize.Block8x8 ? Bmi[block].Mode : Mode;
        }

        public readonly TxSize GetUvTxSize(ref MacroBlockDPlane pd)
        {
            Debug.Assert(SbType < BlockSize.Block8x8 ||
                Luts.SsSizeLookup[(int)SbType][pd.SubsamplingX][pd.SubsamplingY] != BlockSize.BlockInvalid);

            return Luts.UvTxsizeLookup[(int)SbType][(int)TxSize][pd.SubsamplingX][pd.SubsamplingY];
        }

        public bool IsInterBlock()
        {
            return RefFrame[0] > Constants.IntraFrame;
        }

        public bool HasSecondRef()
        {
            return RefFrame[1] > Constants.IntraFrame;
        }

        private static readonly int[][] _idxNColumnToSubblock = [
            [1, 2], [1, 3], [3, 2], [3, 3],
        ];

        // This function returns either the appropriate sub block or block's mv
        // on whether the block_size < 8x8 and we have check_sub_blocks set.
        public Mv GetSubBlockMv(int whichMv, int searchCol, int blockIdx)
        {
            return blockIdx >= 0 && SbType < BlockSize.Block8x8
                ? Bmi[_idxNColumnToSubblock[blockIdx][searchCol == 0 ? 1 : 0]].Mv[whichMv]
                : Mv[whichMv];
        }
    }
}
