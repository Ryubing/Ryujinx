namespace Ryujinx.Graphics.Nvdec.FFmpeg.Native
{
    struct AVCodec
    {
#pragma warning disable CS0649 // Field is never assigned to
        public unsafe byte* Name;
        public unsafe byte* LongName;
        public int Type;
        public AVCodecID Id;
        public int Capabilities;
        public byte MaxLowRes;
        public unsafe AVRational* SupportedFramerates;
        public nint PixFmts;
        public nint SupportedSamplerates;
        public nint SampleFmts;
        // Deprecated
        public unsafe ulong* ChannelLayouts;
        public unsafe nint PrivClass;
        public nint Profiles;
        public unsafe byte* WrapperName;
        public nint ChLayouts;
#pragma warning restore CS0649
    }
}
