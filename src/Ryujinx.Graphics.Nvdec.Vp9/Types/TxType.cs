namespace Ryujinx.Graphics.Nvdec.Vp9.Types
{
    internal enum TxType
    {
        DctDct, // DCT  in both horizontal and vertical
        AdstDct, // ADST in vertical, DCT in horizontal
        DctAdst, // DCT  in vertical, ADST in horizontal
        AdstAdst, // ADST in both directions
        TxTypes
    }
}