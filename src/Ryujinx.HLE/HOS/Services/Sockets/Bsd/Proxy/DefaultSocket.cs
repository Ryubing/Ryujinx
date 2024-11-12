using Ryujinx.Common.Utilities;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Ryujinx.HLE.HOS.Services.Sockets.Bsd.Proxy
{
    class DefaultSocket : ISocketImpl
    {
        public Socket BaseSocket { get; }

        public EndPoint RemoteEndPoint => BaseSocket.RemoteEndPoint;

        public EndPoint LocalEndPoint => BaseSocket.LocalEndPoint;

        public bool Connected => BaseSocket.Connected;

        public bool IsBound => BaseSocket.IsBound;

        public AddressFamily AddressFamily => BaseSocket.AddressFamily;

        public SocketType SocketType => BaseSocket.SocketType;

        public ProtocolType ProtocolType => BaseSocket.ProtocolType;

        public bool Blocking { get => BaseSocket.Blocking; set => BaseSocket.Blocking = value; }

        public int Available => BaseSocket.Available;

        private readonly string _lanInterfaceId;

        public DefaultSocket(Socket baseSocket, string lanInterfaceId)
        {
            _lanInterfaceId = lanInterfaceId;

            BaseSocket = baseSocket;
        }

        public DefaultSocket(AddressFamily domain, SocketType type, ProtocolType protocol, string lanInterfaceId)
        {
            _lanInterfaceId = lanInterfaceId;

            BaseSocket = new Socket(domain, type, protocol);
        }

        private void EnsureNetworkInterfaceBound()
        {
            if (_lanInterfaceId != "0" && !BaseSocket.IsBound)
            {
                (_, UnicastIPAddressInformation ipInfo) = NetworkHelpers.GetLocalInterface(_lanInterfaceId);

                BaseSocket.Bind(new IPEndPoint(ipInfo.Address, 0));
            }
        }

        public ISocketImpl Accept()
        {
            return new DefaultSocket(BaseSocket.Accept(), _lanInterfaceId);
        }

        public void Bind(EndPoint localEP)
        {
            // NOTE: The guest is able to receive on 0.0.0.0 without it being limited to the chosen network interface.
            // This is because it must get loopback traffic as well. This could allow other network traffic to leak in.

            BaseSocket.Bind(localEP);
        }

        public void Close()
        {
            BaseSocket.Close();
        }

        public void Connect(EndPoint remoteEP)
        {
            EnsureNetworkInterfaceBound();

            BaseSocket.Connect(remoteEP);
        }

        public void Disconnect(bool reuseSocket)
        {
            BaseSocket.Disconnect(reuseSocket);
        }

        public void GetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue)
        {
            BaseSocket.GetSocketOption(optionLevel, optionName, optionValue);
        }

        public void Listen(int backlog)
        {
            BaseSocket.Listen(backlog);
        }

        public int Receive(Span<byte> buffer)
        {
            EnsureNetworkInterfaceBound();

            return BaseSocket.Receive(buffer);
        }

        public int Receive(Span<byte> buffer, SocketFlags flags)
        {
            EnsureNetworkInterfaceBound();

            return BaseSocket.Receive(buffer, flags);
        }

        public int Receive(Span<byte> buffer, SocketFlags flags, out SocketError socketError)
        {
            EnsureNetworkInterfaceBound();

            return BaseSocket.Receive(buffer, flags, out socketError);
        }

        public int ReceiveFrom(Span<byte> buffer, SocketFlags flags, ref EndPoint remoteEP)
        {
            EnsureNetworkInterfaceBound();

            return BaseSocket.ReceiveFrom(buffer, flags, ref remoteEP);
        }

        public int Send(ReadOnlySpan<byte> buffer)
        {
            EnsureNetworkInterfaceBound();

            return BaseSocket.Send(buffer);
        }

        public int Send(ReadOnlySpan<byte> buffer, SocketFlags flags)
        {
            EnsureNetworkInterfaceBound();

            return BaseSocket.Send(buffer, flags);
        }

        public int Send(ReadOnlySpan<byte> buffer, SocketFlags flags, out SocketError socketError)
        {
            EnsureNetworkInterfaceBound();

            return BaseSocket.Send(buffer, flags, out socketError);
        }

        public int SendTo(ReadOnlySpan<byte> buffer, SocketFlags flags, EndPoint remoteEP)
        {
            EnsureNetworkInterfaceBound();

            return BaseSocket.SendTo(buffer, flags, remoteEP);
        }

        public bool Poll(int microSeconds, SelectMode mode)
        {
            return BaseSocket.Poll(microSeconds, mode);
        }

        public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, int optionValue)
        {
            BaseSocket.SetSocketOption(optionLevel, optionName, optionValue);
        }

        public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, object optionValue)
        {
            BaseSocket.SetSocketOption(optionLevel, optionName, optionValue);
        }

        public void Shutdown(SocketShutdown how)
        {
            BaseSocket.Shutdown(how);
        }

        public void Dispose()
        {
            BaseSocket.Dispose();
        }
    }
}
