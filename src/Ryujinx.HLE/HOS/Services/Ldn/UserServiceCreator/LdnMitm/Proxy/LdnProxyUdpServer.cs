using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Services.Ldn.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnMitm.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnMitm.Proxy
{
    internal class LdnProxyUdpServer : NetCoreServer.UdpServer, ILdnSocket
    {
        private const long ScanFrequency = 1000;

        private readonly LanProtocol _protocol;
        private byte[] _buffer;
        private int _bufferEnd;

        private readonly Lock _scanLock = new();

        private Dictionary<ulong, NetworkInfo> _scanResultsLast = new();
        private Dictionary<ulong, NetworkInfo> _scanResults = new();
        private readonly AutoResetEvent _scanResponse = new(false);
        private long _lastScanTime;

        public LdnProxyUdpServer(LanProtocol protocol, IPAddress address, int port) : base(address, port)
        {
            _protocol = protocol;
            _protocol.Scan += HandleScan;
            _protocol.ScanResponse += HandleScanResponse;
            _buffer = new byte[LanProtocol.BufferSize];
            OptionReuseAddress = true;
            OptionReceiveBufferSize = LanProtocol.RxBufferSizeMax;
            OptionSendBufferSize = LanProtocol.TxBufferSizeMax;

            Start();
        }

        protected override Socket CreateSocket()
        {
            return new Socket(Endpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp)
            {
                EnableBroadcast = true,
            };
        }

        protected override void OnStarted()
        {
            ReceiveAsync();
        }

        protected override void OnReceived(EndPoint endpoint, byte[] buffer, long offset, long size)
        {
            _protocol.Read(ref _buffer, ref _bufferEnd, buffer, (int)offset, (int)size, endpoint);
            ReceiveAsync();
        }

        protected override void OnError(SocketError error)
        {
            Logger.Error?.PrintMsg(LogClass.ServiceLdn, $"LdnProxyUdpServer caught an error with code {error}");
        }

        protected override void Dispose(bool disposingManagedResources)
        {
            _protocol.Scan -= HandleScan;
            _protocol.ScanResponse -= HandleScanResponse;

            _scanResponse.Dispose();

            base.Dispose(disposingManagedResources);
        }

        public bool SendPacketAsync(EndPoint endpoint, byte[] data)
        {
            return SendAsync(endpoint, data);
        }

        private void HandleScan(EndPoint endpoint, LanPacketType type, byte[] data)
        {
            _protocol.SendPacket(this, type, data, endpoint);
        }

        private void HandleScanResponse(NetworkInfo info)
        {
            Span<byte> mac = stackalloc byte[8];

            info.Common.MacAddress.AsSpan().CopyTo(mac);

            lock (_scanLock)
            {
                _scanResults[BitConverter.ToUInt64(mac)] = info;

                _scanResponse.Set();
            }
        }

        public void ClearScanResults()
        {
            // Rate limit scans.

            long timeMs = Stopwatch.GetTimestamp() / (Stopwatch.Frequency / 1000);
            long delay = ScanFrequency - (timeMs - _lastScanTime);

            if (delay > 0)
            {
                Thread.Sleep((int)delay);
            }

            _lastScanTime = timeMs;

            lock (_scanLock)
            {
                var newResults = _scanResultsLast;
                newResults.Clear();

                _scanResultsLast = _scanResults;
                _scanResults = newResults;

                _scanResponse.Reset();
            }
        }

        public Dictionary<ulong, NetworkInfo> GetScanResults()
        {
            // NOTE: Try to minimize waiting time for scan results.
            // After we receive the first response, wait a short time for follow-ups and return.
            // Responses that were too late to catch will appear in the next scan.

            // ldn_mitm does not do this, but this improves latency for games that expect it to be low (it is on console).

            if (_scanResponse.WaitOne(1000))
            {
                // Wait a short while longer in case there are some other responses.
                Thread.Sleep(33);
            }

            lock (_scanLock)
            {
                var results = new Dictionary<ulong, NetworkInfo>();

                foreach (KeyValuePair<ulong, NetworkInfo> last in _scanResultsLast)
                {
                    results[last.Key] = last.Value;
                }

                foreach (KeyValuePair<ulong, NetworkInfo> scan in _scanResults)
                {
                    results[scan.Key] = scan.Value;
                }

                return results;
            }
        }
    }
}
