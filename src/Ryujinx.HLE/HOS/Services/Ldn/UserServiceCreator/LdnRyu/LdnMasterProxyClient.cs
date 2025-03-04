using Ryujinx.Common.Logging;
using Ryujinx.Common.Utilities;
using Ryujinx.HLE.HOS.Services.Ldn.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnRyu.Proxy;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnRyu.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Types;
using Ryujinx.HLE.HOS.Services.Sockets.Bsd.Proxy;
using Ryujinx.HLE.Utilities;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TcpClient = NetCoreServer.TcpClient;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnRyu
{
    class LdnMasterProxyClient : TcpClient, INetworkClient, IProxyClient
    {
        public bool NeedsRealId => true;

        private static InitializeMessage InitializeMemory = new();

        private const int InactiveTimeout = 6000;
        private const int FailureTimeout = 4000;
        private const int ScanTimeout = 1000;

        private bool _useP2pProxy;
        private NetworkError _lastError;

        private readonly ManualResetEvent _connected = new(false);
        private readonly ManualResetEvent _error = new(false);
        private readonly ManualResetEvent _scan = new(false);
        private readonly ManualResetEvent _reject = new(false);
        private readonly AutoResetEvent _apConnected = new(false);

        private readonly RyuLdnProtocol _protocol;
        private readonly NetworkTimeout _timeout;

        private readonly List<NetworkInfo> _availableGames = [];
        private DisconnectReason _disconnectReason;

        private P2pProxyServer _hostedProxy;
        private P2pProxyClient _connectedProxy;

        private bool _networkConnected;

        private string _passphrase;
        private byte[] _gameVersion = new byte[0x10];

        private readonly HleConfiguration _config;

        public event EventHandler<NetworkChangeEventArgs> NetworkChange;

        public ProxyConfig Config { get; private set; }

        public LdnMasterProxyClient(string address, int port, HleConfiguration config) : base(address, port)
        {
            if (ProxyHelpers.SupportsNoDelay())
            {
                OptionNoDelay = true;
            }

            _protocol = new RyuLdnProtocol();
            _timeout = new NetworkTimeout(InactiveTimeout, TimeoutConnection);

            _protocol.Initialize += HandleInitialize;
            _protocol.Connected += HandleConnected;
            _protocol.Reject += HandleReject;
            _protocol.RejectReply += HandleRejectReply;
            _protocol.SyncNetwork += HandleSyncNetwork;
            _protocol.ProxyConfig += HandleProxyConfig;
            _protocol.Disconnected += HandleDisconnected;

            _protocol.ScanReply += HandleScanReply;
            _protocol.ScanReplyEnd += HandleScanReplyEnd;
            _protocol.ExternalProxy += HandleExternalProxy;

            _protocol.Ping += HandlePing;
            _protocol.NetworkError += HandleNetworkError;

            _config = config;
            _useP2pProxy = !config.MultiplayerDisableP2p;
        }

        private void TimeoutConnection()
        {
            _connected.Reset();

            DisconnectAsync();

            while (IsConnected)
            {
                Thread.Yield();
            }
        }

        private bool EnsureConnected()
        {
            if (IsConnected)
            {
                return true;
            }

            _error.Reset();

            ConnectAsync();

            int index = WaitHandle.WaitAny([_connected, _error], FailureTimeout);

            if (IsConnected)
            {
                SendAsync(_protocol.Encode(PacketId.Initialize, InitializeMemory));
            }

            return index == 0 && IsConnected;
        }

        private void UpdatePassphraseIfNeeded()
        {
            string passphrase = _config.MultiplayerLdnPassphrase ?? string.Empty;
            if (passphrase != _passphrase)
            {
                _passphrase = passphrase;

                SendAsync(_protocol.Encode(PacketId.Passphrase, StringUtils.GetFixedLengthBytes(passphrase, 0x80, Encoding.UTF8)));
            }
        }

        protected override void OnConnected()
        {
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, $"LDN TCP client connected a new session with Id {Id}");

            UpdatePassphraseIfNeeded();

            _connected.Set();
        }

        protected override void OnDisconnected()
        {
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, $"LDN TCP client disconnected a session with Id {Id}");

            _passphrase = null;

            _connected.Reset();

            if (_networkConnected)
            {
                DisconnectInternal();
            }
        }

        public void DisconnectAndStop()
        {
            _timeout.Dispose();

            DisconnectAsync();

            while (IsConnected)
            {
                Thread.Yield();
            }

            Dispose();
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            _protocol.Read(buffer, (int)offset, (int)size);
        }

        protected override void OnError(SocketError error)
        {
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, $"LDN TCP client caught an error with code {error}");

            _error.Set();
        }



        private void HandleInitialize(LdnHeader header, InitializeMessage initialize)
        {
            InitializeMemory = initialize;
        }

        private void HandleExternalProxy(LdnHeader header, ExternalProxyConfig config)
        {
            int length = config.AddressFamily switch
            {
                AddressFamily.InterNetwork => 4,
                AddressFamily.InterNetworkV6 => 16,
                _ => 0
            };

            if (length == 0)
            {
                return; // Invalid external proxy.
            }

            IPAddress address = new(config.ProxyIp.AsSpan()[..length].ToArray());
            P2pProxyClient proxy = new(address.ToString(), config.ProxyPort);

            _connectedProxy = proxy;

            bool success = proxy.PerformAuth(config);

            if (!success)
            {
                DisconnectInternal();
            }
        }

        private void HandlePing(LdnHeader header, PingMessage ping)
        {
            if (ping.Requester == 0) // Server requested.
            {
                // Send the ping message back.

                SendAsync(_protocol.Encode(PacketId.Ping, ping));
            }
        }

        private void HandleNetworkError(LdnHeader header, NetworkErrorMessage error)
        {
            if (error.Error == NetworkError.PortUnreachable)
            {
                _useP2pProxy = false;
            }
            else
            {
                _lastError = error.Error;
            }
        }

        private NetworkError ConsumeNetworkError()
        {
            NetworkError result = _lastError;

            _lastError = NetworkError.None;

            return result;
        }

        private void HandleSyncNetwork(LdnHeader header, NetworkInfo info)
        {
            NetworkChange?.Invoke(this, new NetworkChangeEventArgs(info, true));
        }

        private void HandleConnected(LdnHeader header, NetworkInfo info)
        {
            _networkConnected = true;
            _disconnectReason = DisconnectReason.None;

            _apConnected.Set();

            NetworkChange?.Invoke(this, new NetworkChangeEventArgs(info, true));
        }

        private void HandleDisconnected(LdnHeader header, DisconnectMessage message)
        {
            DisconnectInternal();
        }

        private void HandleReject(LdnHeader header, RejectRequest reject)
        {
            // When the client receives a Reject request, we have been rejected and will be disconnected shortly.
            _disconnectReason = reject.DisconnectReason;
        }

        private void HandleRejectReply(LdnHeader header)
        {
            _reject.Set();
        }

        private void HandleScanReply(LdnHeader header, NetworkInfo info)
        {
            _availableGames.Add(info);
        }

        private void HandleScanReplyEnd(LdnHeader obj)
        {
            _scan.Set();
        }

        private void DisconnectInternal()
        {
            if (_networkConnected)
            {
                _networkConnected = false;

                _hostedProxy?.Dispose();
                _hostedProxy = null;

                _connectedProxy?.Dispose();
                _connectedProxy = null;

                _apConnected.Reset();

                NetworkChange?.Invoke(this, new NetworkChangeEventArgs(new NetworkInfo(), false, _disconnectReason));

                if (IsConnected)
                {
                    _timeout.RefreshTimeout();
                }
            }
        }

        public void DisconnectNetwork()
        {
            if (_networkConnected)
            {
                SendAsync(_protocol.Encode(PacketId.Disconnect, new DisconnectMessage()));

                DisconnectInternal();
            }
        }

        public ResultCode Reject(DisconnectReason disconnectReason, uint nodeId)
        {
            if (_networkConnected)
            {
                _reject.Reset();

                SendAsync(_protocol.Encode(PacketId.Reject, new RejectRequest(disconnectReason, nodeId)));

                int index = WaitHandle.WaitAny([_reject, _error], InactiveTimeout);

                if (index == 0)
                {
                    return (ConsumeNetworkError() != NetworkError.None) ? ResultCode.InvalidState : ResultCode.Success;
                }
            }

            return ResultCode.InvalidState;
        }

        public void SetAdvertiseData(byte[] data)
        {
            // TODO: validate we're the owner (the server will do this anyways tho)
            if (_networkConnected)
            {
                SendAsync(_protocol.Encode(PacketId.SetAdvertiseData, data));
            }
        }

        public void SetGameVersion(byte[] versionString)
        {
            _gameVersion = versionString;

            if (_gameVersion.Length < 0x10)
            {
                Array.Resize(ref _gameVersion, 0x10);
            }
        }

        public void SetStationAcceptPolicy(AcceptPolicy acceptPolicy)
        {
            // TODO: validate we're the owner (the server will do this anyways tho)
            if (_networkConnected)
            {
                SendAsync(_protocol.Encode(PacketId.SetAcceptPolicy, new SetAcceptPolicyRequest
                {
                    StationAcceptPolicy = acceptPolicy
                }));
            }
        }

        private void DisposeProxy()
        {
            _hostedProxy?.Dispose();
            _hostedProxy = null;
        }

        private void ConfigureAccessPoint(ref RyuNetworkConfig request)
        {
            _gameVersion.AsSpan().CopyTo(request.GameVersion.AsSpan());

            if (_useP2pProxy)
            {
                // Before sending the request, attempt to set up a proxy server.
                // This can be on a range of private ports, which can be exposed on a range of public
                // ports via UPnP. If any of this fails, we just fall back to using the master server.

                int i = 0;
                for (; i < P2pProxyServer.PrivatePortRange; i++)
                {
                    _hostedProxy = new P2pProxyServer(this, (ushort)(P2pProxyServer.PrivatePortBase + i), _protocol);

                    try
                    {
                        _hostedProxy.Start();

                        break;
                    }
                    catch (SocketException e)
                    {
                        _hostedProxy.Dispose();
                        _hostedProxy = null;

                        if (e.SocketErrorCode != SocketError.AddressAlreadyInUse)
                        {
                            i = P2pProxyServer.PrivatePortRange; // Immediately fail.
                        }
                    }
                }

                bool openSuccess = i < P2pProxyServer.PrivatePortRange;

                if (openSuccess)
                {
                    Task<ushort> natPunchResult = _hostedProxy.NatPunch();

                    try
                    {
                        if (natPunchResult.Result != 0)
                        {
                            // Tell the server that we are hosting the proxy.
                            request.ExternalProxyPort = natPunchResult.Result;
                        }
                    }
                    catch (Exception) { }

                    if (request.ExternalProxyPort == 0)
                    {
                        Logger.Warning?.Print(LogClass.ServiceLdn, "Failed to open a port with UPnP for P2P connection. Proxying through the master server instead. Expect higher latency.");
                        _hostedProxy.Dispose();
                    }
                    else
                    {
                        Logger.Info?.Print(LogClass.ServiceLdn, $"Created a wireless P2P network on port {request.ExternalProxyPort}.");
                        _hostedProxy.Start();

                        (_, UnicastIPAddressInformation unicastAddress) = NetworkHelpers.GetLocalInterface();

                        unicastAddress.Address.GetAddressBytes().AsSpan().CopyTo(request.PrivateIp.AsSpan());
                        request.InternalProxyPort = _hostedProxy.PrivatePort;
                        request.AddressFamily = unicastAddress.Address.AddressFamily;
                    }
                }
                else
                {
                    Logger.Warning?.Print(LogClass.ServiceLdn, "Cannot create a P2P server. Proxying through the master server instead. Expect higher latency.");
                }
            }
        }

        private bool CreateNetworkCommon()
        {
            bool signalled = _apConnected.WaitOne(FailureTimeout);

            if (!_useP2pProxy && _hostedProxy != null)
            {
                Logger.Warning?.Print(LogClass.ServiceLdn, "Locally hosted proxy server was not externally reachable. Proxying through the master server instead. Expect higher latency.");

                DisposeProxy();
            }

            if (signalled && _connectedProxy != null)
            {
                _connectedProxy.EnsureProxyReady();

                Config = _connectedProxy.ProxyConfig;
            }
            else
            {
                DisposeProxy();
            }

            return signalled;
        }

        public bool CreateNetwork(CreateAccessPointRequest request, byte[] advertiseData)
        {
            _timeout.DisableTimeout();

            ConfigureAccessPoint(ref request.RyuNetworkConfig);

            if (!EnsureConnected())
            {
                DisposeProxy();

                return false;
            }

            UpdatePassphraseIfNeeded();

            SendAsync(_protocol.Encode(PacketId.CreateAccessPoint, request, advertiseData));

            // Send a network change event with dummy data immediately. Necessary to avoid crashes in some games
            NetworkChangeEventArgs networkChangeEvent = new(new NetworkInfo()
            {
                Common = new CommonNetworkInfo()
                {
                    MacAddress = InitializeMemory.MacAddress,
                    Channel = request.NetworkConfig.Channel,
                    LinkLevel = 3,
                    NetworkType = 2,
                    Ssid = new Ssid()
                    {
                        Length = 32
                    }
                },
                Ldn = new LdnNetworkInfo()
                {
                    AdvertiseDataSize = (ushort)advertiseData.Length,
                    AuthenticationId = 0,
                    NodeCount = 1,
                    NodeCountMax = request.NetworkConfig.NodeCountMax,
                    SecurityMode = (ushort)request.SecurityConfig.SecurityMode
                }
            }, true);
            networkChangeEvent.Info.Ldn.Nodes[0] = new NodeInfo()
            {
                Ipv4Address = 175243265,
                IsConnected = 1,
                LocalCommunicationVersion = request.NetworkConfig.LocalCommunicationVersion,
                MacAddress = InitializeMemory.MacAddress,
                NodeId = 0,
                UserName = request.UserConfig.UserName
            };
            "12345678123456781234567812345678"u8.ToArray().CopyTo(networkChangeEvent.Info.Common.Ssid.Name.AsSpan());
            NetworkChange?.Invoke(this, networkChangeEvent);

            return CreateNetworkCommon();
        }

        public bool CreateNetworkPrivate(CreateAccessPointPrivateRequest request, byte[] advertiseData)
        {
            _timeout.DisableTimeout();

            ConfigureAccessPoint(ref request.RyuNetworkConfig);

            if (!EnsureConnected())
            {
                DisposeProxy();

                return false;
            }

            UpdatePassphraseIfNeeded();

            SendAsync(_protocol.Encode(PacketId.CreateAccessPointPrivate, request, advertiseData));

            return CreateNetworkCommon();
        }

        public NetworkInfo[] Scan(ushort channel, ScanFilter scanFilter)
        {
            if (!_networkConnected)
            {
                _timeout.RefreshTimeout();
            }

            _availableGames.Clear();

            int index = -1;

            if (EnsureConnected())
            {
                UpdatePassphraseIfNeeded();

                _scan.Reset();

                SendAsync(_protocol.Encode(PacketId.Scan, scanFilter));

                index = WaitHandle.WaitAny([_scan, _error], ScanTimeout);
            }

            if (index != 0)
            {
                // An error occurred or timeout. Write 0 games.
                return [];
            }

            return _availableGames.ToArray();
        }

        private NetworkError ConnectCommon()
        {
            bool signalled = _apConnected.WaitOne(FailureTimeout);

            NetworkError error = ConsumeNetworkError();

            if (error != NetworkError.None)
            {
                return error;
            }

            if (signalled && _connectedProxy != null)
            {
                _connectedProxy.EnsureProxyReady();

                Config = _connectedProxy.ProxyConfig;
            }

            return signalled ? NetworkError.None : NetworkError.ConnectTimeout;
        }

        public NetworkError Connect(ConnectRequest request)
        {
            _timeout.DisableTimeout();

            if (!EnsureConnected())
            {
                return NetworkError.Unknown;
            }

            SendAsync(_protocol.Encode(PacketId.Connect, request));

            NetworkChangeEventArgs networkChangeEvent = new(new NetworkInfo()
            {
                Common = request.NetworkInfo.Common,
                Ldn = request.NetworkInfo.Ldn
            }, true);

            NetworkChange?.Invoke(this, networkChangeEvent);

            return ConnectCommon();
        }

        public NetworkError ConnectPrivate(ConnectPrivateRequest request)
        {
            _timeout.DisableTimeout();

            if (!EnsureConnected())
            {
                return NetworkError.Unknown;
            }

            SendAsync(_protocol.Encode(PacketId.ConnectPrivate, request));

            return ConnectCommon();
        }

        private void HandleProxyConfig(LdnHeader header, ProxyConfig config)
        {
            Config = config;

            SocketHelpers.RegisterProxy(new LdnProxy(config, this, _protocol));
        }
    }
}
