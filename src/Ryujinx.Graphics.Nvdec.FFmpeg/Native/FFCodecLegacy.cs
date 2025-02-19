namespace Ryujinx.Graphics.Nvdec.FFmpeg.Native
{
    struct FFCodecLegacy<T> where T : struct
    {
#pragma warning disable CS0649 // Field is never assigned to
        public T Base;
        public uint CapsInternalOrCbType;
        public int PrivDataSize;
        public nint UpdateThreadContext;
        public nint UpdateThreadContextForUser;
        public nint Defaults;
        public nint InitStaticData;
        public nint Init;
        public nint EncodeSub;
        public nint Encode2;
        public nint Decode;
#pragma warning restore CS0649

        // NOTE: There is more after, but the layout kind of changed a bit and we don't need more than this. This is safe as we only manipulate this behind a reference.
    }
}
