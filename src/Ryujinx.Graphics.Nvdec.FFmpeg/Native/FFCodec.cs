namespace Ryujinx.Graphics.Nvdec.FFmpeg.Native
{
    struct FFCodec<T> where T : struct
    {
#pragma warning disable CS0649 // Field is never assigned to
        public T Base;
        public int CapsInternalOrCbType;
        public int PrivDataSize;
        public nint UpdateThreadContext;
        public nint UpdateThreadContextForUser;
        public nint Defaults;
        public nint InitStaticData;
        public nint Init;
        public nint CodecCallback;
#pragma warning restore CS0649

        // NOTE: There is more after, but the layout kind of changed a bit and we don't need more than this. This is safe as we only manipulate this behind a reference.
    }
}
