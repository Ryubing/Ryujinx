using System.Text.RegularExpressions;

namespace Ryujinx.Common.Helper
{
    public static partial class Patterns
    {
        #region Accessors

        public static readonly Regex Numeric = NumericRegex();

        public static readonly Regex AmdGcn = AmdGcnRegex();
        public static readonly Regex NvidiaConsumerClass = NvidiaConsumerClassRegex();

        public static readonly Regex DomainLp1Ns = DomainLp1NsRegex();
        public static readonly Regex DomainLp1Lp1Npln = DomainLp1Lp1NplnRegex();
        public static readonly Regex DomainLp1Znc = DomainLp1ZncRegex();
        public static readonly Regex DomainSbApi = DomainSbApiRegex();
        public static readonly Regex DomainSbAccounts = DomainSbAccountsRegex();
        public static readonly Regex DomainAccounts = DomainAccountsRegex();

        public static readonly Regex Module = ModuleRegex();
        public static readonly Regex FsSdk = FsSdkRegex();
        public static readonly Regex SdkMw = SdkMwRegex();

        // ReSharper disable once InconsistentNaming
        public static readonly Regex CJK = CJKRegex();

        public static readonly Regex LdnPassphrase = LdnPassphraseRegex();

        public static readonly Regex CleanText = CleanTextRegex();

        #endregion

        #region Generated pattern stubs

        #region Numeric validation

        [GeneratedRegex("[0-9]|.")]
        internal static partial Regex NumericRegex();

        #endregion

        #region GPU names

        [GeneratedRegex(
            "Radeon (((HD|R(5|7|9|X)) )?((M?[2-6]\\d{2}(\\D|$))|([7-8]\\d{3}(\\D|$))|Fury|Nano))|(Pro Duo)")]
        internal static partial Regex AmdGcnRegex();

        [GeneratedRegex("NVIDIA GeForce (R|G)?TX? (\\d{3}\\d?)M?")]
        internal static partial Regex NvidiaConsumerClassRegex();

        #endregion

        #region DNS blocking

        public static readonly Regex[] BlockedHosts =
        [
            DomainLp1Ns,
            DomainLp1Lp1Npln,
            DomainLp1Znc,
            DomainSbApi,
            DomainSbAccounts,
            DomainAccounts
        ];

        const RegexOptions DnsRegexOpts =
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture;

        [GeneratedRegex(@"^(.*)\-lp1\.(n|s)\.n\.srv\.nintendo\.net$", DnsRegexOpts)]
        internal static partial Regex DomainLp1NsRegex();

        [GeneratedRegex(@"^(.*)\-lp1\.lp1\.t\.npln\.srv\.nintendo\.net$", DnsRegexOpts)]
        internal static partial Regex DomainLp1Lp1NplnRegex();

        [GeneratedRegex(@"^(.*)\-lp1\.(znc|p)\.srv\.nintendo\.net$", DnsRegexOpts)]
        internal static partial Regex DomainLp1ZncRegex();

        [GeneratedRegex(@"^(.*)\-sb\-api\.accounts\.nintendo\.com$", DnsRegexOpts)]
        internal static partial Regex DomainSbApiRegex();

        [GeneratedRegex(@"^(.*)\-sb\.accounts\.nintendo\.com$", DnsRegexOpts)]
        internal static partial Regex DomainSbAccountsRegex();

        [GeneratedRegex(@"^accounts\.nintendo\.com$", DnsRegexOpts)]
        internal static partial Regex DomainAccountsRegex();

        #endregion

        #region Executable information

        [GeneratedRegex(@"[a-z]:[\\/][ -~]{5,}\.nss", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        internal static partial Regex ModuleRegex();

        [GeneratedRegex(@"sdk_version: ([0-9.]*)")]
        internal static partial Regex FsSdkRegex();

        [GeneratedRegex(@"SDK MW[ -~]*")]
        internal static partial Regex SdkMwRegex();

        #endregion

        #region CJK

        [GeneratedRegex(
            "\\p{IsHangulJamo}|\\p{IsCJKRadicalsSupplement}|\\p{IsCJKSymbolsandPunctuation}|\\p{IsEnclosedCJKLettersandMonths}|\\p{IsCJKCompatibility}|\\p{IsCJKUnifiedIdeographsExtensionA}|\\p{IsCJKUnifiedIdeographs}|\\p{IsHangulSyllables}|\\p{IsCJKCompatibilityForms}")]
        private static partial Regex CJKRegex();

        #endregion

        [GeneratedRegex("Ryujinx-[0-9a-f]{8}")]
        private static partial Regex LdnPassphraseRegex();

        [GeneratedRegex(@"[^\u0000\u0009\u000A\u000D\u0020-\uFFFF]..")]
        private static partial Regex CleanTextRegex();

        #endregion
    }
}
