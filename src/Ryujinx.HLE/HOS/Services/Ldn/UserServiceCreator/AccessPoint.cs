using Ryujinx.Common.Memory;
using Ryujinx.HLE.HOS.Services.Ldn.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Types;
using System;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator
{
    class AccessPoint : IDisposable
    {
        private byte[] _advertiseData;

        private readonly IUserLocalCommunicationService _parent;

        public NetworkInfo NetworkInfo;
        public Array8<NodeLatestUpdate> LatestUpdates = new();
        public bool Connected { get; private set; }

        public ProxyConfig Config => _parent.NetworkClient.Config;

        public AccessPoint(IUserLocalCommunicationService parent)
        {
            _parent = parent;

            _parent.NetworkClient.NetworkChange += NetworkChanged;
        }

        public void Dispose()
        {
            if (_parent?.NetworkClient != null)
            {
                _parent.NetworkClient.DisconnectNetwork();

                _parent.NetworkClient.NetworkChange -= NetworkChanged;
            }
        }

        private void NetworkChanged(object sender, NetworkChangeEventArgs e)
        {
            LatestUpdates.CalculateLatestUpdate(NetworkInfo.Ldn.Nodes, e.Info.Ldn.Nodes);

            NetworkInfo = e.Info;

            if (Connected != e.Connected)
            {
                Connected = e.Connected;

                if (Connected)
                {
                    _parent.SetState(NetworkState.AccessPointCreated);
                }
                else
                {
                    _parent.SetDisconnectReason(e.DisconnectReasonOrDefault(DisconnectReason.DestroyedBySystem));
                }
            }
            else
            {
                _parent.SetState();
            }
        }

        public ResultCode SetAdvertiseData(byte[] advertiseData)
        {
            _advertiseData = advertiseData;

            _parent.NetworkClient.SetAdvertiseData(_advertiseData);

            return ResultCode.Success;
        }

        public ResultCode SetStationAcceptPolicy(AcceptPolicy acceptPolicy)
        {
            _parent.NetworkClient.SetStationAcceptPolicy(acceptPolicy);

            return ResultCode.Success;
        }

        public ResultCode CreateNetwork(SecurityConfig securityConfig, UserConfig userConfig, NetworkConfig networkConfig)
        {
            CreateAccessPointRequest request = new()
            {
                SecurityConfig = securityConfig,
                UserConfig = userConfig,
                NetworkConfig = networkConfig,
            };

            bool success = _parent.NetworkClient.CreateNetwork(request, _advertiseData ?? []);

            return success ? ResultCode.Success : ResultCode.InvalidState;
        }

        public ResultCode CreateNetworkPrivate(SecurityConfig securityConfig, SecurityParameter securityParameter, UserConfig userConfig, NetworkConfig networkConfig, AddressList addressList)
        {
            CreateAccessPointPrivateRequest request = new()
            {
                SecurityConfig = securityConfig,
                SecurityParameter = securityParameter,
                UserConfig = userConfig,
                NetworkConfig = networkConfig,
                AddressList = addressList,
            };

            bool success = _parent.NetworkClient.CreateNetworkPrivate(request, _advertiseData);

            return success ? ResultCode.Success : ResultCode.InvalidState;
        }
    }
}
