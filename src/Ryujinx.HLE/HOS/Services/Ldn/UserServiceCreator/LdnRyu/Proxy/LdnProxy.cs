using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnRyu.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Types;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnRyu.Proxy
{
    class LdnProxy : IDisposable
    {
        public EndPoint LocalEndpoint { get; }
        public IPAddress LocalAddress { get; }

        private readonly List<LdnProxySocket> _sockets = new List<LdnProxySocket>();
        private readonly Dictionary<ProtocolType, EphemeralPortPool> _ephemeralPorts = new Dictionary<ProtocolType, EphemeralPortPool>();

        private readonly IProxyClient _parent;
        private RyuLdnProtocol _protocol;
        private readonly uint _subnetMask;
        private readonly uint _localIp;
        private readonly uint _broadcast;

        public LdnProxy(ProxyConfig config, IProxyClient client, RyuLdnProtocol protocol)
        {
            _parent = client;
            _protocol = protocol;

            _ephemeralPorts[ProtocolType.Udp] = new EphemeralPortPool();
            _ephemeralPorts[ProtocolType.Tcp] = new EphemeralPortPool();

            byte[] address = BitConverter.GetBytes(config.ProxyIp);
            Array.Reverse(address);
            LocalAddress = new IPAddress(address);

            _subnetMask = config.ProxySubnetMask;
            _localIp = config.ProxyIp;
            _broadcast = _localIp | (~_subnetMask);

            RegisterHandlers(protocol);
        }

        public bool Supported(AddressFamily domain, SocketType type, ProtocolType protocol)
        {
            if (protocol == ProtocolType.Tcp)
            {
                Logger.Error?.PrintMsg(LogClass.ServiceLdn, "Tcp proxy networking is untested. Please report this game so that it can be tested.");
            }
            return domain == AddressFamily.InterNetwork && (protocol == ProtocolType.Tcp || protocol == ProtocolType.Udp);
        }

        private void RegisterHandlers(RyuLdnProtocol protocol)
        {
            protocol.ProxyConnect += HandleConnectionRequest;
            protocol.ProxyConnectReply += HandleConnectionResponse;
            protocol.ProxyData += HandleData;
            protocol.ProxyDisconnect += HandleDisconnect;

            _protocol = protocol;
        }

        public void UnregisterHandlers(RyuLdnProtocol protocol)
        {
            protocol.ProxyConnect -= HandleConnectionRequest;
            protocol.ProxyConnectReply -= HandleConnectionResponse;
            protocol.ProxyData -= HandleData;
            protocol.ProxyDisconnect -= HandleDisconnect;
        }

        public ushort GetEphemeralPort(ProtocolType type)
        {
            return _ephemeralPorts[type].Get();
        }

        public void ReturnEphemeralPort(ProtocolType type, ushort port)
        {
            _ephemeralPorts[type].Return(port);
        }

        public void RegisterSocket(LdnProxySocket socket)
        {
            lock (_sockets)
            {
                _sockets.Add(socket);
            }
        }

        public void UnregisterSocket(LdnProxySocket socket)
        {
            lock (_sockets)
            {
                _sockets.Remove(socket);
            }
        }

        private void ForRoutedSockets(ProxyInfo info, Action<LdnProxySocket> action)
        {
            lock (_sockets)
            {
                foreach (LdnProxySocket socket in _sockets)
                {
                    // Must match protocol and destination port.
                    if (socket.ProtocolType != info.Protocol || socket.LocalEndPoint is not IPEndPoint endpoint || endpoint.Port != info.DestPort)
                    {
                        continue;
                    }

                    // We can assume packets routed to us have been sent to our destination.
                    // They will either be sent to us, or broadcast packets.

                    action(socket);
                }
            }
        }

        public void HandleConnectionRequest(LdnHeader header, ProxyConnectRequest request)
        {
            ForRoutedSockets(request.Info, (socket) =>
            {
                socket.HandleConnectRequest(request);
            });
        }

        public void HandleConnectionResponse(LdnHeader header, ProxyConnectResponse response)
        {
            ForRoutedSockets(response.Info, (socket) =>
            {
                socket.HandleConnectResponse(response);
            });
        }

        public void HandleData(LdnHeader header, ProxyDataHeader proxyHeader, byte[] data)
        {
            ProxyDataPacket packet = new ProxyDataPacket() { Header = proxyHeader, Data = data };

            ForRoutedSockets(proxyHeader.Info, (socket) =>
            {
                socket.IncomingData(packet);
            });
        }

        public void HandleDisconnect(LdnHeader header, ProxyDisconnectMessage disconnect)
        {
            ForRoutedSockets(disconnect.Info, (socket) =>
            {
                socket.HandleDisconnect(disconnect);
            });
        }

        private uint GetIpV4(IPEndPoint endpoint)
        {
            if (endpoint.AddressFamily != AddressFamily.InterNetwork)
            {
                throw new NotSupportedException();
            }

            byte[] address = endpoint.Address.GetAddressBytes();
            Array.Reverse(address);

            return BitConverter.ToUInt32(address);
        }

        private ProxyInfo MakeInfo(IPEndPoint localEp, IPEndPoint remoteEP, ProtocolType type)
        {
            return new ProxyInfo
            {
                SourceIpV4 = GetIpV4(localEp),
                SourcePort = (ushort)localEp.Port,

                DestIpV4 = GetIpV4(remoteEP),
                DestPort = (ushort)remoteEP.Port,

                Protocol = type
            };
        }

        public void RequestConnection(IPEndPoint localEp, IPEndPoint remoteEp, ProtocolType type)
        {
            // We must ask the other side to initialize a connection, so they can accept a socket for us.

            ProxyConnectRequest request = new ProxyConnectRequest
            {
                Info = MakeInfo(localEp, remoteEp, type)
            };

            _parent.SendAsync(_protocol.Encode(PacketId.ProxyConnect, request));
        }

        public void SignalConnected(IPEndPoint localEp, IPEndPoint remoteEp, ProtocolType type)
        {
            // We must tell the other side that we have accepted their request for connection.

            ProxyConnectResponse request = new ProxyConnectResponse
            {
                Info = MakeInfo(localEp, remoteEp, type)
            };

            _parent.SendAsync(_protocol.Encode(PacketId.ProxyConnectReply, request));
        }

        public void EndConnection(IPEndPoint localEp, IPEndPoint remoteEp, ProtocolType type)
        {
            // We must tell the other side that our connection is dropped.

            ProxyDisconnectMessage request = new ProxyDisconnectMessage
            {
                Info = MakeInfo(localEp, remoteEp, type),
                DisconnectReason = 0 // TODO
            };

            _parent.SendAsync(_protocol.Encode(PacketId.ProxyDisconnect, request));
        }

        public int SendTo(ReadOnlySpan<byte> buffer, SocketFlags flags, IPEndPoint localEp, IPEndPoint remoteEp, ProtocolType type)
        {
            // We send exactly as much as the user wants us to, currently instantly.
            // TODO: handle over "virtual mtu" (we have a max packet size to worry about anyways). fragment if tcp? throw if udp?

            ProxyDataHeader request = new ProxyDataHeader
            {
                Info = MakeInfo(localEp, remoteEp, type),
                DataLength = (uint)buffer.Length
            };

            _parent.SendAsync(_protocol.Encode(PacketId.ProxyData, request, buffer.ToArray()));

            return buffer.Length;
        }

        public bool IsBroadcast(uint ip)
        {
            return ip == _broadcast;
        }

        public bool IsMyself(uint ip)
        {
            return ip == _localIp;
        }

        public void Dispose()
        {
            UnregisterHandlers(_protocol);

            lock (_sockets)
            {
                foreach (LdnProxySocket socket in _sockets)
                {
                    socket.ProxyDestroyed();
                }
            }
        }
    }
}
