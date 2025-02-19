using Ryujinx.Common.Memory;

namespace Ryujinx.Graphics.Nvdec.Vp9.Types
{
    // This structure holds bit masks for all 8x8 blocks in a 64x64 region.
    // Each 1 bit represents a position in which we want to apply the loop filter.
    // Left_ entries refer to whether we apply a filter on the border to the
    // left of the block.   Above_ entries refer to whether or not to apply a
    // filter on the above border.   Int_ entries refer to whether or not to
    // apply borders on the 4x4 edges within the 8x8 block that each bit
    // represents.
    // Since each transform is accompanied by a potentially different type of
    // loop filter there is a different entry in the array for each transform size.
    internal struct LoopFilterMask
    {
        public Array4<ulong> LeftY;
        public Array4<ulong> AboveY;
        public ulong Int4X4Y;
        public Array4<ushort> LeftUv;
        public Array4<ushort> AboveUv;
        public ushort Int4X4Uv;
        public Array64<byte> LflY;
    }
}