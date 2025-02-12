using Ryujinx.Common.Memory;

namespace Ryujinx.Graphics.Nvdec.Vp9.Types
{
    internal struct VpxCodecFrameBuffer
    {
        public ArrayPtr<byte> Data;
        public Ptr<InternalFrameBuffer> Priv;
    }
}