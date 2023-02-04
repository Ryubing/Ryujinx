namespace Ryujinx.Graphics.Nvdec.Vp9.Types
{
    public enum TxMode
    {
        Only4x4, // Only 4x4 transform used
        Allow8x8, // Allow block transform size up to 8x8
        Allow16x16, // Allow block transform size up to 16x16
        Allow32x32, // Allow block transform size up to 32x32
        TxModeSelect, // Transform specified for each block
        TxModes
    }
}