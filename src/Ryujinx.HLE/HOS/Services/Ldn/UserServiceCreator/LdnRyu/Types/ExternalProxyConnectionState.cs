using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnRyu.Types
{
    /// <summary>
    /// Indicates a change in connection state for the given client.
    /// Is sent to notify the master server when connection is first established.
    /// Can be sent by the external proxy to the master server to notify it of a proxy disconnect.
    /// Can be sent by the master server to notify the external proxy of a user leaving a room.
    /// Both will result in a force kick.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 0x8, Pack = 4)]
    struct ExternalProxyConnectionState
    {
        public uint IpAddress;
        public bool Connected;
    }
}
