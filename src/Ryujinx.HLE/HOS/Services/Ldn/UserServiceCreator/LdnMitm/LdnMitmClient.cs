using Ryujinx.Common.Logging;
using Ryujinx.Common.Utilities;
using Ryujinx.HLE.HOS.Services.Ldn.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Types;
using System;
using System.Net.NetworkInformation;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnMitm
{
    /// <summary>
    /// Client implementation for <a href="https://github.com/spacemeowx2/ldn_mitm">ldn_mitm</a>
    /// </summary>
    internal class LdnMitmClient : INetworkClient
    {
        public ProxyConfig Config { get; }
        public bool NeedsRealId => false;

        public event EventHandler<NetworkChangeEventArgs> NetworkChange;

        private readonly LanDiscovery _lanDiscovery;

        public LdnMitmClient(HleConfiguration config)
        {
            UnicastIPAddressInformation localIpInterface = NetworkHelpers.GetLocalInterface(config.MultiplayerLanInterfaceId).Item2;

            _lanDiscovery = new LanDiscovery(this, localIpInterface.Address, localIpInterface.IPv4Mask);
        }

        internal void InvokeNetworkChange(NetworkInfo info, bool connected, DisconnectReason reason = DisconnectReason.None)
        {
            NetworkChange?.Invoke(this, new NetworkChangeEventArgs(info, connected: connected, disconnectReason: reason));
        }

        public NetworkError Connect(ConnectRequest request)
        {
            return _lanDiscovery.Connect(request.NetworkInfo, request.UserConfig, request.LocalCommunicationVersion);
        }

        public NetworkError ConnectPrivate(ConnectPrivateRequest request)
        {
            // NOTE: This method is not implemented in ldn_mitm
            Logger.Stub?.PrintMsg(LogClass.ServiceLdn, "LdnMitmClient ConnectPrivate");

            return NetworkError.None;
        }

        public bool CreateNetwork(CreateAccessPointRequest request, byte[] advertiseData)
        {
            return _lanDiscovery.CreateNetwork(request.SecurityConfig, request.UserConfig, request.NetworkConfig);
        }

        public bool CreateNetworkPrivate(CreateAccessPointPrivateRequest request, byte[] advertiseData)
        {
            // NOTE: This method is not implemented in ldn_mitm
            Logger.Stub?.PrintMsg(LogClass.ServiceLdn, "LdnMitmClient CreateNetworkPrivate");

            return true;
        }

        public void DisconnectAndStop()
        {
            _lanDiscovery.DisconnectAndStop();
        }

        public void DisconnectNetwork()
        {
            _lanDiscovery.DestroyNetwork();
        }

        public ResultCode Reject(DisconnectReason disconnectReason, uint nodeId)
        {
            // NOTE: This method is not implemented in ldn_mitm
            Logger.Stub?.PrintMsg(LogClass.ServiceLdn, "LdnMitmClient Reject");

            return ResultCode.Success;
        }

        public NetworkInfo[] Scan(ushort channel, ScanFilter scanFilter)
        {
            return _lanDiscovery.Scan(channel, scanFilter);
        }

        public void SetAdvertiseData(byte[] data)
        {
            _lanDiscovery.SetAdvertiseData(data);
        }

        public void SetGameVersion(byte[] versionString)
        {
            // NOTE: This method is not implemented in ldn_mitm
            Logger.Stub?.PrintMsg(LogClass.ServiceLdn, "LdnMitmClient SetGameVersion");
        }

        public void SetStationAcceptPolicy(AcceptPolicy acceptPolicy)
        {
            // NOTE: This method is not implemented in ldn_mitm
            Logger.Stub?.PrintMsg(LogClass.ServiceLdn, "LdnMitmClient SetStationAcceptPolicy");
        }

        public void Dispose()
        {
            _lanDiscovery.Dispose();
        }
    }
}
