using System.Collections.Generic;
using System.Threading;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnRyu.Proxy
{
    public class EphemeralPortPool
    {
        private const ushort EphemeralBase = 49152;

        private readonly List<ushort> _ephemeralPorts = new List<ushort>();

        private readonly Lock _lock = new();

        public ushort Get()
        {
            ushort port = EphemeralBase;
            lock (_lock)
            {
                // Starting at the ephemeral port base, return an ephemeral port that is not in use.
                // Returns 0 if the range is exhausted.

                for (int i = 0; i < _ephemeralPorts.Count; i++)
                {
                    ushort existingPort = _ephemeralPorts[i];

                    if (existingPort > port)
                    {
                        // The port was free - take it.
                        _ephemeralPorts.Insert(i, port);

                        return port;
                    }

                    port++;
                }

                if (port != 0)
                {
                    _ephemeralPorts.Add(port);
                }

                return port;
            }
        }

        public void Return(ushort port)
        {
            lock (_lock)
            {
                _ephemeralPorts.Remove(port);
            }
        }
    }
}
