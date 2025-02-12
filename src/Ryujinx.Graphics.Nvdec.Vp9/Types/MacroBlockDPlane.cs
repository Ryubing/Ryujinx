using Ryujinx.Common.Memory;

namespace Ryujinx.Graphics.Nvdec.Vp9.Types
{
    internal struct MacroBlockDPlane
    {
        public ArrayPtr<int> DqCoeff;
        public int SubsamplingX;
        public int SubsamplingY;
        public Buf2D Dst;
        public Array2<Buf2D> Pre;
        public ArrayPtr<sbyte> AboveContext;
        public ArrayPtr<sbyte> LeftContext;
        public Array8<Array2<short>> SegDequant;

        // Number of 4x4s in current block
        public ushort N4W, N4H;

        // Log2 of N4W, N4H
        public byte N4Wl, N4Hl;
    }
}