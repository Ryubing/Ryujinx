namespace Ryujinx.Graphics.Nvdec.Vp9.Types
{
    internal enum SegLvlFeatures
    {
        AltQ, // Use alternate Quantizer ....
        AltLf, // Use alternate loop filter value...
        RefFrame, // Optional Segment reference frame
        Skip, // Optional Segment (0,0) + skip mode
        Max // Number of features supported
    }
}