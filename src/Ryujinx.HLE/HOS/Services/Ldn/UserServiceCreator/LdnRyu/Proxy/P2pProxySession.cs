using NetCoreServer;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnRyu.Types;
using System;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnRyu.Proxy
{
    class P2pProxySession : TcpSession
    {
        public uint VirtualIpAddress { get; private set; }
        public RyuLdnProtocol Protocol { get; }

        private readonly P2pProxyServer _parent;

        private bool _masterClosed;

        public P2pProxySession(P2pProxyServer server) : base(server)
        {
            _parent = server;

            Protocol = new RyuLdnProtocol();

            Protocol.ProxyDisconnect += HandleProxyDisconnect;
            Protocol.ProxyData += HandleProxyData;
            Protocol.ProxyConnectReply += HandleProxyConnectReply;
            Protocol.ProxyConnect += HandleProxyConnect;

            Protocol.ExternalProxy += HandleAuthentication;
        }

        private void HandleAuthentication(LdnHeader header, ExternalProxyConfig token)
        {
            if (!_parent.TryRegisterUser(this, token))
            {
                Disconnect();
            }
        }

        public void SetIpv4(uint ip)
        {
            VirtualIpAddress = ip;
        }

        public void DisconnectAndStop()
        {
            _masterClosed = true;

            Disconnect();
        }

        protected override void OnDisconnected()
        {
            if (!_masterClosed)
            {
                _parent.DisconnectProxyClient(this);
            }
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            try
            {
                Protocol.Read(buffer, (int)offset, (int)size);
            }
            catch (Exception)
            {
                Disconnect();
            }
        }

        private void HandleProxyDisconnect(LdnHeader header, ProxyDisconnectMessage message)
        {
            _parent.HandleProxyDisconnect(this, header, message);
        }

        private void HandleProxyData(LdnHeader header, ProxyDataHeader message, byte[] data)
        {
            _parent.HandleProxyData(this, header, message, data);
        }

        private void HandleProxyConnectReply(LdnHeader header, ProxyConnectResponse data)
        {
            _parent.HandleProxyConnectReply(this, header, data);
        }

        private void HandleProxyConnect(LdnHeader header, ProxyConnectRequest message)
        {
            _parent.HandleProxyConnect(this, header, message);
        }
    }
}
