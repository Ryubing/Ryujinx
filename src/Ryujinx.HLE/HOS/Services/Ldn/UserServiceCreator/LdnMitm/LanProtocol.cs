using Gommon;
using Ryujinx.Common.Logging;
using Ryujinx.Common.Memory;
using Ryujinx.Common.Utilities;
using Ryujinx.HLE.HOS.Services.Ldn.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnMitm.Proxy;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnMitm.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnMitm
{
    internal class LanProtocol
    {
        private const uint LanMagic = 0x11451400;

        public const int BufferSize = 2048;
        public const int TcpTxBufferSize = 0x800;
        public const int TcpRxBufferSize = 0x1000;
        public const int TxBufferSizeMax = 0x2000;
        public const int RxBufferSizeMax = 0x2000;

        private readonly int _headerSize = Marshal.SizeOf<LanPacketHeader>();

        private readonly LanDiscovery _discovery;

        public event Action<LdnProxyTcpSession> Accept;
        public event Action<EndPoint, LanPacketType, byte[]> Scan;
        public event Action<NetworkInfo> ScanResponse;
        public event Action<NetworkInfo> SyncNetwork;
        public event Action<NodeInfo, EndPoint> Connect;
        public event Action<LdnProxyTcpSession> DisconnectStation;

        public LanProtocol(LanDiscovery parent)
        {
            _discovery = parent;
        }

        public void InvokeAccept(LdnProxyTcpSession session)
        {
            Accept?.Invoke(session);
        }

        public void InvokeDisconnectStation(LdnProxyTcpSession session)
        {
            DisconnectStation?.Invoke(session);
        }

        private void DecodeAndHandle(LanPacketHeader header, byte[] data, EndPoint endPoint = null)
        {
            switch (header.Type)
            {
                case LanPacketType.Scan:
                    // UDP
                    if (_discovery.IsHost)
                    {
                        Scan?.Invoke(endPoint, LanPacketType.ScanResponse, SpanHelpers.AsSpan<NetworkInfo, byte>(ref _discovery.NetworkInfo).ToArray());
                    }
                    break;
                case LanPacketType.ScanResponse:
                    // UDP
                    ScanResponse?.Invoke(MemoryMarshal.Cast<byte, NetworkInfo>(data)[0]);
                    break;
                case LanPacketType.SyncNetwork:
                    // TCP
                    SyncNetwork?.Invoke(MemoryMarshal.Cast<byte, NetworkInfo>(data)[0]);
                    break;
                case LanPacketType.Connect:
                    // TCP Session / Station
                    Connect?.Invoke(MemoryMarshal.Cast<byte, NodeInfo>(data)[0], endPoint);
                    break;
                default:
                    Logger.Error?.PrintMsg(LogClass.ServiceLdn, $"Decode error: Unhandled type {header.Type}");
                    break;
            }
        }

        public void Read(scoped ref byte[] buffer, scoped ref int bufferEnd, byte[] data, int offset, int size, EndPoint endPoint = null)
        {
            if (endPoint != null && _discovery.LocalAddr.Equals(((IPEndPoint)endPoint).Address))
            {
                return;
            }

            int index = 0;
            while (index < size)
            {
                if (bufferEnd < _headerSize)
                {
                    int copyable2 = Math.Min(size - index, Math.Min(size, _headerSize - bufferEnd));

                    Array.Copy(data, index + offset, buffer, bufferEnd, copyable2);

                    index += copyable2;
                    bufferEnd += copyable2;
                }

                if (bufferEnd >= _headerSize)
                {
                    LanPacketHeader header = MemoryMarshal.Cast<byte, LanPacketHeader>(buffer)[0];
                    if (header.Magic != LanMagic)
                    {
                        bufferEnd = 0;

                        Logger.Warning?.PrintMsg(LogClass.ServiceLdn, $"Invalid magic number in received packet. [magic: {header.Magic}] [EP: {endPoint}]");

                        return;
                    }

                    int totalSize = _headerSize + header.Length;
                    if (totalSize > BufferSize)
                    {
                        bufferEnd = 0;

                        Logger.Error?.PrintMsg(LogClass.ServiceLdn, $"Max packet size {BufferSize} exceeded.");

                        return;
                    }

                    int copyable = Math.Min(size - index, Math.Min(size, totalSize - bufferEnd));

                    Array.Copy(data, index + offset, buffer, bufferEnd, copyable);

                    index += copyable;
                    bufferEnd += copyable;

                    if (totalSize == bufferEnd)
                    {
                        byte[] ldnData = new byte[totalSize - _headerSize];
                        Array.Copy(buffer, _headerSize, ldnData, 0, ldnData.Length);

                        if (header.Compressed == 1)
                        {
                            if (Decompress(ldnData, out byte[] decompressedLdnData) != 0)
                            {
                                Logger.Error?.PrintMsg(LogClass.ServiceLdn, $"Decompress error:\n {header}, {_headerSize}\n {ldnData}, {ldnData.Length}");

                                return;
                            }

                            if (decompressedLdnData.Length != header.DecompressLength)
                            {
                                Logger.Error?.PrintMsg(LogClass.ServiceLdn, $"Decompress error: length does not match. ({decompressedLdnData.Length} != {header.DecompressLength})");
                                Logger.Error?.PrintMsg(LogClass.ServiceLdn, $"Decompress error data: '{decompressedLdnData.Select(x => (int)x).JoinToString(string.Empty)}'");

                                return;
                            }

                            ldnData = decompressedLdnData;
                        }

                        DecodeAndHandle(header, ldnData, endPoint);

                        bufferEnd = 0;
                    }
                }
            }
        }

        public int SendBroadcast(ILdnSocket s, LanPacketType type, int port)
        {
            return SendPacket(s, type, [], new IPEndPoint(_discovery.LocalBroadcastAddr, port));
        }

        public int SendPacket(ILdnSocket s, LanPacketType type, byte[] data, EndPoint endPoint = null)
        {
            byte[] buf = PreparePacket(type, data);

            return s.SendPacketAsync(endPoint, buf) ? 0 : -1;
        }

        public int SendPacket(LdnProxyTcpSession s, LanPacketType type, byte[] data)
        {
            byte[] buf = PreparePacket(type, data);

            return s.SendAsync(buf) ? 0 : -1;
        }

        private LanPacketHeader PrepareHeader(LanPacketHeader header, LanPacketType type)
        {
            header.Magic = LanMagic;
            header.Type = type;
            header.Compressed = 0;
            header.Length = 0;
            header.DecompressLength = 0;
            header.Reserved = new Array2<byte>();

            return header;
        }

        private byte[] PreparePacket(LanPacketType type, byte[] data)
        {
            LanPacketHeader header = PrepareHeader(new LanPacketHeader(), type);
            header.Length = (ushort)data.Length;

            byte[] buf;
            if (data.Length > 0)
            {
                if (Compress(data, out byte[] compressed) == 0)
                {
                    header.DecompressLength = header.Length;
                    header.Length = (ushort)compressed.Length;
                    header.Compressed = 1;

                    buf = new byte[compressed.Length + _headerSize];

                    SpanHelpers.AsSpan<LanPacketHeader, byte>(ref header).ToArray().CopyTo(buf, 0);
                    compressed.CopyTo(buf, _headerSize);
                }
                else
                {
                    buf = new byte[data.Length + _headerSize];

                    Logger.Error?.PrintMsg(LogClass.ServiceLdn, "Compressing packet data failed.");

                    SpanHelpers.AsSpan<LanPacketHeader, byte>(ref header).ToArray().CopyTo(buf, 0);
                    data.CopyTo(buf, _headerSize);
                }
            }
            else
            {
                buf = new byte[_headerSize];
                SpanHelpers.AsSpan<LanPacketHeader, byte>(ref header).ToArray().CopyTo(buf, 0);
            }

            return buf;
        }

        private int Compress(byte[] input, out byte[] output)
        {
            List<byte> outputList = [];
            int i = 0;
            int maxCount = 0xFF;

            while (i < input.Length)
            {
                byte inputByte = input[i++];
                int count = 0;

                if (inputByte == 0)
                {
                    while (i < input.Length && input[i] == 0 && count < maxCount)
                    {
                        count += 1;
                        i++;
                    }
                }

                if (inputByte == 0)
                {
                    outputList.Add(0);

                    if (outputList.Count == BufferSize)
                    {
                        output = null;

                        return -1;
                    }

                    outputList.Add((byte)count);
                }
                else
                {
                    outputList.Add(inputByte);
                }
            }

            output = outputList.ToArray();

            return i == input.Length ? 0 : -1;
        }

        private int Decompress(byte[] input, out byte[] output)
        {
            List<byte> outputList = [];
            int i = 0;

            while (i < input.Length && outputList.Count < BufferSize)
            {
                byte inputByte = input[i++];

                outputList.Add(inputByte);

                if (inputByte == 0)
                {
                    if (i == input.Length)
                    {
                        output = null;

                        return -1;
                    }

                    int count = input[i++];

                    for (int j = 0; j < count; j++)
                    {
                        if (outputList.Count == BufferSize)
                        {
                            break;
                        }

                        outputList.Add(inputByte);
                    }
                }
            }

            output = outputList.ToArray();

            return i == input.Length ? 0 : -1;
        }
    }
}
