using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnRyu.Types
{
    /// <summary>
    /// Represents data sent over a transport layer.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 0x14)]
    struct ProxyDataHeader
    {
        public ProxyInfo Info;
        public uint DataLength; // Followed by the data with the specified byte length.
    }
}
