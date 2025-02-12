namespace Ryujinx.Graphics.Nvdec.Vp9.Types
{
    internal enum BlockSize
    {
        Block4x4,
        Block4x8,
        Block8x4,
        Block8x8,
        Block8x16,
        Block16x8,
        Block16x16,
        Block16x32,
        Block32x16,
        Block32x32,
        Block32x64,
        Block64x32,
        Block64x64,
        BlockSizes,
        BlockInvalid = BlockSizes
    }
}