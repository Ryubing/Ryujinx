namespace Ryujinx.Graphics.Nvdec.Vp9.Types
{
    internal enum VpxColorRange
    {
        // Y [16..235], UV [16..240]
        Studio,

        // YUV/RGB [0..255]
        Full
    }
}