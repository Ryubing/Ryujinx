using Ryujinx.Common.Memory;
using Ryujinx.Graphics.Nvdec.Vp9.Common;
using Ryujinx.Graphics.Nvdec.Vp9.Types;
using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Nvdec.Vp9
{
    internal static class LoopFilter
    {
        public const int MaxLoopFilter = 63;

        public const int MaxRefLfDeltas = 4;
        public const int MaxModeLfDeltas = 2;

        // 64 bit masks for left transform size. Each 1 represents a position where
        // we should apply a loop filter across the left border of an 8x8 block
        // boundary.
        //
        // In the case of TX_16X16 ->  ( in low order byte first we end up with
        // a mask that looks like this
        //
        //    10101010
        //    10101010
        //    10101010
        //    10101010
        //    10101010
        //    10101010
        //    10101010
        //    10101010
        //
        // A loopfilter should be applied to every other 8x8 horizontally.
        private static readonly ulong[] _left64X64TxformMask = [
            0xffffffffffffffffUL, // TX_4X4
            0xffffffffffffffffUL, // TX_8x8
            0x5555555555555555UL, // TX_16x16
            0x1111111111111111UL, // TX_32x32
        ];

        // 64 bit masks for above transform size. Each 1 represents a position where
        // we should apply a loop filter across the top border of an 8x8 block
        // boundary.
        //
        // In the case of TX_32x32 ->  ( in low order byte first we end up with
        // a mask that looks like this
        //
        //    11111111
        //    00000000
        //    00000000
        //    00000000
        //    11111111
        //    00000000
        //    00000000
        //    00000000
        //
        // A loopfilter should be applied to every other 4 the row vertically.
        private static readonly ulong[] _above64X64TxformMask = [
            0xffffffffffffffffUL, // TX_4X4
            0xffffffffffffffffUL, // TX_8x8
            0x00ff00ff00ff00ffUL, // TX_16x16
            0x000000ff000000ffUL, // TX_32x32
        ];

        // 64 bit masks for prediction sizes (left). Each 1 represents a position
        // where left border of an 8x8 block. These are aligned to the right most
        // appropriate bit, and then shifted into place.
        //
        // In the case of TX_16x32 ->  ( low order byte first ) we end up with
        // a mask that looks like this :
        //
        //  10000000
        //  10000000
        //  10000000
        //  10000000
        //  00000000
        //  00000000
        //  00000000
        //  00000000
        private static readonly ulong[] _leftPredictionMask = [
            0x0000000000000001UL, // BLOCK_4X4,
            0x0000000000000001UL, // BLOCK_4X8,
            0x0000000000000001UL, // BLOCK_8X4,
            0x0000000000000001UL, // BLOCK_8X8,
            0x0000000000000101UL, // BLOCK_8X16,
            0x0000000000000001UL, // BLOCK_16X8,
            0x0000000000000101UL, // BLOCK_16X16,
            0x0000000001010101UL, // BLOCK_16X32,
            0x0000000000000101UL, // BLOCK_32X16,
            0x0000000001010101UL, // BLOCK_32X32,
            0x0101010101010101UL, // BLOCK_32X64,
            0x0000000001010101UL, // BLOCK_64X32,
            0x0101010101010101UL, // BLOCK_64X64
        ];

        // 64 bit mask to shift and set for each prediction size.
        private static readonly ulong[] _abovePredictionMask = [
            0x0000000000000001UL, // BLOCK_4X4
            0x0000000000000001UL, // BLOCK_4X8
            0x0000000000000001UL, // BLOCK_8X4
            0x0000000000000001UL, // BLOCK_8X8
            0x0000000000000001UL, // BLOCK_8X16,
            0x0000000000000003UL, // BLOCK_16X8
            0x0000000000000003UL, // BLOCK_16X16
            0x0000000000000003UL, // BLOCK_16X32,
            0x000000000000000fUL, // BLOCK_32X16,
            0x000000000000000fUL, // BLOCK_32X32,
            0x000000000000000fUL, // BLOCK_32X64,
            0x00000000000000ffUL, // BLOCK_64X32,
            0x00000000000000ffUL, // BLOCK_64X64
        ];

        // 64 bit mask to shift and set for each prediction size. A bit is set for
        // each 8x8 block that would be in the left most block of the given block
        // size in the 64x64 block.
        private static readonly ulong[] _sizeMask = [
            0x0000000000000001UL, // BLOCK_4X4
            0x0000000000000001UL, // BLOCK_4X8
            0x0000000000000001UL, // BLOCK_8X4
            0x0000000000000001UL, // BLOCK_8X8
            0x0000000000000101UL, // BLOCK_8X16,
            0x0000000000000003UL, // BLOCK_16X8
            0x0000000000000303UL, // BLOCK_16X16
            0x0000000003030303UL, // BLOCK_16X32,
            0x0000000000000f0fUL, // BLOCK_32X16,
            0x000000000f0f0f0fUL, // BLOCK_32X32,
            0x0f0f0f0f0f0f0f0fUL, // BLOCK_32X64,
            0x00000000ffffffffUL, // BLOCK_64X32,
            0xffffffffffffffffUL, // BLOCK_64X64
        ];

        // These are used for masking the left and above borders.
#pragma warning disable IDE0051 // Remove unused private member
        private const ulong LeftBorder = 0x1111111111111111UL;
        private const ulong AboveBorder = 0x000000ff000000ffUL;
#pragma warning restore IDE0051

        // 16 bit masks for uv transform sizes.
        private static readonly ushort[] _left64X64TxformMaskUv = [
            0xffff, // TX_4X4
            0xffff, // TX_8x8
            0x5555, // TX_16x16
            0x1111, // TX_32x32
        ];

        private static readonly ushort[] _above64X64TxformMaskUv = [
            0xffff, // TX_4X4
            0xffff, // TX_8x8
            0x0f0f, // TX_16x16
            0x000f, // TX_32x32
        ];

        // 16 bit left mask to shift and set for each uv prediction size.
        private static readonly ushort[] _leftPredictionMaskUv = [
            0x0001, // BLOCK_4X4,
            0x0001, // BLOCK_4X8,
            0x0001, // BLOCK_8X4,
            0x0001, // BLOCK_8X8,
            0x0001, // BLOCK_8X16,
            0x0001, // BLOCK_16X8,
            0x0001, // BLOCK_16X16,
            0x0011, // BLOCK_16X32,
            0x0001, // BLOCK_32X16,
            0x0011, // BLOCK_32X32,
            0x1111, // BLOCK_32X64
            0x0011, // BLOCK_64X32,
            0x1111, // BLOCK_64X64
        ];

        // 16 bit above mask to shift and set for uv each prediction size.
        private static readonly ushort[] _abovePredictionMaskUv = [
            0x0001, // BLOCK_4X4
            0x0001, // BLOCK_4X8
            0x0001, // BLOCK_8X4
            0x0001, // BLOCK_8X8
            0x0001, // BLOCK_8X16,
            0x0001, // BLOCK_16X8
            0x0001, // BLOCK_16X16
            0x0001, // BLOCK_16X32,
            0x0003, // BLOCK_32X16,
            0x0003, // BLOCK_32X32,
            0x0003, // BLOCK_32X64,
            0x000f, // BLOCK_64X32,
            0x000f, // BLOCK_64X64
        ];

        // 64 bit mask to shift and set for each uv prediction size
        private static readonly ushort[] _sizeMaskUv = [
            0x0001, // BLOCK_4X4
            0x0001, // BLOCK_4X8
            0x0001, // BLOCK_8X4
            0x0001, // BLOCK_8X8
            0x0001, // BLOCK_8X16,
            0x0001, // BLOCK_16X8
            0x0001, // BLOCK_16X16
            0x0011, // BLOCK_16X32,
            0x0003, // BLOCK_32X16,
            0x0033, // BLOCK_32X32,
            0x3333, // BLOCK_32X64,
            0x00ff, // BLOCK_64X32,
            0xffff, // BLOCK_64X64
        ];

#pragma warning disable IDE0051 // Remove unused private member
        private const ushort LeftBorderUv = 0x1111;
        private const ushort AboveBorderUv = 0x000f;
#pragma warning restore IDE0051

        private static readonly int[] _modeLfLut = [
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // INTRA_MODES
            1, 1, 0, 1, // INTER_MODES (ZEROMV == 0)
        ];

        private static byte GetFilterLevel(ref LoopFilterInfoN lfiN, ref ModeInfo mi)
        {
            return lfiN.Lvl[mi.SegmentId][mi.RefFrame[0]][_modeLfLut[(int)mi.Mode]];
        }

        private static ref LoopFilterMask GetLfm(ref Types.LoopFilter lf, int miRow, int miCol)
        {
            return ref lf.Lfm[(miCol >> 3) + ((miRow >> 3) * lf.LfmStride)];
        }

        // 8x8 blocks in a superblock. A "1" represents the first block in a 16x16
        // or greater area.
        private static readonly byte[][] _firstBlockIn16X16 = [
            [1, 0, 1, 0, 1, 0, 1, 0], [0, 0, 0, 0, 0, 0, 0, 0],
            [1, 0, 1, 0, 1, 0, 1, 0], [0, 0, 0, 0, 0, 0, 0, 0],
            [1, 0, 1, 0, 1, 0, 1, 0], [0, 0, 0, 0, 0, 0, 0, 0],
            [1, 0, 1, 0, 1, 0, 1, 0], [0, 0, 0, 0, 0, 0, 0, 0],
        ];

        // This function sets up the bit masks for a block represented
        // by miRow, miCol in a 64x64 region.
        public static void BuildMask(ref Vp9Common cm, ref ModeInfo mi, int miRow, int miCol, int bw, int bh)
        {
            BlockSize blockSize = mi.SbType;
            TxSize txSizeY = mi.TxSize;
            ref LoopFilterInfoN lfiN = ref cm.LfInfo;
            int filterLevel = GetFilterLevel(ref lfiN, ref mi);
            TxSize txSizeUv = Luts.UvTxsizeLookup[(int)blockSize][(int)txSizeY][1][1];
            ref LoopFilterMask lfm = ref GetLfm(ref cm.Lf, miRow, miCol);
            ref ulong leftY = ref lfm.LeftY[(int)txSizeY];
            ref ulong aboveY = ref lfm.AboveY[(int)txSizeY];
            ref ulong int4X4Y = ref lfm.Int4x4Y;
            ref ushort leftUv = ref lfm.LeftUv[(int)txSizeUv];
            ref ushort aboveUv = ref lfm.AboveUv[(int)txSizeUv];
            ref ushort int4X4Uv = ref lfm.Int4x4Uv;
            int rowInSb = (miRow & 7);
            int colInSb = (miCol & 7);
            int shiftY = colInSb + (rowInSb << 3);
            int shiftUv = (colInSb >> 1) + ((rowInSb >> 1) << 2);
            int buildUv = _firstBlockIn16X16[rowInSb][colInSb];

            if (filterLevel == 0)
            {
                return;
            }

            int index = shiftY;
            int i;
            for (i = 0; i < bh; i++)
            {
                MemoryMarshal.CreateSpan(ref lfm.LflY[index], 64 - index)[..bw].Fill((byte)filterLevel);
                index += 8;
            }

            // These set 1 in the current block size for the block size edges.
            // For instance if the block size is 32x16, we'll set:
            //    above =   1111
            //              0000
            //    and
            //    left  =   1000
            //          =   1000
            // NOTE : In this example the low bit is left most ( 1000 ) is stored as
            //        1,  not 8...
            //
            // U and V set things on a 16 bit scale.
            //
            aboveY |= _abovePredictionMask[(int)blockSize] << shiftY;
            leftY |= _leftPredictionMask[(int)blockSize] << shiftY;

            if (buildUv != 0)
            {
                aboveUv |= (ushort)(_abovePredictionMaskUv[(int)blockSize] << shiftUv);
                leftUv |= (ushort)(_leftPredictionMaskUv[(int)blockSize] << shiftUv);
            }

            // If the block has no coefficients and is not intra we skip applying
            // the loop filter on block edges.
            if (mi.Skip != 0 && mi.IsInterBlock())
            {
                return;
            }

            // Add a mask for the transform size. The transform size mask is set to
            // be correct for a 64x64 prediction block size. Mask to match the size of
            // the block we are working on and then shift it into place.
            aboveY |= (_sizeMask[(int)blockSize] & _above64X64TxformMask[(int)txSizeY]) << shiftY;
            leftY |= (_sizeMask[(int)blockSize] & _left64X64TxformMask[(int)txSizeY]) << shiftY;

            if (buildUv != 0)
            {
                aboveUv |= (ushort)((_sizeMaskUv[(int)blockSize] & _above64X64TxformMaskUv[(int)txSizeUv]) << shiftUv);
                leftUv |= (ushort)((_sizeMaskUv[(int)blockSize] & _left64X64TxformMaskUv[(int)txSizeUv]) << shiftUv);
            }

            // Try to determine what to do with the internal 4x4 block boundaries. These
            // differ from the 4x4 boundaries on the outside edge of an 8x8 in that the
            // internal ones can be skipped and don't depend on the prediction block size.
            if (txSizeY == TxSize.Tx4x4)
            {
                int4X4Y |= _sizeMask[(int)blockSize] << shiftY;
            }

            if (buildUv != 0 && txSizeUv == TxSize.Tx4x4)
            {
                int4X4Uv |= (ushort)((_sizeMaskUv[(int)blockSize] & 0xffff) << shiftUv);
            }
        }

        public static unsafe void ResetLfm(ref Vp9Common cm)
        {
            if (cm.Lf.FilterLevel != 0)
            {
                MemoryUtil.Fill(cm.Lf.Lfm.ToPointer(), new LoopFilterMask(), ((cm.MiRows + (Constants.MiBlockSize - 1)) >> 3) * cm.Lf.LfmStride);
            }
        }

        private static void UpdateSharpness(ref LoopFilterInfoN lfi, int sharpnessLvl)
        {
            int lvl;

            // For each possible value for the loop filter fill out limits
            for (lvl = 0; lvl <= MaxLoopFilter; lvl++)
            {
                // Set loop filter parameters that control sharpness.
                int blockInsideLimit = lvl >> ((sharpnessLvl > 0 ? 1 : 0) + (sharpnessLvl > 4 ? 1 : 0));

                if (sharpnessLvl > 0)
                {
                    if (blockInsideLimit > (9 - sharpnessLvl))
                    {
                        blockInsideLimit = (9 - sharpnessLvl);
                    }
                }

                if (blockInsideLimit < 1)
                {
                    blockInsideLimit = 1;
                }

                lfi.Lfthr[lvl].Lim.AsSpan().Fill((byte)blockInsideLimit);
                lfi.Lfthr[lvl].Mblim.AsSpan().Fill((byte)(2 * (lvl + 2) + blockInsideLimit));
            }
        }

        public static void LoopFilterFrameInit(ref Vp9Common cm, int defaultFiltLvl)
        {
            int segId;
            // nShift is the multiplier for lfDeltas
            // the multiplier is 1 for when filterLvl is between 0 and 31;
            // 2 when filterLvl is between 32 and 63
            int scale = 1 << (defaultFiltLvl >> 5);
            ref LoopFilterInfoN lfi = ref cm.LfInfo;
            ref Types.LoopFilter lf = ref cm.Lf;
            ref Segmentation seg = ref cm.Seg;

            // Update limits if sharpness has changed
            if (lf.LastSharpnessLevel != lf.SharpnessLevel)
            {
                UpdateSharpness(ref lfi, lf.SharpnessLevel);
                lf.LastSharpnessLevel = lf.SharpnessLevel;
            }

            for (segId = 0; segId < Constants.MaxSegments; segId++)
            {
                int lvlSeg = defaultFiltLvl;
                if (seg.IsSegFeatureActive(segId, SegLvlFeatures.SegLvlAltLf) != 0)
                {
                    int data = seg.GetSegData(segId, SegLvlFeatures.SegLvlAltLf);
                    lvlSeg = Math.Clamp(seg.AbsDelta == Constants.SegmentAbsData ? data : defaultFiltLvl + data, 0, MaxLoopFilter);
                }

                if (!lf.ModeRefDeltaEnabled)
                {
                    // We could get rid of this if we assume that deltas are set to
                    // zero when not in use; encoder always uses deltas
                    MemoryMarshal.Cast<Array2<byte>, byte>(lfi.Lvl[segId].AsSpan()).Fill((byte)lvlSeg);
                }
                else
                {
                    int refr, mode;
                    int intraLvl = lvlSeg + lf.RefDeltas[Constants.IntraFrame] * scale;
                    lfi.Lvl[segId][Constants.IntraFrame][0] = (byte)Math.Clamp(intraLvl, 0, MaxLoopFilter);

                    for (refr = Constants.LastFrame; refr < Constants.MaxRefFrames; ++refr)
                    {
                        for (mode = 0; mode < MaxModeLfDeltas; ++mode)
                        {
                            int interLvl = lvlSeg + lf.RefDeltas[refr] * scale + lf.ModeDeltas[mode] * scale;
                            lfi.Lvl[segId][refr][mode] = (byte)Math.Clamp(interLvl, 0, MaxLoopFilter);
                        }
                    }
                }
            }
        }
    }
}
