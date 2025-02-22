namespace Ryujinx.Graphics.Nvdec.Vp9.Types
{
    internal struct RefCntBuffer
    {
        public int RefCount;
        public int MiRows;
        public int MiCols;
        public byte Released;
        public VpxCodecFrameBuffer RawFrameBuffer;
        public Surface Buf;
    }
}