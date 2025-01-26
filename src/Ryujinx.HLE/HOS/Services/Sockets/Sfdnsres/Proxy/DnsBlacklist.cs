using System.Text.RegularExpressions;

namespace Ryujinx.HLE.HOS.Services.Sockets.Sfdnsres.Proxy
{
    static partial class DnsBlacklist
    {
        const RegexOptions RegexOpts = RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture;

        [GeneratedRegex(@"^(.*)\-lp1\.(n|s)\.n\.srv\.nintendo\.net$", RegexOpts)]
        private static partial Regex BlockedHost1();
        [GeneratedRegex(@"^(.*)\-lp1\.lp1\.t\.npln\.srv\.nintendo\.net$", RegexOpts)]
        private static partial Regex BlockedHost2();
        [GeneratedRegex(@"^(.*)\-lp1\.(znc|p)\.srv\.nintendo\.net$", RegexOpts)]
        private static partial Regex BlockedHost3();
        [GeneratedRegex(@"^(.*)\-sb\-api\.accounts\.nintendo\.com$", RegexOpts)]
        private static partial Regex BlockedHost4();
        [GeneratedRegex(@"^(.*)\-sb\.accounts\.nintendo\.com$", RegexOpts)]
        private static partial Regex BlockedHost5();
        [GeneratedRegex(@"^accounts\.nintendo\.com$", RegexOpts)]
        private static partial Regex BlockedHost6();

        private static readonly Regex[] _blockedHosts =
        [
            BlockedHost1(),
            BlockedHost2(),
            BlockedHost3(),
            BlockedHost4(),
            BlockedHost5(),
            BlockedHost6()
        ];

        public static bool IsHostBlocked(string host)
        {
            foreach (Regex regex in _blockedHosts)
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
