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
            if (cbPriv.IsNull)
            {
                return -1;
            }

            // Find a free frame buffer.
            for (i = 0; i < cbPriv.Value.IntFb.Length; ++i)
            {
                if (!cbPriv.Value.IntFb[i].InUse)
                {
                    break;
                }
            }

            if (i == cbPriv.Value.IntFb.Length)
            {
                return -1;
            }

            if ((ulong)cbPriv.Value.IntFb[i].Data.Length < minSize)
            {
                if (!cbPriv.Value.IntFb[i].Data.IsNull)
                {
                    allocator.Free(cbPriv.Value.IntFb[i].Data);
                }

                // The data must be zeroed to fix a valgrind error from the C loop filter
                // due to access uninitialized memory in frame border. It could be
                // skipped if border were totally removed.
                cbPriv.Value.IntFb[i].Data = allocator.Allocate<byte>((int)minSize);
                if (cbPriv.Value.IntFb[i].Data.IsNull)
                {
                    return -1;
                }
            }

            fb.Data = cbPriv.Value.IntFb[i].Data;
            cbPriv.Value.IntFb[i].InUse = true;

            // Set the frame buffer's private data to point at the internal frame buffer.
            fb.Priv = new Ptr<InternalFrameBuffer>(ref cbPriv.Value.IntFb[i]);
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
