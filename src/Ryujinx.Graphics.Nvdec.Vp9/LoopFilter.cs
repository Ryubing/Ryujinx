using Ryujinx.Common.Memory;
using Ryujinx.Graphics.Nvdec.Vp9.Common;
using Ryujinx.Graphics.Nvdec.Vp9.Dsp;
using Ryujinx.Graphics.Nvdec.Vp9.Types;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Ryujinx.Graphics.Nvdec.Vp9
{
    internal static class LoopFilter
    {
        public const int MaxLoopFilter = 63;

        public const int MaxRefLfDeltas = 4;
        public const int MaxModeLfDeltas = 2;

        private struct LfSync
        {
            private int[] _curSbCol;
            private object[] _syncObjects;
            private int _syncRange;

            private static int GetSyncRange(int width)
            {
                // nsync numbers are picked by testing. For example, for 4k
                // video, using 4 gives best performance.
                if (width < 640)
                {
                    return 1;
                }

                if (width <= 1280)
                {
                    return 2;
                }

                if (width <= 4096)
                {
                    return 4;
                }

                return 8;
            }

            public void Initialize(int width, int sbRows)
            {
                if (_curSbCol == null || _curSbCol.Length != sbRows)
                {
                    _curSbCol = new int[sbRows];
                    _syncObjects = new object[sbRows];

                    for (int i = 0; i < sbRows; i++)
                    {
                        _syncObjects[i] = new object();
                    }
                }

                _syncRange = GetSyncRange(width);
                _curSbCol.AsSpan().Fill(-1);
            }

            public void SyncRead(int r, int c)
            {
                if (_curSbCol == null)
                {
                    return;
                }

                int nsync = _syncRange;

                if (r != 0 && (c & (nsync - 1)) == 0)
                {
                    object syncObject = _syncObjects[r - 1];
                    lock (syncObject)
                    {
                        while (c > _curSbCol[r - 1] - nsync)
                        {
                            Monitor.Wait(syncObject);
                        }
                    }
                }
            }

            public void SyncWrite(int r, int c, int sbCols)
            {
                if (_curSbCol == null)
                {
                    return;
                }

                int nsync = _syncRange;

                int cur;
                // Only signal when there are enough filtered SB for next row to run.
                bool sig = true;

                if (c < sbCols - 1)
                {
                    cur = c;

                    if (c % nsync != 0)
                    {
                        sig = false;
                    }
                }
                else
                {
                    cur = sbCols + nsync;
                }

                if (sig)
                {
                    object syncObject = _syncObjects[r];

                    lock (syncObject)
                    {
                        _curSbCol[r] = cur;

                        Monitor.Pulse(syncObject);
                    }
                }
            }
        }

        // 64 bit masks for left transform size. Each 1 represents a position where
        // we should apply a loop filter across the left border of an 8x8 block
        // boundary.
        //
        // In the case of (int)TxSize.Tx16x16 .  ( in low order byte first we end up with
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
        private static readonly ulong[] _left64X64TxformMask =
        [
            0xffffffffffffffffUL, // (int)TxSize.Tx4x4
            0xffffffffffffffffUL, // (int)TxSize.Tx8x8
            0x5555555555555555UL, // (int)TxSize.Tx16x16
            0x1111111111111111UL // (int)TxSize.Tx32x32
        ];

        // 64 bit masks for above transform size. Each 1 represents a position where
        // we should apply a loop filter across the top border of an 8x8 block
        // boundary.
        //
        // In the case of (int)TxSize.Tx32x32 .  ( in low order byte first we end up with
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
        private static readonly ulong[] _above64X64TxformMask =
        [
            0xffffffffffffffffUL, // (int)TxSize.Tx4x4
            0xffffffffffffffffUL, // (int)TxSize.Tx8x8
            0x00ff00ff00ff00ffUL, // (int)TxSize.Tx16x16
            0x000000ff000000ffUL // (int)TxSize.Tx32x32
        ];

        // 64 bit masks for prediction sizes (left). Each 1 represents a position
        // where left border of an 8x8 block. These are aligned to the right most
        // appropriate bit, and then shifted into place.
        //
        // In the case of TX_16x32 .  ( low order byte first ) we end up with
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
        private static readonly ulong[] _leftPredictionMask =
        [
            0x0000000000000001UL, // BLOCK_4x4,
            0x0000000000000001UL, // BLOCK_4x8,
            0x0000000000000001UL, // BLOCK_8x4,
            0x0000000000000001UL, // BLOCK_8x8,
            0x0000000000000101UL, // BLOCK_8x16,
            0x0000000000000001UL, // BLOCK_16x8,
            0x0000000000000101UL, // BLOCK_16x16,
            0x0000000001010101UL, // BLOCK_16x32,
            0x0000000000000101UL, // BLOCK_32x16,
            0x0000000001010101UL, // BLOCK_32x32,
            0x0101010101010101UL, // BLOCK_32x64,
            0x0000000001010101UL, // BLOCK_64x32,
            0x0101010101010101UL // BLOCK_64x64
        ];

        // 64 bit mask to shift and set for each prediction size.
        private static readonly ulong[] _abovePredictionMask =
        [
            0x0000000000000001UL, // BLOCK_4x4
            0x0000000000000001UL, // BLOCK_4x8
            0x0000000000000001UL, // BLOCK_8x4
            0x0000000000000001UL, // BLOCK_8x8
            0x0000000000000001UL, // BLOCK_8x16,
            0x0000000000000003UL, // BLOCK_16x8
            0x0000000000000003UL, // BLOCK_16x16
            0x0000000000000003UL, // BLOCK_16x32,
            0x000000000000000fUL, // BLOCK_32x16,
            0x000000000000000fUL, // BLOCK_32x32,
            0x000000000000000fUL, // BLOCK_32x64,
            0x00000000000000ffUL, // BLOCK_64x32,
            0x00000000000000ffUL // BLOCK_64x64
        ];

        // 64 bit mask to shift and set for each prediction size. A bit is set for
        // each 8x8 block that would be in the left most block of the given block
        // size in the 64x64 block.
        private static readonly ulong[] _sizeMask =
        [
            0x0000000000000001UL, // BLOCK_4x4
            0x0000000000000001UL, // BLOCK_4x8
            0x0000000000000001UL, // BLOCK_8x4
            0x0000000000000001UL, // BLOCK_8x8
            0x0000000000000101UL, // BLOCK_8x16,
            0x0000000000000003UL, // BLOCK_16x8
            0x0000000000000303UL, // BLOCK_16x16
            0x0000000003030303UL, // BLOCK_16x32,
            0x0000000000000f0fUL, // BLOCK_32x16,
            0x000000000f0f0f0fUL, // BLOCK_32x32,
            0x0f0f0f0f0f0f0f0fUL, // BLOCK_32x64,
            0x00000000ffffffffUL, // BLOCK_64x32,
            0xffffffffffffffffUL // BLOCK_64x64
        ];

        // These are used for masking the left and above borders.
        private const ulong LeftBorder = 0x1111111111111111UL;
        private const ulong AboveBorder = 0x000000ff000000ffUL;

        // 16 bit masks for uv transform sizes.
        private static readonly ushort[] _left64X64TxformMaskUv =
        [
            0xffff, // (int)TxSize.Tx4x4
            0xffff, // (int)TxSize.Tx8x8
            0x5555, // (int)TxSize.Tx16x16
            0x1111 // (int)TxSize.Tx32x32
        ];

        private static readonly ushort[] _above64X64TxformMaskUv =
        [
            0xffff, // (int)TxSize.Tx4x4
            0xffff, // (int)TxSize.Tx8x8
            0x0f0f, // (int)TxSize.Tx16x16
            0x000f // (int)TxSize.Tx32x32
        ];

        // 16 bit left mask to shift and set for each uv prediction size.
        private static readonly ushort[] _leftPredictionMaskUv =
        [
            0x0001, // BLOCK_4x4,
            0x0001, // BLOCK_4x8,
            0x0001, // BLOCK_8x4,
            0x0001, // BLOCK_8x8,
            0x0001, // BLOCK_8x16,
            0x0001, // BLOCK_16x8,
            0x0001, // BLOCK_16x16,
            0x0011, // BLOCK_16x32,
            0x0001, // BLOCK_32x16,
            0x0011, // BLOCK_32x32,
            0x1111, // BLOCK_32x64
            0x0011, // BLOCK_64x32,
            0x1111 // BLOCK_64x64
        ];

        // 16 bit above mask to shift and set for uv each prediction size.
        private static readonly ushort[] _abovePredictionMaskUv =
        [
            0x0001, // BLOCK_4x4
            0x0001, // BLOCK_4x8
            0x0001, // BLOCK_8x4
            0x0001, // BLOCK_8x8
            0x0001, // BLOCK_8x16,
            0x0001, // BLOCK_16x8
            0x0001, // BLOCK_16x16
            0x0001, // BLOCK_16x32,
            0x0003, // BLOCK_32x16,
            0x0003, // BLOCK_32x32,
            0x0003, // BLOCK_32x64,
            0x000f, // BLOCK_64x32,
            0x000f // BLOCK_64x64
        ];

        // 64 bit mask to shift and set for each uv prediction size
        private static readonly ushort[] _sizeMaskUv =
        [
            0x0001, // BLOCK_4x4
            0x0001, // BLOCK_4x8
            0x0001, // BLOCK_8x4
            0x0001, // BLOCK_8x8
            0x0001, // BLOCK_8x16,
            0x0001, // BLOCK_16x8
            0x0001, // BLOCK_16x16
            0x0011, // BLOCK_16x32,
            0x0003, // BLOCK_32x16,
            0x0033, // BLOCK_32x32,
            0x3333, // BLOCK_32x64,
            0x00ff, // BLOCK_64x32,
            0xffff // BLOCK_64x64
        ];

        private const ushort LeftBorderUv = 0x1111;
        private const ushort AboveBorderUv = 0x000f;

        private static readonly int[] _modeLfLut =
        [
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // INTRA_MODES
            1, 1, 0, 1 // INTER_MODES (ZEROMV == 0)
        ];

        private static byte GetFilterLevel(ref LoopFilterInfoN lfiN, ref ModeInfo mi)
        {
            return lfiN.Lvl[mi.SegmentId][mi.RefFrame[0]][_modeLfLut[(int)mi.Mode]];
        }

        private static Span<LoopFilterMask> GetLfm(ref Types.LoopFilter lf, int miRow, int miCol)
        {
            return lf.Lfm.AsSpan().Slice((miCol >> 3) + ((miRow >> 3) * lf.LfmStride));
        }

        // 8x8 blocks in a superblock. A "1" represents the first block in a 16x16
        // or greater area.
        private static readonly byte[][] _firstBlockIn16X16 =
        [
            [1, 0, 1, 0, 1, 0, 1, 0], [0, 0, 0, 0, 0, 0, 0, 0],
            [1, 0, 1, 0, 1, 0, 1, 0], [0, 0, 0, 0, 0, 0, 0, 0],
            [1, 0, 1, 0, 1, 0, 1, 0], [0, 0, 0, 0, 0, 0, 0, 0],
            [1, 0, 1, 0, 1, 0, 1, 0], [0, 0, 0, 0, 0, 0, 0, 0]
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
            ref LoopFilterMask lfm = ref GetLfm(ref cm.Lf, miRow, miCol)[0];
            ref ulong leftY = ref lfm.LeftY[(int)txSizeY];
            ref ulong aboveY = ref lfm.AboveY[(int)txSizeY];
            ref ulong int4X4Y = ref lfm.Int4X4Y;
            ref ushort leftUv = ref lfm.LeftUv[(int)txSizeUv];
            ref ushort aboveUv = ref lfm.AboveUv[(int)txSizeUv];
            ref ushort int4X4Uv = ref lfm.Int4X4Uv;
            int rowInSb = miRow & 7;
            int colInSb = miCol & 7;
            int shiftY = colInSb + (rowInSb << 3);
            int shiftUv = (colInSb >> 1) + ((rowInSb >> 1) << 2);
            int buildUv = _firstBlockIn16X16[rowInSb][colInSb];

            if (filterLevel == 0)
            {
                return;
            }

            int index = shiftY;

            for (int i = 0; i < bh; i++)
            {
                MemoryMarshal.CreateSpan(ref lfm.LflY[index], 64 - index).Slice(0, bw).Fill((byte)filterLevel);
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
            if (txSizeY == TxSize.Tx4X4)
            {
                int4X4Y |= _sizeMask[(int)blockSize] << shiftY;
            }

            if (buildUv != 0 && txSizeUv == TxSize.Tx4X4)
            {
                int4X4Uv |= (ushort)((_sizeMaskUv[(int)blockSize] & 0xffff) << shiftUv);
            }
        }

        private static void AdjustMask(ref Vp9Common cm, int miRow, int miCol, ref LoopFilterMask lfm)
        {
            const ulong LeftBorder = 0x1111111111111111UL;
            const ulong AboveBorder = 0x000000ff000000ffUL;
            const ushort LeftBorderUv = 0x1111;
            const ushort AboveBorderUv = 0x000f;


            // The largest loopfilter we have is 16x16 so we use the 16x16 mask
            // for 32x32 transforms also.
            lfm.LeftY[(int)TxSize.Tx16X16] |= lfm.LeftY[(int)TxSize.Tx32X32];
            lfm.AboveY[(int)TxSize.Tx16X16] |= lfm.AboveY[(int)TxSize.Tx32X32];
            lfm.LeftUv[(int)TxSize.Tx16X16] |= lfm.LeftUv[(int)TxSize.Tx32X32];
            lfm.AboveUv[(int)TxSize.Tx16X16] |= lfm.AboveUv[(int)TxSize.Tx32X32];

            // We do at least 8 tap filter on every 32x32 even if the transform size
            // is 4x4. So if the 4x4 is set on a border pixel add it to the 8x8 and
            // remove it from the 4x4.
            lfm.LeftY[(int)TxSize.Tx8X8] |= lfm.LeftY[(int)TxSize.Tx4X4] & LeftBorder;
            lfm.LeftY[(int)TxSize.Tx4X4] &= ~LeftBorder;
            lfm.AboveY[(int)TxSize.Tx8X8] |= lfm.AboveY[(int)TxSize.Tx4X4] & AboveBorder;
            lfm.AboveY[(int)TxSize.Tx4X4] &= ~AboveBorder;
            lfm.LeftUv[(int)TxSize.Tx8X8] |= (ushort)(lfm.LeftUv[(int)TxSize.Tx4X4] & LeftBorderUv);
            lfm.LeftUv[(int)TxSize.Tx4X4] &= unchecked((ushort)~LeftBorderUv);
            lfm.AboveUv[(int)TxSize.Tx8X8] |= (ushort)(lfm.AboveUv[(int)TxSize.Tx4X4] & AboveBorderUv);
            lfm.AboveUv[(int)TxSize.Tx4X4] &= unchecked((ushort)~AboveBorderUv);

            // We do some special edge handling.
            if (miRow + Constants.MiBlockSize > cm.MiRows)
            {
                int rows = cm.MiRows - miRow;

                // Each pixel inside the border gets a 1,
                ulong maskY = (1UL << (rows << 3)) - 1;
                ushort maskUv = (ushort)((1 << (((rows + 1) >> 1) << 2)) - 1);

                // Remove values completely outside our border.
                for (int i = 0; i < (int)TxSize.Tx32X32; i++)
                {
                    lfm.LeftY[i] &= maskY;
                    lfm.AboveY[i] &= maskY;
                    lfm.LeftUv[i] &= maskUv;
                    lfm.AboveUv[i] &= maskUv;
                }

                lfm.Int4X4Y &= maskY;
                lfm.Int4X4Uv &= maskUv;

                // We don't apply a wide loop filter on the last uv block row. If set
                // apply the shorter one instead.
                if (rows == 1)
                {
                    lfm.AboveUv[(int)TxSize.Tx8X8] |= lfm.AboveUv[(int)TxSize.Tx16X16];
                    lfm.AboveUv[(int)TxSize.Tx16X16] = 0;
                }

                if (rows == 5)
                {
                    lfm.AboveUv[(int)TxSize.Tx8X8] |= (ushort)(lfm.AboveUv[(int)TxSize.Tx16X16] & 0xff00);
                    lfm.AboveUv[(int)TxSize.Tx16X16] &= (ushort)~(lfm.AboveUv[(int)TxSize.Tx16X16] & 0xff00);
                }
            }

            if (miCol + Constants.MiBlockSize > cm.MiCols)
            {
                int columns = cm.MiCols - miCol;

                // Each pixel inside the border gets a 1, the multiply copies the border
                // to where we need it.
                ulong maskY = ((1UL << columns) - 1) * 0x0101010101010101UL;
                ushort maskUv = (ushort)(((1 << ((columns + 1) >> 1)) - 1) * 0x1111);

                // Internal edges are not applied on the last column of the image so
                // we mask 1 more for the internal edges
                ushort maskUvInt = (ushort)(((1 << (columns >> 1)) - 1) * 0x1111);

                // Remove the bits outside the image edge.
                for (int i = 0; i < (int)TxSize.Tx32X32; i++)
                {
                    lfm.LeftY[i] &= maskY;
                    lfm.AboveY[i] &= maskY;
                    lfm.LeftUv[i] &= maskUv;
                    lfm.AboveUv[i] &= maskUv;
                }

                lfm.Int4X4Y &= maskY;
                lfm.Int4X4Uv &= maskUvInt;

                // We don't apply a wide loop filter on the last uv column. If set
                // apply the shorter one instead.
                if (columns == 1)
                {
                    lfm.LeftUv[(int)TxSize.Tx8X8] |= lfm.LeftUv[(int)TxSize.Tx16X16];
                    lfm.LeftUv[(int)TxSize.Tx16X16] = 0;
                }

                if (columns == 5)
                {
                    lfm.LeftUv[(int)TxSize.Tx8X8] |= (ushort)(lfm.LeftUv[(int)TxSize.Tx16X16] & 0xcccc);
                    lfm.LeftUv[(int)TxSize.Tx16X16] &= (ushort)~(lfm.LeftUv[(int)TxSize.Tx16X16] & 0xcccc);
                }
            }

            // We don't apply a loop filter on the first column in the image, mask that
            // out.
            if (miCol == 0)
            {
                for (int i = 0; i < (int)TxSize.Tx32X32; i++)
                {
                    lfm.LeftY[i] &= 0xfefefefefefefefeUL;
                    lfm.LeftUv[i] &= 0xeeee;
                }
            }

            // Assert if we try to apply 2 different loop filters at the same position.
            Debug.Assert((lfm.LeftY[(int)TxSize.Tx16X16] & lfm.LeftY[(int)TxSize.Tx8X8]) == 0);
            Debug.Assert((lfm.LeftY[(int)TxSize.Tx16X16] & lfm.LeftY[(int)TxSize.Tx4X4]) == 0);
            Debug.Assert((lfm.LeftY[(int)TxSize.Tx8X8] & lfm.LeftY[(int)TxSize.Tx4X4]) == 0);
            Debug.Assert((lfm.Int4X4Y & lfm.LeftY[(int)TxSize.Tx16X16]) == 0);
            Debug.Assert((lfm.LeftUv[(int)TxSize.Tx16X16] & lfm.LeftUv[(int)TxSize.Tx8X8]) == 0);
            Debug.Assert((lfm.LeftUv[(int)TxSize.Tx16X16] & lfm.LeftUv[(int)TxSize.Tx4X4]) == 0);
            Debug.Assert((lfm.LeftUv[(int)TxSize.Tx8X8] & lfm.LeftUv[(int)TxSize.Tx4X4]) == 0);
            Debug.Assert((lfm.Int4X4Uv & lfm.LeftUv[(int)TxSize.Tx16X16]) == 0);
            Debug.Assert((lfm.AboveY[(int)TxSize.Tx16X16] & lfm.AboveY[(int)TxSize.Tx8X8]) == 0);
            Debug.Assert((lfm.AboveY[(int)TxSize.Tx16X16] & lfm.AboveY[(int)TxSize.Tx4X4]) == 0);
            Debug.Assert((lfm.AboveY[(int)TxSize.Tx8X8] & lfm.AboveY[(int)TxSize.Tx4X4]) == 0);
            Debug.Assert((lfm.Int4X4Y & lfm.AboveY[(int)TxSize.Tx16X16]) == 0);
            Debug.Assert((lfm.AboveUv[(int)TxSize.Tx16X16] & lfm.AboveUv[(int)TxSize.Tx8X8]) == 0);
            Debug.Assert((lfm.AboveUv[(int)TxSize.Tx16X16] & lfm.AboveUv[(int)TxSize.Tx4X4]) == 0);
            Debug.Assert((lfm.AboveUv[(int)TxSize.Tx8X8] & lfm.AboveUv[(int)TxSize.Tx4X4]) == 0);
            Debug.Assert((lfm.Int4X4Uv & lfm.AboveUv[(int)TxSize.Tx16X16]) == 0);
        }

        public static unsafe void ResetLfm(ref Vp9Common cm)
        {
            if (cm.Lf.FilterLevel != 0)
            {
                MemoryUtil.Fill(cm.Lf.Lfm.ToPointer(), new LoopFilterMask(),
                    ((cm.MiRows + (Constants.MiBlockSize - 1)) >> 3) * cm.Lf.LfmStride);
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
                    if (blockInsideLimit > 9 - sharpnessLvl)
                    {
                        blockInsideLimit = 9 - sharpnessLvl;
                    }
                }

                if (blockInsideLimit < 1)
                {
                    blockInsideLimit = 1;
                }

                lfi.Lfthr[lvl].Lim.AsSpan().Fill((byte)blockInsideLimit);
                lfi.Lfthr[lvl].Mblim.AsSpan().Fill((byte)((2 * (lvl + 2)) + blockInsideLimit));
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
                if (seg.IsSegFeatureActive(segId, SegLvlFeatures.AltLf) != 0)
                {
                    int data = seg.GetSegData(segId, SegLvlFeatures.AltLf);
                    lvlSeg = Math.Clamp(seg.AbsDelta == Constants.SegmentAbsData ? data : defaultFiltLvl + data, 0,
                        MaxLoopFilter);
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
                    int intraLvl = lvlSeg + (lf.RefDeltas[Constants.IntraFrame] * scale);
                    lfi.Lvl[segId][Constants.IntraFrame][0] = (byte)Math.Clamp(intraLvl, 0, MaxLoopFilter);

                    for (refr = Constants.LastFrame; refr < Constants.MaxRefFrames; ++refr)
                    {
                        for (mode = 0; mode < MaxModeLfDeltas; ++mode)
                        {
                            int interLvl = lvlSeg + (lf.RefDeltas[refr] * scale) + (lf.ModeDeltas[mode] * scale);
                            lfi.Lvl[segId][refr][mode] = (byte)Math.Clamp(interLvl, 0, MaxLoopFilter);
                        }
                    }
                }
            }
        }

        private static void FilterSelectivelyVertRow2(
            int subsamplingFactor,
            ArrayPtr<byte> s,
            int pitch,
            uint mask16X16,
            uint mask8X8,
            uint mask4X4,
            uint mask4X4Int,
            ReadOnlySpan<LoopFilterThresh> lfthr,
            ReadOnlySpan<byte> lfl)
        {
            uint dualMaskCutoff = subsamplingFactor != 0 ? 0xffu : 0xffffu;
            int lflForward = subsamplingFactor != 0 ? 4 : 8;
            uint dualOne = 1u | (1u << lflForward);
            Span<ArrayPtr<byte>> ss = stackalloc ArrayPtr<byte>[2];
            Span<LoopFilterThresh> lfis = stackalloc LoopFilterThresh[2];
            ss[0] = s;

            for (uint mask = (mask16X16 | mask8X8 | mask4X4 | mask4X4Int) & dualMaskCutoff;
                 mask != 0;
                 mask = (mask & ~dualOne) >> 1)
            {
                if ((mask & dualOne) != 0)
                {
                    lfis[0] = lfthr[lfl[0]];
                    lfis[1] = lfthr[lfl[lflForward]];
                    ss[1] = ss[0].Slice(8 * pitch);

                    if ((mask16X16 & dualOne) != 0)
                    {
                        if ((mask16X16 & dualOne) == dualOne)
                        {
                            LoopFilterAuto.LpfVertical16Dual(ss[0], pitch, lfis[0].Mblim.AsSpan(), lfis[0].Lim.AsSpan(),
                                lfis[0].HevThr.AsSpan());
                        }
                        else
                        {
                            ref LoopFilterThresh lfi = ref lfis[(mask16X16 & 1) == 0 ? 1 : 0];
                            LoopFilterAuto.LpfVertical16(ss[(mask16X16 & 1) == 0 ? 1 : 0], pitch, lfi.Mblim.AsSpan(),
                                lfi.Lim.AsSpan(), lfi.HevThr.AsSpan());
                        }
                    }

                    if ((mask8X8 & dualOne) != 0)
                    {
                        if ((mask8X8 & dualOne) == dualOne)
                        {
                            LoopFilterAuto.LpfVertical8Dual(
                                ss[0],
                                pitch,
                                lfis[0].Mblim.AsSpan(),
                                lfis[0].Lim.AsSpan(),
                                lfis[0].HevThr.AsSpan(),
                                lfis[1].Mblim.AsSpan(),
                                lfis[1].Lim.AsSpan(),
                                lfis[1].HevThr.AsSpan());
                        }
                        else
                        {
                            ref LoopFilterThresh lfi = ref lfis[(mask8X8 & 1) == 0 ? 1 : 0];
                            LoopFilterAuto.LpfVertical8(
                                ss[(mask8X8 & 1) == 0 ? 1 : 0],
                                pitch,
                                lfi.Mblim.AsSpan(),
                                lfi.Lim.AsSpan(),
                                lfi.HevThr.AsSpan());
                        }
                    }

                    if ((mask4X4 & dualOne) != 0)
                    {
                        if ((mask4X4 & dualOne) == dualOne)
                        {
                            LoopFilterAuto.LpfVertical4Dual(
                                ss[0],
                                pitch,
                                lfis[0].Mblim.AsSpan(),
                                lfis[0].Lim.AsSpan(),
                                lfis[0].HevThr.AsSpan(),
                                lfis[1].Mblim.AsSpan(),
                                lfis[1].Lim.AsSpan(),
                                lfis[1].HevThr.AsSpan());
                        }
                        else
                        {
                            ref LoopFilterThresh lfi = ref lfis[(mask4X4 & 1) == 0 ? 1 : 0];
                            LoopFilterAuto.LpfVertical4(ss[(mask4X4 & 1) == 0 ? 1 : 0], pitch, lfi.Mblim.AsSpan(),
                                lfi.Lim.AsSpan(), lfi.HevThr.AsSpan());
                        }
                    }

                    if ((mask4X4Int & dualOne) != 0)
                    {
                        if ((mask4X4Int & dualOne) == dualOne)
                        {
                            LoopFilterAuto.LpfVertical4Dual(
                                ss[0].Slice(4),
                                pitch,
                                lfis[0].Mblim.AsSpan(),
                                lfis[0].Lim.AsSpan(),
                                lfis[0].HevThr.AsSpan(),
                                lfis[1].Mblim.AsSpan(),
                                lfis[1].Lim.AsSpan(),
                                lfis[1].HevThr.AsSpan());
                        }
                        else
                        {
                            ref LoopFilterThresh lfi = ref lfis[(mask4X4Int & 1) == 0 ? 1 : 0];
                            LoopFilterAuto.LpfVertical4(ss[(mask4X4Int & 1) == 0 ? 1 : 0].Slice(4), pitch,
                                lfi.Mblim.AsSpan(), lfi.Lim.AsSpan(), lfi.HevThr.AsSpan());
                        }
                    }
                }

                ss[0] = ss[0].Slice(8);
                lfl = lfl.Slice(1);
                mask16X16 >>= 1;
                mask8X8 >>= 1;
                mask4X4 >>= 1;
                mask4X4Int >>= 1;
            }
        }

        private static void HighbdFilterSelectivelyVertRow2(
            int subsamplingFactor,
            ArrayPtr<ushort> s,
            int pitch,
            uint mask16X16,
            uint mask8X8,
            uint mask4X4,
            uint mask4X4Int,
            ReadOnlySpan<LoopFilterThresh> lfthr,
            ReadOnlySpan<byte> lfl,
            int bd)
        {
            uint dualMaskCutoff = subsamplingFactor != 0 ? 0xffu : 0xffffu;
            int lflForward = subsamplingFactor != 0 ? 4 : 8;
            uint dualOne = 1u | (1u << lflForward);
            Span<ArrayPtr<ushort>> ss = stackalloc ArrayPtr<ushort>[2];
            Span<LoopFilterThresh> lfis = stackalloc LoopFilterThresh[2];
            ss[0] = s;

            for (uint mask = (mask16X16 | mask8X8 | mask4X4 | mask4X4Int) & dualMaskCutoff;
                 mask != 0;
                 mask = (mask & ~dualOne) >> 1)
            {
                if ((mask & dualOne) != 0)
                {
                    lfis[0] = lfthr[lfl[0]];
                    lfis[1] = lfthr[lfl[lflForward]];
                    ss[1] = ss[0].Slice(8 * pitch);

                    if ((mask16X16 & dualOne) != 0)
                    {
                        if ((mask16X16 & dualOne) == dualOne)
                        {
                            LoopFilterScalar.HighBdLpfVertical16Dual(ss[0], pitch, lfis[0].Mblim[0], lfis[0].Lim[0],
                                lfis[0].HevThr[0], bd);
                        }
                        else
                        {
                            ref LoopFilterThresh lfi = ref lfis[(mask16X16 & 1) == 0 ? 1 : 0];
                            LoopFilterScalar.HighBdLpfVertical16(ss[(mask16X16 & 1) == 0 ? 1 : 0], pitch, lfi.Mblim[0],
                                lfi.Lim[0], lfi.HevThr[0], bd);
                        }
                    }

                    if ((mask8X8 & dualOne) != 0)
                    {
                        if ((mask8X8 & dualOne) == dualOne)
                        {
                            LoopFilterScalar.HighBdLpfVertical8Dual(
                                ss[0],
                                pitch,
                                lfis[0].Mblim[0],
                                lfis[0].Lim[0],
                                lfis[0].HevThr[0],
                                lfis[1].Mblim[0],
                                lfis[1].Lim[0],
                                lfis[1].HevThr[0],
                                bd);
                        }
                        else
                        {
                            ref LoopFilterThresh lfi = ref lfis[(mask8X8 & 1) == 0 ? 1 : 0];
                            LoopFilterScalar.HighBdLpfVertical8(
                                ss[(mask8X8 & 1) == 0 ? 1 : 0],
                                pitch,
                                lfi.Mblim[0],
                                lfi.Lim[0],
                                lfi.HevThr[0],
                                bd);
                        }
                    }

                    if ((mask4X4 & dualOne) != 0)
                    {
                        if ((mask4X4 & dualOne) == dualOne)
                        {
                            LoopFilterScalar.HighBdLpfVertical4Dual(
                                ss[0],
                                pitch,
                                lfis[0].Mblim[0],
                                lfis[0].Lim[0],
                                lfis[0].HevThr[0],
                                lfis[1].Mblim[0],
                                lfis[1].Lim[0],
                                lfis[1].HevThr[0],
                                bd);
                        }
                        else
                        {
                            ref LoopFilterThresh lfi = ref lfis[(mask4X4 & 1) == 0 ? 1 : 0];
                            LoopFilterScalar.HighBdLpfVertical4(ss[(mask4X4 & 1) == 0 ? 1 : 0], pitch, lfi.Mblim[0],
                                lfi.Lim[0], lfi.HevThr[0], bd);
                        }
                    }

                    if ((mask4X4Int & dualOne) != 0)
                    {
                        if ((mask4X4Int & dualOne) == dualOne)
                        {
                            LoopFilterScalar.HighBdLpfVertical4Dual(
                                ss[0].Slice(4),
                                pitch,
                                lfis[0].Mblim[0],
                                lfis[0].Lim[0],
                                lfis[0].HevThr[0],
                                lfis[1].Mblim[0],
                                lfis[1].Lim[0],
                                lfis[1].HevThr[0],
                                bd);
                        }
                        else
                        {
                            ref LoopFilterThresh lfi = ref lfis[(mask4X4Int & 1) == 0 ? 1 : 0];
                            LoopFilterScalar.HighBdLpfVertical4(ss[(mask4X4Int & 1) == 0 ? 1 : 0].Slice(4), pitch,
                                lfi.Mblim[0], lfi.Lim[0], lfi.HevThr[0], bd);
                        }
                    }
                }

                ss[0] = ss[0].Slice(8);
                lfl = lfl.Slice(1);
                mask16X16 >>= 1;
                mask8X8 >>= 1;
                mask4X4 >>= 1;
                mask4X4Int >>= 1;
            }
        }

        private static void FilterSelectivelyHoriz(
            ArrayPtr<byte> s,
            int pitch,
            uint mask16X16,
            uint mask8X8,
            uint mask4X4,
            uint mask4X4Int,
            ReadOnlySpan<LoopFilterThresh> lfthr,
            ReadOnlySpan<byte> lfl)
        {
            int count;

            for (uint mask = mask16X16 | mask8X8 | mask4X4 | mask4X4Int; mask != 0; mask >>= count)
            {
                count = 1;
                if ((mask & 1) != 0)
                {
                    LoopFilterThresh lfi = lfthr[lfl[0]];

                    if ((mask16X16 & 1) != 0)
                    {
                        if ((mask16X16 & 3) == 3)
                        {
                            LoopFilterAuto.LpfHorizontal16Dual(s, pitch, lfi.Mblim.AsSpan(), lfi.Lim.AsSpan(),
                                lfi.HevThr.AsSpan());
                            count = 2;
                        }
                        else
                        {
                            LoopFilterAuto.LpfHorizontal16(s, pitch, lfi.Mblim.AsSpan(), lfi.Lim.AsSpan(),
                                lfi.HevThr.AsSpan());
                        }
                    }
                    else if ((mask8X8 & 1) != 0)
                    {
                        if ((mask8X8 & 3) == 3)
                        {
                            // Next block's thresholds.
                            LoopFilterThresh lfin = lfthr[lfl[1]];

                            LoopFilterAuto.LpfHorizontal8Dual(
                                s,
                                pitch,
                                lfi.Mblim.AsSpan(),
                                lfi.Lim.AsSpan(),
                                lfi.HevThr.AsSpan(),
                                lfin.Mblim.AsSpan(),
                                lfin.Lim.AsSpan(),
                                lfin.HevThr.AsSpan());

                            if ((mask4X4Int & 3) == 3)
                            {
                                LoopFilterAuto.LpfHorizontal4Dual(
                                    s.Slice(4 * pitch),
                                    pitch,
                                    lfi.Mblim.AsSpan(),
                                    lfi.Lim.AsSpan(),
                                    lfi.HevThr.AsSpan(),
                                    lfin.Mblim.AsSpan(),
                                    lfin.Lim.AsSpan(),
                                    lfin.HevThr.AsSpan());
                            }
                            else if ((mask4X4Int & 1) != 0)
                            {
                                LoopFilterAuto.LpfHorizontal4(s.Slice(4 * pitch), pitch, lfi.Mblim.AsSpan(),
                                    lfi.Lim.AsSpan(), lfi.HevThr.AsSpan());
                            }
                            else if ((mask4X4Int & 2) != 0)
                            {
                                LoopFilterAuto.LpfHorizontal4(s.Slice(8 + (4 * pitch)), pitch, lfin.Mblim.AsSpan(),
                                    lfin.Lim.AsSpan(), lfin.HevThr.AsSpan());
                            }

                            count = 2;
                        }
                        else
                        {
                            LoopFilterAuto.LpfHorizontal8(s, pitch, lfi.Mblim.AsSpan(), lfi.Lim.AsSpan(),
                                lfi.HevThr.AsSpan());

                            if ((mask4X4Int & 1) != 0)
                            {
                                LoopFilterAuto.LpfHorizontal4(s.Slice(4 * pitch), pitch, lfi.Mblim.AsSpan(),
                                    lfi.Lim.AsSpan(), lfi.HevThr.AsSpan());
                            }
                        }
                    }
                    else if ((mask4X4 & 1) != 0)
                    {
                        if ((mask4X4 & 3) == 3)
                        {
                            // Next block's thresholds.
                            LoopFilterThresh lfin = lfthr[lfl[1]];

                            LoopFilterAuto.LpfHorizontal4Dual(
                                s,
                                pitch,
                                lfi.Mblim.AsSpan(),
                                lfi.Lim.AsSpan(),
                                lfi.HevThr.AsSpan(),
                                lfin.Mblim.AsSpan(),
                                lfin.Lim.AsSpan(),
                                lfin.HevThr.AsSpan());

                            if ((mask4X4Int & 3) == 3)
                            {
                                LoopFilterAuto.LpfHorizontal4Dual(
                                    s.Slice(4 * pitch),
                                    pitch,
                                    lfi.Mblim.AsSpan(),
                                    lfi.Lim.AsSpan(),
                                    lfi.HevThr.AsSpan(),
                                    lfin.Mblim.AsSpan(),
                                    lfin.Lim.AsSpan(),
                                    lfin.HevThr.AsSpan());
                            }
                            else if ((mask4X4Int & 1) != 0)
                            {
                                LoopFilterAuto.LpfHorizontal4(s.Slice(4 * pitch), pitch, lfi.Mblim.AsSpan(),
                                    lfi.Lim.AsSpan(), lfi.HevThr.AsSpan());
                            }
                            else if ((mask4X4Int & 2) != 0)
                            {
                                LoopFilterAuto.LpfHorizontal4(s.Slice(8 + (4 * pitch)), pitch, lfin.Mblim.AsSpan(),
                                    lfin.Lim.AsSpan(), lfin.HevThr.AsSpan());
                            }

                            count = 2;
                        }
                        else
                        {
                            LoopFilterAuto.LpfHorizontal4(s, pitch, lfi.Mblim.AsSpan(), lfi.Lim.AsSpan(),
                                lfi.HevThr.AsSpan());

                            if ((mask4X4Int & 1) != 0)
                            {
                                LoopFilterAuto.LpfHorizontal4(s.Slice(4 * pitch), pitch, lfi.Mblim.AsSpan(),
                                    lfi.Lim.AsSpan(), lfi.HevThr.AsSpan());
                            }
                        }
                    }
                    else
                    {
                        LoopFilterAuto.LpfHorizontal4(s.Slice(4 * pitch), pitch, lfi.Mblim.AsSpan(), lfi.Lim.AsSpan(),
                            lfi.HevThr.AsSpan());
                    }
                }

                s = s.Slice(8 * count);
                lfl = lfl.Slice(count);
                mask16X16 >>= count;
                mask8X8 >>= count;
                mask4X4 >>= count;
                mask4X4Int >>= count;
            }
        }

        private static void HighbdFilterSelectivelyHoriz(
            ArrayPtr<ushort> s,
            int pitch,
            uint mask16X16,
            uint mask8X8,
            uint mask4X4,
            uint mask4X4Int,
            ReadOnlySpan<LoopFilterThresh> lfthr,
            ReadOnlySpan<byte> lfl,
            int bd)
        {
            int count;

            for (uint mask = mask16X16 | mask8X8 | mask4X4 | mask4X4Int; mask != 0; mask >>= count)
            {
                count = 1;
                if ((mask & 1) != 0)
                {
                    LoopFilterThresh lfi = lfthr[lfl[0]];

                    if ((mask16X16 & 1) != 0)
                    {
                        if ((mask16X16 & 3) == 3)
                        {
                            LoopFilterScalar.HighBdLpfHorizontal16Dual(s, pitch, lfi.Mblim[0], lfi.Lim[0],
                                lfi.HevThr[0], bd);
                            count = 2;
                        }
                        else
                        {
                            LoopFilterScalar.HighBdLpfHorizontal16(s, pitch, lfi.Mblim[0], lfi.Lim[0], lfi.HevThr[0],
                                bd);
                        }
                    }
                    else if ((mask8X8 & 1) != 0)
                    {
                        if ((mask8X8 & 3) == 3)
                        {
                            // Next block's thresholds.
                            LoopFilterThresh lfin = lfthr[lfl[1]];

                            LoopFilterScalar.HighBdLpfHorizontal8Dual(
                                s,
                                pitch,
                                lfi.Mblim[0],
                                lfi.Lim[0],
                                lfi.HevThr[0],
                                lfin.Mblim[0],
                                lfin.Lim[0],
                                lfin.HevThr[0],
                                bd);

                            if ((mask4X4Int & 3) == 3)
                            {
                                LoopFilterScalar.HighBdLpfHorizontal4Dual(
                                    s.Slice(4 * pitch),
                                    pitch,
                                    lfi.Mblim[0],
                                    lfi.Lim[0],
                                    lfi.HevThr[0],
                                    lfin.Mblim[0],
                                    lfin.Lim[0],
                                    lfin.HevThr[0],
                                    bd);
                            }
                            else if ((mask4X4Int & 1) != 0)
                            {
                                LoopFilterScalar.HighBdLpfHorizontal4(s.Slice(4 * pitch), pitch, lfi.Mblim[0],
                                    lfi.Lim[0], lfi.HevThr[0], bd);
                            }
                            else if ((mask4X4Int & 2) != 0)
                            {
                                LoopFilterScalar.HighBdLpfHorizontal4(s.Slice(8 + (4 * pitch)), pitch, lfin.Mblim[0],
                                    lfin.Lim[0], lfin.HevThr[0], bd);
                            }

                            count = 2;
                        }
                        else
                        {
                            LoopFilterScalar.HighBdLpfHorizontal8(s, pitch, lfi.Mblim[0], lfi.Lim[0], lfi.HevThr[0],
                                bd);

                            if ((mask4X4Int & 1) != 0)
                            {
                                LoopFilterScalar.HighBdLpfHorizontal4(s.Slice(4 * pitch), pitch, lfi.Mblim[0],
                                    lfi.Lim[0], lfi.HevThr[0], bd);
                            }
                        }
                    }
                    else if ((mask4X4 & 1) != 0)
                    {
                        if ((mask4X4 & 3) == 3)
                        {
                            // Next block's thresholds.
                            LoopFilterThresh lfin = lfthr[lfl[1]];

                            LoopFilterScalar.HighBdLpfHorizontal4Dual(
                                s,
                                pitch,
                                lfi.Mblim[0],
                                lfi.Lim[0],
                                lfi.HevThr[0],
                                lfin.Mblim[0],
                                lfin.Lim[0],
                                lfin.HevThr[0],
                                bd);

                            if ((mask4X4Int & 3) == 3)
                            {
                                LoopFilterScalar.HighBdLpfHorizontal4Dual(
                                    s.Slice(4 * pitch),
                                    pitch,
                                    lfi.Mblim[0],
                                    lfi.Lim[0],
                                    lfi.HevThr[0],
                                    lfin.Mblim[0],
                                    lfin.Lim[0],
                                    lfin.HevThr[0],
                                    bd);
                            }
                            else if ((mask4X4Int & 1) != 0)
                            {
                                LoopFilterScalar.HighBdLpfHorizontal4(s.Slice(4 * pitch), pitch, lfi.Mblim[0],
                                    lfi.Lim[0], lfi.HevThr[0], bd);
                            }
                            else if ((mask4X4Int & 2) != 0)
                            {
                                LoopFilterScalar.HighBdLpfHorizontal4(s.Slice(8 + (4 * pitch)), pitch, lfin.Mblim[0],
                                    lfin.Lim[0], lfin.HevThr[0], bd);
                            }

                            count = 2;
                        }
                        else
                        {
                            LoopFilterScalar.HighBdLpfHorizontal4(s, pitch, lfi.Mblim[0], lfi.Lim[0], lfi.HevThr[0],
                                bd);

                            if ((mask4X4Int & 1) != 0)
                            {
                                LoopFilterScalar.HighBdLpfHorizontal4(s.Slice(4 * pitch), pitch, lfi.Mblim[0],
                                    lfi.Lim[0], lfi.HevThr[0], bd);
                            }
                        }
                    }
                    else
                    {
                        LoopFilterScalar.HighBdLpfHorizontal4(s.Slice(4 * pitch), pitch, lfi.Mblim[0], lfi.Lim[0],
                            lfi.HevThr[0], bd);
                    }
                }

                s = s.Slice(8 * count);
                lfl = lfl.Slice(count);
                mask16X16 >>= count;
                mask8X8 >>= count;
                mask4X4 >>= count;
                mask4X4Int >>= count;
            }
        }

        private static void FilterSelectivelyVert(
            ArrayPtr<byte> s,
            int pitch,
            uint mask16X16,
            uint mask8X8,
            uint mask4X4,
            uint mask4X4Int,
            ReadOnlySpan<LoopFilterThresh> lfthr,
            ReadOnlySpan<byte> lfl)
        {
            for (uint mask = mask16X16 | mask8X8 | mask4X4 | mask4X4Int; mask != 0; mask >>= 1)
            {
                LoopFilterThresh lfi = lfthr[lfl[0]];

                if ((mask & 1) != 0)
                {
                    if ((mask16X16 & 1) != 0)
                    {
                        LoopFilterAuto.LpfVertical16(s, pitch, lfi.Mblim.AsSpan(), lfi.Lim.AsSpan(),
                            lfi.HevThr.AsSpan());
                    }
                    else if ((mask8X8 & 1) != 0)
                    {
                        LoopFilterAuto.LpfVertical8(s, pitch, lfi.Mblim.AsSpan(), lfi.Lim.AsSpan(),
                            lfi.HevThr.AsSpan());
                    }
                    else if ((mask4X4 & 1) != 0)
                    {
                        LoopFilterAuto.LpfVertical4(s, pitch, lfi.Mblim.AsSpan(), lfi.Lim.AsSpan(),
                            lfi.HevThr.AsSpan());
                    }
                }

                if ((mask4X4Int & 1) != 0)
                {
                    LoopFilterAuto.LpfVertical4(s.Slice(4), pitch, lfi.Mblim.AsSpan(), lfi.Lim.AsSpan(),
                        lfi.HevThr.AsSpan());
                }

                s = s.Slice(8);
                lfl = lfl.Slice(1);
                mask16X16 >>= 1;
                mask8X8 >>= 1;
                mask4X4 >>= 1;
                mask4X4Int >>= 1;
            }
        }

        private static void HighbdFilterSelectivelyVert(
            ArrayPtr<ushort> s,
            int pitch,
            uint mask16X16,
            uint mask8X8,
            uint mask4X4,
            uint mask4X4Int,
            ReadOnlySpan<LoopFilterThresh> lfthr,
            ReadOnlySpan<byte> lfl,
            int bd)
        {
            for (uint mask = mask16X16 | mask8X8 | mask4X4 | mask4X4Int; mask != 0; mask >>= 1)
            {
                LoopFilterThresh lfi = lfthr[lfl[0]];

                if ((mask & 1) != 0)
                {
                    if ((mask16X16 & 1) != 0)
                    {
                        LoopFilterScalar.HighBdLpfVertical16(s, pitch, lfi.Mblim[0], lfi.Lim[0], lfi.HevThr[0], bd);
                    }
                    else if ((mask8X8 & 1) != 0)
                    {
                        LoopFilterScalar.HighBdLpfVertical8(s, pitch, lfi.Mblim[0], lfi.Lim[0], lfi.HevThr[0], bd);
                    }
                    else if ((mask4X4 & 1) != 0)
                    {
                        LoopFilterScalar.HighBdLpfVertical4(s, pitch, lfi.Mblim[0], lfi.Lim[0], lfi.HevThr[0], bd);
                    }
                }

                if ((mask4X4Int & 1) != 0)
                {
                    LoopFilterScalar.HighBdLpfVertical4(s.Slice(4), pitch, lfi.Mblim[0], lfi.Lim[0], lfi.HevThr[0], bd);
                }

                s = s.Slice(8);
                lfl = lfl.Slice(1);
                mask16X16 >>= 1;
                mask8X8 >>= 1;
                mask4X4 >>= 1;
                mask4X4Int >>= 1;
            }
        }

        private static readonly byte[] _num4X4BlocksWideLookup = [1, 1, 2, 2, 2, 4, 4, 4, 8, 8, 8, 16, 16];
        private static readonly byte[] _num4X4BlocksHighLookup = [1, 2, 1, 2, 4, 2, 4, 8, 4, 8, 16, 8, 16];
        private static readonly byte[] _num8X8BlocksWideLookup = [1, 1, 1, 1, 1, 2, 2, 2, 4, 4, 4, 8, 8];
        private static readonly byte[] _num8X8BlocksHighLookup = [1, 1, 1, 1, 2, 1, 2, 4, 2, 4, 8, 4, 8];

        private static void FilterBlockPlaneNon420(
            ref Vp9Common cm,
            ref MacroBlockDPlane plane,
            ArrayPtr<Ptr<ModeInfo>> mi8X8,
            int miRow,
            int miCol)
        {
            int ssX = plane.SubsamplingX;
            int ssY = plane.SubsamplingY;
            int rowStep = 1 << ssY;
            int colStep = 1 << ssX;
            int rowStepStride = cm.MiStride * rowStep;
            ref Buf2D dst = ref plane.Dst;
            ArrayPtr<byte> dst0 = dst.Buf;
            Span<int> mask16X16 = stackalloc int[Constants.MiBlockSize];
            Span<int> mask8X8 = stackalloc int[Constants.MiBlockSize];
            Span<int> mask4X4 = stackalloc int[Constants.MiBlockSize];
            Span<int> mask4X4Int = stackalloc int[Constants.MiBlockSize];
            Span<byte> lfl = stackalloc byte[Constants.MiBlockSize * Constants.MiBlockSize];


            for (int r = 0; r < Constants.MiBlockSize && miRow + r < cm.MiRows; r += rowStep)
            {
                uint mask16X16C = 0;
                uint mask8X8C = 0;
                uint mask4X4C = 0;
                uint borderMask;

                // Determine the vertical edges that need filtering
                for (int c = 0; c < Constants.MiBlockSize && miCol + c < cm.MiCols; c += colStep)
                {
                    ref ModeInfo mi = ref mi8X8[c].Value;
                    BlockSize sbType = mi.SbType;
                    bool skipThis = mi.Skip != 0 && mi.IsInterBlock();
                    // left edge of current unit is block/partition edge -> no skip
                    bool blockEdgeLeft = _num4X4BlocksWideLookup[(int)sbType] <= 1 || (c & (_num8X8BlocksWideLookup[(int)sbType] - 1)) == 0;
                    bool skipThisC = skipThis && !blockEdgeLeft;
                    // top edge of current unit is block/partition edge -> no skip
                    bool blockEdgeAbove = _num4X4BlocksHighLookup[(int)sbType] <= 1 || (r & (_num8X8BlocksHighLookup[(int)sbType] - 1)) == 0;
                    bool skipThisR = skipThis && !blockEdgeAbove;
                    TxSize txSize = mi.GetUvTxSize(ref plane);
                    bool skipBorder4X4C = ssX != 0 && miCol + c == cm.MiCols - 1;
                    bool skipBorder4X4R = ssY != 0 && miRow + r == cm.MiRows - 1;

                    // Filter level can vary per MI
                    if ((lfl[(r << 3) + (c >> ssX)] = GetFilterLevel(ref cm.LfInfo, ref mi)) == 0)
                    {
                        continue;
                    }

                    // Build masks based on the transform size of each block
                    if (txSize == TxSize.Tx32X32)
                    {
                        if (!skipThisC && ((c >> ssX) & 3) == 0)
                        {
                            if (!skipBorder4X4C)
                            {
                                mask16X16C |= 1u << (c >> ssX);
                            }
                            else
                            {
                                mask8X8C |= 1u << (c >> ssX);
                            }
                        }

                        if (!skipThisR && ((r >> ssY) & 3) == 0)
                        {
                            if (!skipBorder4X4R)
                            {
                                mask16X16[r] |= 1 << (c >> ssX);
                            }
                            else
                            {
                                mask8X8[r] |= 1 << (c >> ssX);
                            }
                        }
                    }
                    else if (txSize == TxSize.Tx16X16)
                    {
                        if (!skipThisC && ((c >> ssX) & 1) == 0)
                        {
                            if (!skipBorder4X4C)
                            {
                                mask16X16C |= 1u << (c >> ssX);
                            }
                            else
                            {
                                mask8X8C |= 1u << (c >> ssX);
                            }
                        }

                        if (!skipThisR && ((r >> ssY) & 1) == 0)
                        {
                            if (!skipBorder4X4R)
                            {
                                mask16X16[r] |= 1 << (c >> ssX);
                            }
                            else
                            {
                                mask8X8[r] |= 1 << (c >> ssX);
                            }
                        }
                    }
                    else
                    {
                        // force 8x8 filtering on 32x32 boundaries
                        if (!skipThisC)
                        {
                            if (txSize == TxSize.Tx8X8 || ((c >> ssX) & 3) == 0)
                            {
                                mask8X8C |= 1u << (c >> ssX);
                            }
                            else
                            {
                                mask4X4C |= 1u << (c >> ssX);
                            }
                        }

                        if (!skipThisR)
                        {
                            if (txSize == TxSize.Tx8X8 || ((r >> ssY) & 3) == 0)
                            {
                                mask8X8[r] |= 1 << (c >> ssX);
                            }
                            else
                            {
                                mask4X4[r] |= 1 << (c >> ssX);
                            }
                        }

                        if (!skipThis && txSize < TxSize.Tx8X8 && !skipBorder4X4C)
                        {
                            mask4X4Int[r] |= 1 << (c >> ssX);
                        }
                    }
                }

                // Disable filtering on the leftmost column
                borderMask = ~(miCol == 0 ? 1u : 0u);

                if (cm.UseHighBitDepth)
                {
                    HighbdFilterSelectivelyVert(
                        ConvertToUshortPtr(dst.Buf),
                        dst.Stride,
                        mask16X16C & borderMask,
                        mask8X8C & borderMask,
                        mask4X4C & borderMask,
                        (uint)mask4X4Int[r],
                        cm.LfInfo.Lfthr.AsSpan(),
                        lfl.Slice(r << 3),
                        (int)cm.BitDepth);
                }
                else
                {
                    FilterSelectivelyVert(
                        dst.Buf,
                        dst.Stride,
                        mask16X16C & borderMask,
                        mask8X8C & borderMask,
                        mask4X4C & borderMask,
                        (uint)mask4X4Int[r],
                        cm.LfInfo.Lfthr.AsSpan(),
                        lfl.Slice(r << 3));
                }

                dst.Buf = dst.Buf.Slice(8 * dst.Stride);
                mi8X8 = mi8X8.Slice(rowStepStride);
            }

            // Now do horizontal pass
            dst.Buf = dst0;
            for (int r = 0; r < Constants.MiBlockSize && miRow + r < cm.MiRows; r += rowStep)
            {
                bool skipBorder4X4R = ssY != 0 && miRow + r == cm.MiRows - 1;
                uint mask4X4IntR = skipBorder4X4R ? 0u : (uint)mask4X4Int[r];

                uint mask16X16R;
                uint mask8X8R;
                uint mask4X4R;

                if (miRow + r == 0)
                {
                    mask16X16R = 0;
                    mask8X8R = 0;
                    mask4X4R = 0;
                }
                else
                {
                    mask16X16R = (uint)mask16X16[r];
                    mask8X8R = (uint)mask8X8[r];
                    mask4X4R = (uint)mask4X4[r];
                }

                if (cm.UseHighBitDepth)
                {
                    HighbdFilterSelectivelyHoriz(
                        ConvertToUshortPtr(dst.Buf),
                        dst.Stride,
                        mask16X16R,
                        mask8X8R,
                        mask4X4R,
                        mask4X4IntR,
                        cm.LfInfo.Lfthr.AsSpan(),
                        lfl.Slice(r << 3),
                        (int)cm.BitDepth);
                }
                else
                {
                    FilterSelectivelyHoriz(
                        dst.Buf,
                        dst.Stride,
                        mask16X16R,
                        mask8X8R,
                        mask4X4R,
                        mask4X4IntR,
                        cm.LfInfo.Lfthr.AsSpan(),
                        lfl.Slice(r << 3));
                }

                dst.Buf = dst.Buf.Slice(8 * dst.Stride);
            }
        }

        private static void FilterBlockPlaneSs00(ref Vp9Common cm, ref MacroBlockDPlane plane, int miRow,
            ref LoopFilterMask lfm)
        {
            ref Buf2D dst = ref plane.Dst;
            ArrayPtr<byte> dst0 = dst.Buf;
            ulong mask16X16 = lfm.LeftY[(int)TxSize.Tx16X16];
            ulong mask8X8 = lfm.LeftY[(int)TxSize.Tx8X8];
            ulong mask4X4 = lfm.LeftY[(int)TxSize.Tx4X4];
            ulong mask4X4Int = lfm.Int4X4Y;

            Debug.Assert(plane.SubsamplingX == 0 && plane.SubsamplingY == 0);

            // Vertical pass: do 2 rows at one time
            for (int r = 0; r < Constants.MiBlockSize && miRow + r < cm.MiRows; r += 2)
            {
                if (cm.UseHighBitDepth)
                {
                    // Disable filtering on the leftmost column.
                    HighbdFilterSelectivelyVertRow2(
                        plane.SubsamplingX,
                        ConvertToUshortPtr(dst.Buf),
                        dst.Stride,
                        (uint)mask16X16,
                        (uint)mask8X8,
                        (uint)mask4X4,
                        (uint)mask4X4Int,
                        cm.LfInfo.Lfthr.AsSpan(),
                        lfm.LflY.AsSpan().Slice(r << 3),
                        (int)cm.BitDepth);
                }
                else
                {
                    // Disable filtering on the leftmost column.
                    FilterSelectivelyVertRow2(
                        plane.SubsamplingX,
                        dst.Buf,
                        dst.Stride,
                        (uint)mask16X16,
                        (uint)mask8X8,
                        (uint)mask4X4,
                        (uint)mask4X4Int,
                        cm.LfInfo.Lfthr.AsSpan(),
                        lfm.LflY.AsSpan().Slice(r << 3));
                }

                dst.Buf = dst.Buf.Slice(16 * dst.Stride);
                mask16X16 >>= 16;
                mask8X8 >>= 16;
                mask4X4 >>= 16;
                mask4X4Int >>= 16;
            }

            // Horizontal pass
            dst.Buf = dst0;
            mask16X16 = lfm.AboveY[(int)TxSize.Tx16X16];
            mask8X8 = lfm.AboveY[(int)TxSize.Tx8X8];
            mask4X4 = lfm.AboveY[(int)TxSize.Tx4X4];
            mask4X4Int = lfm.Int4X4Y;

            for (int r = 0; r < Constants.MiBlockSize && miRow + r < cm.MiRows; r++)
            {
                uint mask16X16R;
                uint mask8X8R;
                uint mask4X4R;

                if (miRow + r == 0)
                {
                    mask16X16R = 0;
                    mask8X8R = 0;
                    mask4X4R = 0;
                }
                else
                {
                    mask16X16R = (uint)mask16X16 & 0xff;
                    mask8X8R = (uint)mask8X8 & 0xff;
                    mask4X4R = (uint)mask4X4 & 0xff;
                }

                if (cm.UseHighBitDepth)
                {
                    HighbdFilterSelectivelyHoriz(
                        ConvertToUshortPtr(dst.Buf),
                        dst.Stride,
                        mask16X16R,
                        mask8X8R,
                        mask4X4R,
                        (uint)mask4X4Int & 0xff,
                        cm.LfInfo.Lfthr.AsSpan(),
                        lfm.LflY.AsSpan().Slice(r << 3),
                        (int)cm.BitDepth);
                }
                else
                {
                    FilterSelectivelyHoriz(
                        dst.Buf,
                        dst.Stride,
                        mask16X16R,
                        mask8X8R,
                        mask4X4R,
                        (uint)mask4X4Int & 0xff,
                        cm.LfInfo.Lfthr.AsSpan(),
                        lfm.LflY.AsSpan().Slice(r << 3));
                }

                dst.Buf = dst.Buf.Slice(8 * dst.Stride);
                mask16X16 >>= 8;
                mask8X8 >>= 8;
                mask4X4 >>= 8;
                mask4X4Int >>= 8;
            }
        }

        private static void FilterBlockPlaneSs11(ref Vp9Common cm, ref MacroBlockDPlane plane, int miRow,
            ref LoopFilterMask lfm)
        {
            Buf2D dst = plane.Dst;
            ArrayPtr<byte> dst0 = dst.Buf;

            Span<byte> lflUv = stackalloc byte[16];

            ushort mask16X16 = lfm.LeftUv[(int)TxSize.Tx16X16];
            ushort mask8X8 = lfm.LeftUv[(int)TxSize.Tx8X8];
            ushort mask4X4 = lfm.LeftUv[(int)TxSize.Tx4X4];
            ushort mask4X4Int = lfm.Int4X4Uv;

            Debug.Assert(plane.SubsamplingX == 1 && plane.SubsamplingY == 1);

            // Vertical pass: do 2 rows at one time
            for (int r = 0; r < Constants.MiBlockSize && miRow + r < cm.MiRows; r += 4)
            {
                for (int c = 0; c < Constants.MiBlockSize >> 1; c++)
                {
                    lflUv[(r << 1) + c] = lfm.LflY[(r << 3) + (c << 1)];
                    lflUv[((r + 2) << 1) + c] = lfm.LflY[((r + 2) << 3) + (c << 1)];
                }

                if (cm.UseHighBitDepth)
                {
                    // Disable filtering on the leftmost column.
                    HighbdFilterSelectivelyVertRow2(
                        plane.SubsamplingX,
                        ConvertToUshortPtr(dst.Buf),
                        dst.Stride,
                        mask16X16,
                        mask8X8,
                        mask4X4,
                        mask4X4Int,
                        cm.LfInfo.Lfthr.AsSpan(),
                        lflUv.Slice(r << 1),
                        (int)cm.BitDepth);
                }
                else
                {
                    // Disable filtering on the leftmost column.
                    FilterSelectivelyVertRow2(
                        plane.SubsamplingX,
                        dst.Buf,
                        dst.Stride,
                        mask16X16,
                        mask8X8,
                        mask4X4,
                        mask4X4Int,
                        cm.LfInfo.Lfthr.AsSpan(),
                        lflUv.Slice(r << 1));
                }

                dst.Buf = dst.Buf.Slice(16 * dst.Stride);
                mask16X16 >>= 8;
                mask8X8 >>= 8;
                mask4X4 >>= 8;
                mask4X4Int >>= 8;
            }

            // Horizontal pass
            dst.Buf = dst0;
            mask16X16 = lfm.AboveUv[(int)TxSize.Tx16X16];
            mask8X8 = lfm.AboveUv[(int)TxSize.Tx8X8];
            mask4X4 = lfm.AboveUv[(int)TxSize.Tx4X4];
            mask4X4Int = lfm.Int4X4Uv;

            for (int r = 0; r < Constants.MiBlockSize && miRow + r < cm.MiRows; r += 2)
            {
                bool skipBorder4X4R = miRow + r == cm.MiRows - 1;
                uint mask4X4IntR = skipBorder4X4R ? 0u : (uint)mask4X4Int & 0xf;
                uint mask16X16R;
                uint mask8X8R;
                uint mask4X4R;

                if (miRow + r == 0)
                {
                    mask16X16R = 0;
                    mask8X8R = 0;
                    mask4X4R = 0;
                }
                else
                {
                    mask16X16R = (uint)mask16X16 & 0xf;
                    mask8X8R = (uint)mask8X8 & 0xf;
                    mask4X4R = (uint)mask4X4 & 0xf;
                }

                if (cm.UseHighBitDepth)
                {
                    HighbdFilterSelectivelyHoriz(
                        ConvertToUshortPtr(dst.Buf),
                        dst.Stride,
                        mask16X16R,
                        mask8X8R,
                        mask4X4R,
                        mask4X4IntR,
                        cm.LfInfo.Lfthr.AsSpan(),
                        lflUv.Slice(r << 1),
                        (int)cm.BitDepth);
                }
                else
                {
                    FilterSelectivelyHoriz(
                        dst.Buf,
                        dst.Stride,
                        mask16X16R,
                        mask8X8R,
                        mask4X4R,
                        mask4X4IntR,
                        cm.LfInfo.Lfthr.AsSpan(),
                        lflUv.Slice(r << 1));
                }

                dst.Buf = dst.Buf.Slice(8 * dst.Stride);
                mask16X16 >>= 4;
                mask8X8 >>= 4;
                mask4X4 >>= 4;
                mask4X4Int >>= 4;
            }
        }

        private enum LfPath
        {
            LfPathSlow,
            LfPath420,
            LfPath444
        }

        private static void LoopFilterRows(
            ref Surface frameBuffer,
            ref Vp9Common cm,
            Array3<MacroBlockDPlane> planes,
            int start,
            int stop,
            int step,
            bool yOnly,
            LfSync lfSync)
        {
            int numPlanes = yOnly ? 1 : Constants.MaxMbPlane;
            int sbCols = TileInfo.MiColsAlignedToSb(cm.MiCols) >> Constants.MiBlockSizeLog2;
            LfPath path;
            int miRow, miCol;

            if (yOnly)
            {
                path = LfPath.LfPath444;
            }
            else if (planes[1].SubsamplingY == 1 && planes[1].SubsamplingX == 1)
            {
                path = LfPath.LfPath420;
            }
            else if (planes[1].SubsamplingY == 0 && planes[1].SubsamplingX == 0)
            {
                path = LfPath.LfPath444;
            }
            else
            {
                path = LfPath.LfPathSlow;
            }

            for (miRow = start; miRow < stop; miRow += step)
            {
                ArrayPtr<Ptr<ModeInfo>> mi = cm.MiGridVisible.Slice(miRow * cm.MiStride);
                Span<LoopFilterMask> lfm = GetLfm(ref cm.Lf, miRow, 0);

                for (miCol = 0; miCol < cm.MiCols; miCol += Constants.MiBlockSize, lfm = lfm.Slice(1))
                {
                    int r = miRow >> Constants.MiBlockSizeLog2;
                    int c = miCol >> Constants.MiBlockSizeLog2;
                    int plane;

                    lfSync.SyncRead(r, c);

                    ReconInter.SetupDstPlanes(ref planes, ref frameBuffer, miRow, miCol);

                    AdjustMask(ref cm, miRow, miCol, ref lfm[0]);

                    FilterBlockPlaneSs00(ref cm, ref planes[0], miRow, ref lfm[0]);
                    for (plane = 1; plane < numPlanes; ++plane)
                    {
                        switch (path)
                        {
                            case LfPath.LfPath420:
                                FilterBlockPlaneSs11(ref cm, ref planes[plane], miRow, ref lfm[0]);
                                break;
                            case LfPath.LfPath444:
                                FilterBlockPlaneSs00(ref cm, ref planes[plane], miRow, ref lfm[0]);
                                break;
                            case LfPath.LfPathSlow:
                                FilterBlockPlaneNon420(ref cm, ref planes[plane], mi.Slice(miCol), miRow,
                                    miCol);
                                break;
                        }
                    }

                    lfSync.SyncWrite(r, c, sbCols);
                }
            }
        }

        public static void LoopFilterFrame(
            ref Surface frame,
            ref Vp9Common cm,
            ref MacroBlockD xd,
            int frameFilterLevel,
            bool yOnly,
            bool partialFrame)
        {
            if (frameFilterLevel == 0)
            {
                return;
            }

            int startMiRow = 0;
            int miRowsToFilter = cm.MiRows;

            if (partialFrame && cm.MiRows > 8)
            {
                startMiRow = cm.MiRows >> 1;
                startMiRow &= ~7;
                miRowsToFilter = Math.Max(cm.MiRows / 8, 8);
            }

            int endMiRow = startMiRow + miRowsToFilter;

            LoopFilterRows(ref frame, ref cm, xd.Plane, startMiRow, endMiRow, Constants.MiBlockSize, yOnly,
                default);
        }

        private static void LoopFilterRowsMt(
            ref Surface frameBuffer,
            ref Vp9Common cm,
            Array3<MacroBlockDPlane> planes,
            int start,
            int stop,
            bool yOnly,
            int threadCount)
        {
            int sbRows = TileInfo.MiColsAlignedToSb(cm.MiRows) >> Constants.MiBlockSizeLog2;
            int numTileCols = 1 << cm.Log2TileCols;
            int numWorkers = Math.Min(threadCount, Math.Min(numTileCols, sbRows));

            LfSync lfSync = new();
            lfSync.Initialize(cm.Width, sbRows);

            Ptr<Surface> frameBufferPtr = new(ref frameBuffer);
            Ptr<Vp9Common> cmPtr = new(ref cm);

            Parallel.For(0, numWorkers, n =>
            {
                LoopFilterRows(
                    ref frameBufferPtr.Value,
                    ref cmPtr.Value,
                    planes,
                    start + (n * Constants.MiBlockSize),
                    stop,
                    numWorkers * Constants.MiBlockSize,
                    yOnly,
                    lfSync);
            });
        }

        public static void LoopFilterFrameMt(
            ref Surface frame,
            ref Vp9Common cm,
            ref MacroBlockD xd,
            int frameFilterLevel,
            bool yOnly,
            bool partialFrame,
            int threadCount)
        {
            if (frameFilterLevel == 0)
            {
                return;
            }

            int startMiRow = 0;
            int miRowsToFilter = cm.MiRows;

            if (partialFrame && cm.MiRows > 8)
            {
                startMiRow = cm.MiRows >> 1;
                startMiRow &= ~7;
                miRowsToFilter = Math.Max(cm.MiRows / 8, 8);
            }

            int endMiRow = startMiRow + miRowsToFilter;

            LoopFilterFrameInit(ref cm, frameFilterLevel);
            LoopFilterRowsMt(ref frame, ref cm, xd.Plane, startMiRow, endMiRow, yOnly, threadCount);
        }

        private static unsafe ArrayPtr<ushort> ConvertToUshortPtr(ArrayPtr<byte> s)
        {
            return new ArrayPtr<ushort>((ushort*)s.ToPointer(), s.Length / 2);
        }
    }
}