namespace Ryujinx.Graphics.Nvdec.Vp9.Types
{
    internal enum BlockSize
    {
        Block4X4,
        Block4X8,
        Block8X4,
        Block8X8,
        Block8X16,
        Block16X8,
        Block16X16,
        Block16X32,
        Block32X16,
        Block32X32,
        Block32X64,
        Block64X32,
        Block64X64,
        BlockSizes,
        BlockInvalid = BlockSizes
    }
}