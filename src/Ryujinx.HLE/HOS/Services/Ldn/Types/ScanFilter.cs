using Ryujinx.Common.Memory;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.Types
{
    [StructLayout(LayoutKind.Sequential, Size = 0x60, Pack = 8)]
    struct ScanFilter
    {
        public NetworkId NetworkId;
        public NetworkType NetworkType;
        public Array6<byte> MacAddress;
        public Ssid Ssid;
        public Array16<byte> Reserved;
        public ScanFilterFlag Flag;
    }
}
