using Ryujinx.Common.Helper;
using System.Text.RegularExpressions;

namespace Ryujinx.HLE.HOS.Services.Sockets.Sfdnsres.Proxy
{
    static class DnsBlacklist
    {
        public static bool IsHostBlocked(string host)
        {
            foreach (Regex regex in Patterns.BlockedHosts)
            {
                if (regex.IsMatch(host))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
