using Ryujinx.Common.Memory;

namespace Ryujinx.Graphics.Nvdec.Vp9.Types
{
    internal struct BufferPool
    {
        // Private data associated with the frame buffer callbacks.
        public Ptr<InternalFrameBufferList> CbPriv;

        // vpx_get_frame_buffer_cb_fn_t get_fb_cb;
        // vpx_release_frame_buffer_cb_fn_t release_fb_cb;

        public Array12<RefCntBuffer> FrameBufs;

        // Frame buffers allocated internally by the codec.
        public InternalFrameBufferList IntFrameBuffers;
    }
}