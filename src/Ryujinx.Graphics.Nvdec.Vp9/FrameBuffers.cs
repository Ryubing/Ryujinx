using Ryujinx.Common.Memory;
using Ryujinx.Graphics.Nvdec.Vp9.Common;
using Ryujinx.Graphics.Nvdec.Vp9.Types;

namespace Ryujinx.Graphics.Nvdec.Vp9
{
    internal struct InternalFrameBuffer
    {
        public ArrayPtr<byte> Data;
        public bool InUse;
    }

    internal struct InternalFrameBufferList
    {
        public ArrayPtr<InternalFrameBuffer> IntFb;
    }

    internal static class FrameBuffers
    {
        public static int GetFrameBuffer(MemoryAllocator allocator, Ptr<InternalFrameBufferList> cbPriv, ulong minSize,
            ref VpxCodecFrameBuffer fb)
        {
            int i;
            Ptr<InternalFrameBufferList> intFbList = cbPriv;
            if (intFbList.IsNull)
            {
                return -1;
            }

            // Find a free frame buffer.
            for (i = 0; i < intFbList.Value.IntFb.Length; ++i)
            {
                if (!intFbList.Value.IntFb[i].InUse)
                {
                    break;
                }
            }

            if (i == intFbList.Value.IntFb.Length)
            {
                return -1;
            }

            if ((ulong)intFbList.Value.IntFb[i].Data.Length < minSize)
            {
                if (!intFbList.Value.IntFb[i].Data.IsNull)
                {
                    allocator.Free(intFbList.Value.IntFb[i].Data);
                }

                // The data must be zeroed to fix a valgrind error from the C loop filter
                // due to access uninitialized memory in frame border. It could be
                // skipped if border were totally removed.
                intFbList.Value.IntFb[i].Data = allocator.Allocate<byte>((int)minSize);
                if (intFbList.Value.IntFb[i].Data.IsNull)
                {
                    return -1;
                }
            }

            fb.Data = intFbList.Value.IntFb[i].Data;
            intFbList.Value.IntFb[i].InUse = true;

            // Set the frame buffer's private data to point at the internal frame buffer.
            fb.Priv = new Ptr<InternalFrameBuffer>(ref intFbList.Value.IntFb[i]);
            return 0;
        }

        public static int ReleaseFrameBuffer(Ptr<InternalFrameBufferList> cbPriv, ref VpxCodecFrameBuffer fb)
        {
            if (!fb.Priv.IsNull)
            {
                fb.Priv.Value.InUse = false;
            }

            return 0;
        }
    }
}