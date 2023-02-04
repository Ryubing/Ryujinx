using Ryujinx.Graphics.Nvdec.Vp9.Common;
using System;
using System.Diagnostics;

namespace Ryujinx.Graphics.Nvdec.Vp9.Types
{
    internal struct TileInfo
    {
        private const int MinTileWidthB64 = 4;
        private const int MaxTileWidthB64 = 64;

        public int MiRowStart, MiRowEnd;
        public int MiColStart, MiColEnd;

        public static int MiColsAlignedToSb(int nMis)
        {
            return BitUtils.AlignPowerOfTwo(nMis, Constants.MiBlockSizeLog2);
        }

        private static int GetTileOffset(int idx, int mis, int log2)
        {
            int sbCols = MiColsAlignedToSb(mis) >> Constants.MiBlockSizeLog2;
            int offset = ((idx * sbCols) >> log2) << Constants.MiBlockSizeLog2;

            return Math.Min(offset, mis);
        }

        public void SetRow(ref Vp9Common cm, int row)
        {
            MiRowStart = GetTileOffset(row, cm.MiRows, cm.Log2TileRows);
            MiRowEnd = GetTileOffset(row + 1, cm.MiRows, cm.Log2TileRows);
        }

        public void SetCol(ref Vp9Common cm, int col)
        {
            MiColStart = GetTileOffset(col, cm.MiCols, cm.Log2TileCols);
            MiColEnd = GetTileOffset(col + 1, cm.MiCols, cm.Log2TileCols);
        }

        public void Init(ref Vp9Common cm, int row, int col)
        {
            SetRow(ref cm, row);
            SetCol(ref cm, col);
        }

        // Checks that the given miRow, miCol and search point
        // are inside the borders of the tile.
        public readonly bool IsInside(int miCol, int miRow, int miRows, ref Position miPos)
        {
            return !(miRow + miPos.Row < 0 ||
                     miCol + miPos.Col < MiColStart ||
                     miRow + miPos.Row >= miRows ||
                     miCol + miPos.Col >= MiColEnd);
        }

        private static int GetMinLog2TileCols(int sb64Cols)
        {
            int minLog2 = 0;
            while (MaxTileWidthB64 << minLog2 < sb64Cols)
            {
                ++minLog2;
            }

            return minLog2;
        }

        private static int GetMaxLog2TileCols(int sb64Cols)
        {
            int maxLog2 = 1;
            while (sb64Cols >> maxLog2 >= MinTileWidthB64)
            {
                ++maxLog2;
            }

            return maxLog2 - 1;
        }

        public static void GetTileNBits(int miCols, out int minLog2TileCols, out int maxLog2TileCols)
        {
            int sb64Cols = MiColsAlignedToSb(miCols) >> Constants.MiBlockSizeLog2;
            minLog2TileCols = GetMinLog2TileCols(sb64Cols);
            maxLog2TileCols = GetMaxLog2TileCols(sb64Cols);
            Debug.Assert(minLog2TileCols <= maxLog2TileCols);
        }
    }
}