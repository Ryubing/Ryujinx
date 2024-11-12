using System;
using System.Net;
using System.Net.Sockets;

namespace Ryujinx.HLE.HOS.Services.Sockets.Bsd.Proxy
{
    interface ISocketImpl : IDisposable
    {
        EndPoint RemoteEndPoint { get; }
        EndPoint LocalEndPoint { get; }
        bool Connected { get; }
        bool IsBound { get; }

        AddressFamily AddressFamily { get; }
        SocketType SocketType { get; }
        ProtocolType ProtocolType { get; }

        bool Blocking { get; set; }
        int Available { get; }

        int Receive(Span<byte> buffer);
        int Receive(Span<byte> buffer, SocketFlags flags);
        int Receive(Span<byte> buffer, SocketFlags flags, out SocketError socketError);
        int ReceiveFrom(Span<byte> buffer, SocketFlags flags, ref EndPoint remoteEP);

        int Send(ReadOnlySpan<byte> buffer);
        int Send(ReadOnlySpan<byte> buffer, SocketFlags flags);
        int Send(ReadOnlySpan<byte> buffer, SocketFlags flags, out SocketError socketError);
        int SendTo(ReadOnlySpan<byte> buffer, SocketFlags flags, EndPoint remoteEP);

        bool Poll(int microSeconds, SelectMode mode);

        ISocketImpl Accept();

        void Bind(EndPoint localEP);
        void Connect(EndPoint remoteEP);
        void Listen(int backlog);

        void GetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue);
        void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, int optionValue);
        void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, object optionValue);

        void Shutdown(SocketShutdown how);
        void Disconnect(bool reuseSocket);
        void Close();
    }
}
