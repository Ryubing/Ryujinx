using System;
using System.Reflection;

namespace Ryujinx.Common
{
    // DO NOT EDIT, filled by CI
    public static class ReleaseInformation
    {
        private const string CanaryChannel = "canary";
        private const string ReleaseChannel = "release";

        private const string BuildVersion = "%%RYUJINX_BUILD_VERSION%%";
        public const string BuildGitHash = "%%RYUJINX_BUILD_GIT_HASH%%";
        private const string ReleaseChannelName = "%%RYUJINX_TARGET_RELEASE_CHANNEL_NAME%%";
        private const string ConfigFileName = "%%RYUJINX_CONFIG_FILE_NAME%%";

        public const string ReleaseChannelOwner = "%%RYUJINX_TARGET_RELEASE_CHANNEL_OWNER%%";
        public const string ReleaseChannelSourceRepo = "%%RYUJINX_TARGET_RELEASE_CHANNEL_SOURCE_REPO%%";
        public const string ReleaseChannelRepo = "%%RYUJINX_TARGET_RELEASE_CHANNEL_REPO%%";

        public static string ConfigName => !ConfigFileName.StartsWith("%%") ? ConfigFileName : "Config.json";

        public static bool IsValid =>
            !BuildGitHash.StartsWith("%%") &&
            !ReleaseChannelName.StartsWith("%%") &&
            !ReleaseChannelOwner.StartsWith("%%") &&
            !ReleaseChannelSourceRepo.StartsWith("%%") &&
            !ReleaseChannelRepo.StartsWith("%%") &&
            !ConfigFileName.StartsWith("%%");

        public static bool IsCanaryBuild => IsValid && ReleaseChannelName.Equals(CanaryChannel);
        
        public static bool IsReleaseBuild => IsValid && ReleaseChannelName.Equals(ReleaseChannel);

        public static string Version => IsValid ? BuildVersion : Assembly.GetEntryAssembly()!.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        public static string GetChangelogUrl(Version currentVersion, Version newVersion) =>
            IsCanaryBuild 
                ? $"https://github.com/{ReleaseChannelOwner}/{ReleaseChannelSourceRepo}/compare/Canary-{currentVersion}...Canary-{newVersion}" 
                : $"https://github.com/{ReleaseChannelOwner}/{ReleaseChannelRepo}/releases/{newVersion}";
        
        public static string GetChangelogForVersion(Version version) =>
            $"https://github.com/{ReleaseChannelOwner}/{ReleaseChannelRepo}/releases/tag/{version}";
    }
}
