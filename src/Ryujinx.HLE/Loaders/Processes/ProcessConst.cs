namespace Ryujinx.HLE.Loaders.Processes
{
    static class ProcessConst
    {
        // Binaries from exefs are loaded into mem in this order. Do not change.
        public static readonly string[] ExeFsPrefixes =
        [
            "rtld",
            "main",
            "subsdk0",
            "subsdk1",
            "subsdk2",
            "subsdk3",
            "subsdk4",
            "subsdk5",
            "subsdk6",
            "subsdk7",
            "subsdk8",
            "subsdk9",
            "sdk"
        ];

        public const string MainNpdmPath = "/main.npdm";

        public const int NroAsetMagic = ('A' << 0) | ('S' << 8) | ('E' << 16) | ('T' << 24);

        public const bool AslrEnabled = true;

        public const int NsoArgsHeaderSize = 8;
        public const int NsoArgsDataSize = 0x9000;
        public const int NsoArgsTotalSize = NsoArgsHeaderSize + NsoArgsDataSize;
    }
}
