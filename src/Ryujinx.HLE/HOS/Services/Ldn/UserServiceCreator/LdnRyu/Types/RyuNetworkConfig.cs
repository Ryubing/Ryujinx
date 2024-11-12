using Ryujinx.Common.Memory;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnRyu.Types
{
    [StructLayout(LayoutKind.Sequential, Size = 0x28, Pack = 1)]
    struct RyuNetworkConfig
    {
        public Array16<byte> GameVersion;

        // PrivateIp is included for external proxies for the case where a client attempts to join from
        // their own LAN. UPnP forwarding can fail when connecting devices on the same network over the public IP,
        // so if their public IP is identical, the internal address should be sent instead.

        // The fields below are 0 if not hosting a p2p proxy.

        public Array16<byte> PrivateIp;
        public AddressFamily AddressFamily;
        public ushort ExternalProxyPort;
        public ushort InternalProxyPort;
    }
}
