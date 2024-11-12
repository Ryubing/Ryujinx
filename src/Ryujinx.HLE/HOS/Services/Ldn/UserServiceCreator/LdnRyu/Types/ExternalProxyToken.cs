using Ryujinx.Common.Memory;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnRyu.Types
{
    /// <summary>
    /// Sent by the master server to an external proxy to tell them someone is going to connect.
    /// This drives authentication, and lets the proxy know what virtual IP to give to each joiner,
    /// as these are managed by the master server.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 0x28)]
    struct ExternalProxyToken
    {
        public uint VirtualIp;
        public Array16<byte> Token;
        public Array16<byte> PhysicalIp;
        public AddressFamily AddressFamily;
    }
}
