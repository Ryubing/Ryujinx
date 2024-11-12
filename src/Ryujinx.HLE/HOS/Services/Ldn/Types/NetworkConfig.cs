using Ryujinx.Common.Memory;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.Types
{
    [StructLayout(LayoutKind.Sequential, Size = 0x20, Pack = 8)]
    struct NetworkConfig
    {
        public IntentId IntentId;
        public ushort Channel;
        public byte NodeCountMax;
        public byte Reserved1;
        public ushort LocalCommunicationVersion;
        public Array10<byte> Reserved2;
    }
}
