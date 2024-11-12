using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnRyu.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Types;
using Ryujinx.HLE.HOS.Services.Sockets.Bsd.Proxy;
using System.Net.Sockets;
using System.Threading;
using TcpClient = NetCoreServer.TcpClient;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnRyu.Proxy
{
    class P2pProxyClient : TcpClient, IProxyClient
    {
        private const int FailureTimeout = 4000;

        public ProxyConfig ProxyConfig { get; private set; }

        private readonly RyuLdnProtocol _protocol;

        private readonly ManualResetEvent _connected = new ManualResetEvent(false);
        private readonly ManualResetEvent _ready = new ManualResetEvent(false);
        private readonly AutoResetEvent _error = new AutoResetEvent(false);

        public P2pProxyClient(string address, int port) : base(address, port)
        {
            if (ProxyHelpers.SupportsNoDelay())
            {
                OptionNoDelay = true;
            }

            _protocol = new RyuLdnProtocol();

            _protocol.ProxyConfig += HandleProxyConfig;

            ConnectAsync();
        }

        protected override void OnConnected()
        {
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, $"Proxy TCP client connected a new session with Id {Id}");

            _connected.Set();
        }

        protected override void OnDisconnected()
        {
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, $"Proxy TCP client disconnected a session with Id {Id}");

            SocketHelpers.UnregisterProxy();

            _connected.Reset();
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            _protocol.Read(buffer, (int)offset, (int)size);
        }

        protected override void OnError(SocketError error)
        {
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, $"Proxy TCP client caught an error with code {error}");

            _error.Set();
        }

        private void HandleProxyConfig(LdnHeader header, ProxyConfig config)
        {
            ProxyConfig = config;

            SocketHelpers.RegisterProxy(new LdnProxy(config, this, _protocol));

            _ready.Set();
        }

        public bool EnsureProxyReady()
        {
            return _ready.WaitOne(FailureTimeout);
        }

        public bool PerformAuth(ExternalProxyConfig config)
        {
            bool signalled = _connected.WaitOne(FailureTimeout);

            if (!signalled)
            {
                return false;
            }

            SendAsync(_protocol.Encode(PacketId.ExternalProxy, config));

            return true;
        }
    }
}
