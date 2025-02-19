using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnRyu.Proxy;
using Ryujinx.HLE.HOS.Services.Sockets.Bsd.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace Ryujinx.HLE.HOS.Services.Sockets.Bsd.Proxy
{
    static class SocketHelpers
    {
        private static LdnProxy _proxy;

        public static void Select(List<ISocketImpl> readEvents, List<ISocketImpl> writeEvents, List<ISocketImpl> errorEvents, int timeout)
        {
            List<Socket> readDefault = readEvents.Select(x => (x as DefaultSocket)?.BaseSocket).Where(x => x != null).ToList();
            List<Socket> writeDefault = writeEvents.Select(x => (x as DefaultSocket)?.BaseSocket).Where(x => x != null).ToList();
            List<Socket> errorDefault = errorEvents.Select(x => (x as DefaultSocket)?.BaseSocket).Where(x => x != null).ToList();

            if (readDefault.Count != 0 || writeDefault.Count != 0 || errorDefault.Count != 0)
            {
                Socket.Select(readDefault, writeDefault, errorDefault, timeout);
            }

            void FilterSockets(List<ISocketImpl> removeFrom, List<Socket> selectedSockets, Func<LdnProxySocket, bool> ldnCheck)
            {
                removeFrom.RemoveAll(socket =>
                {
                    switch (socket)
                    {
                        case DefaultSocket dsocket:
                            return !selectedSockets.Contains(dsocket.BaseSocket);
                        case LdnProxySocket psocket:
                            return !ldnCheck(psocket);
                        default:
                            throw new NotImplementedException();
                    }
                });
            };

            FilterSockets(readEvents, readDefault, (socket) => socket.Readable);
            FilterSockets(writeEvents, writeDefault, (socket) => socket.Writable);
            FilterSockets(errorEvents, errorDefault, (socket) => socket.Error);
        }

        public static void RegisterProxy(LdnProxy proxy)
        {
            if (_proxy != null)
            {
                UnregisterProxy();
            }

            _proxy = proxy;
        }

        public static void UnregisterProxy()
        {
            _proxy?.Dispose();
            _proxy = null;
        }

        public static ISocketImpl CreateSocket(AddressFamily domain, SocketType type, ProtocolType protocol, string lanInterfaceId)
        {
            if (_proxy != null)
            {
                if (_proxy.Supported(domain, type, protocol))
                {
                    Logger.Info?.PrintMsg(LogClass.ServiceBsd, $"Socket is using LDN proxy");
                    return new LdnProxySocket(domain, type, protocol, _proxy);
                }
                else
                {
                    Logger.Warning?.PrintMsg(LogClass.ServiceBsd, $"LDN proxy does not support socket {domain}, {type}, {protocol}");
                }
            }
            else
            {
                Logger.Info?.PrintMsg(LogClass.ServiceBsd, $"Opening socket using host networking stack");
            }
            return new DefaultSocket(domain, type, protocol, lanInterfaceId);
        }
    }
}
