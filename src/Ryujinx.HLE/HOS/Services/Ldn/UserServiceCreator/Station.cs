using Ryujinx.Common.Memory;
using Ryujinx.HLE.HOS.Services.Ldn.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Types;
using System;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator
{
    class Station : IDisposable
    {
        public NetworkInfo NetworkInfo;
        public Array8<NodeLatestUpdate> LatestUpdates = new();

        private readonly IUserLocalCommunicationService _parent;

        public bool Connected { get; private set; }

        public ProxyConfig Config => _parent.NetworkClient.Config;

        public Station(IUserLocalCommunicationService parent)
        {
            _parent = parent;

            _parent.NetworkClient.NetworkChange += NetworkChanged;
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
                    _parent.SetState(NetworkState.StationConnected);
                }
                else
                {
                    _parent.SetDisconnectReason(e.DisconnectReasonOrDefault(DisconnectReason.DestroyedByUser));
                }
            }
            else
            {
                _parent.SetState();
            }
        }

        public void Dispose()
        {
            if (_parent.NetworkClient != null)
            {
                _parent.NetworkClient.DisconnectNetwork();

                _parent.NetworkClient.NetworkChange -= NetworkChanged;
            }
        }

        private ResultCode NetworkErrorToResult(NetworkError error)
        {
            return error switch
            {
                NetworkError.None => ResultCode.Success,
                NetworkError.VersionTooLow => ResultCode.VersionTooLow,
                NetworkError.VersionTooHigh => ResultCode.VersionTooHigh,
                NetworkError.TooManyPlayers => ResultCode.TooManyPlayers,

                NetworkError.ConnectFailure => ResultCode.ConnectFailure,
                NetworkError.ConnectNotFound => ResultCode.ConnectNotFound,
                NetworkError.ConnectTimeout => ResultCode.ConnectTimeout,
                NetworkError.ConnectRejected => ResultCode.ConnectRejected,

                _ => ResultCode.DeviceNotAvailable,
            };
        }

        public ResultCode Connect(
            SecurityConfig securityConfig,
            UserConfig userConfig,
            uint localCommunicationVersion,
            uint optionUnknown,
            NetworkInfo networkInfo)
        {
            ConnectRequest request = new()
            {
                SecurityConfig = securityConfig,
                UserConfig = userConfig,
                LocalCommunicationVersion = localCommunicationVersion,
                OptionUnknown = optionUnknown,
                NetworkInfo = networkInfo,
            };

            return NetworkErrorToResult(_parent.NetworkClient.Connect(request));
        }

        public ResultCode ConnectPrivate(
            SecurityConfig securityConfig,
            SecurityParameter securityParameter,
            UserConfig userConfig,
            uint localCommunicationVersion,
            uint optionUnknown,
            NetworkConfig networkConfig)
        {
            ConnectPrivateRequest request = new()
            {
                SecurityConfig = securityConfig,
                SecurityParameter = securityParameter,
                UserConfig = userConfig,
                LocalCommunicationVersion = localCommunicationVersion,
                OptionUnknown = optionUnknown,
                NetworkConfig = networkConfig,
            };

            return NetworkErrorToResult(_parent.NetworkClient.ConnectPrivate(request));
        }
    }
}
