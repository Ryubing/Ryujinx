using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnRyu.Types
{
    [StructLayout(LayoutKind.Sequential, Size = 0xA)]
    struct LdnHeader
    {
        public uint Magic;
        public byte Type;
        public byte Version;
        public int DataSize;
    }
}
