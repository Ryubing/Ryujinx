using Ryujinx.Common.Memory;

namespace Ryujinx.HLE.HOS.Services.Vi.Types
{
    struct SharedBufferMap
    {
        public struct Entry
        {
            public ulong Offset;
            public ulong Size;
            public uint Width;
            public uint Height;
        }

        public int Count;
        public int Padding;
        public Array16<Entry> SharedBuffers;
    }
}
