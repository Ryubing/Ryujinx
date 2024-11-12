namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnRyu.Types
{
    enum PacketId
    {
        Initialize,
        Passphrase,

        CreateAccessPoint,
        CreateAccessPointPrivate,
        ExternalProxy,
        ExternalProxyToken,
        ExternalProxyState,
        SyncNetwork,
        Reject,
        RejectReply,
        Scan,
        ScanReply,
        ScanReplyEnd,
        Connect,
        ConnectPrivate,
        Connected,
        Disconnect,

        ProxyConfig,
        ProxyConnect,
        ProxyConnectReply,
        ProxyData,
        ProxyDisconnect,

        SetAcceptPolicy,
        SetAdvertiseData,

        Ping = 254,
        NetworkError = 255
    }
}
