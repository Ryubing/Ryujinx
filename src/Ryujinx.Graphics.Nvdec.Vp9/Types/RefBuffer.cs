namespace Ryujinx.Graphics.Nvdec.Vp9.Types
{
    internal struct RefBuffer
    {
        public const int InvalidIdx = -1; // Invalid buffer index.

        public int Idx;
        public Surface Buf;
        public ScaleFactors Sf;
    }
}