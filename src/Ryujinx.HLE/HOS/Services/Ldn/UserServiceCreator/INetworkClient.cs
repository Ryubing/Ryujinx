using Ryujinx.HLE.HOS.Services.Ldn.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Types;
using System;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator
{
    interface INetworkClient : IDisposable
    {
        ProxyConfig Config { get; }
        bool NeedsRealId { get; }

        event EventHandler<NetworkChangeEventArgs> NetworkChange;

        void DisconnectNetwork();
        void DisconnectAndStop();
        NetworkError Connect(ConnectRequest request);
        NetworkError ConnectPrivate(ConnectPrivateRequest request);
        ResultCode Reject(DisconnectReason disconnectReason, uint nodeId);
        NetworkInfo[] Scan(ushort channel, ScanFilter scanFilter);
        void SetGameVersion(byte[] versionString);
        void SetStationAcceptPolicy(AcceptPolicy acceptPolicy);
        void SetAdvertiseData(byte[] data);
        bool CreateNetwork(CreateAccessPointRequest request, byte[] advertiseData);
        bool CreateNetworkPrivate(CreateAccessPointPrivateRequest request, byte[] advertiseData);
    }
}
