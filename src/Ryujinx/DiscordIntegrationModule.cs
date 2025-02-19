using DiscordRPC;
using Gommon;
using Ryujinx.Ava.Utilities;
using Ryujinx.Ava.Utilities.AppLibrary;
using Ryujinx.Ava.Utilities.Configuration;
using Ryujinx.Ava.Utilities.PlayReport;
using Ryujinx.Common;
using Ryujinx.Common.Logging;
using Ryujinx.HLE;
using Ryujinx.HLE.Loaders.Processes;
using Ryujinx.Horizon;
using Ryujinx.Horizon.Prepo.Types;
using System.Text;

namespace Ryujinx.Ava
{
    public static class DiscordIntegrationModule
    {
        public static Timestamps EmulatorStartedAt { get; set; }
        public static Timestamps GuestAppStartedAt { get; set; }

        private static string VersionString
            => (ReleaseInformation.IsCanaryBuild ? "Canary " : string.Empty) + $"v{ReleaseInformation.Version}";

        private static readonly string _description =
            ReleaseInformation.IsValid
                ? $"{VersionString} {ReleaseInformation.ReleaseChannelOwner}/{ReleaseInformation.ReleaseChannelSourceRepo}"
                : "dev build";

        private const string ApplicationId = "1293250299716173864";

        private const int ApplicationByteLimit = 128;
        private const string Ellipsis = "…";

        private static DiscordRpcClient _discordClient;
        private static RichPresence _discordPresenceMain;
        private static RichPresence _discordPresencePlaying;
        private static ApplicationMetadata _currentApp;

        public static bool HasAssetImage(string titleId) => TitleIDs.DiscordGameAssetKeys.ContainsIgnoreCase(titleId);
        public static bool HasAnalyzer(string titleId) => PlayReports.Analyzer.TitleIds.ContainsIgnoreCase(titleId);

        public static void Initialize()
        {
            _discordPresenceMain = new RichPresence
            {
                Assets = new Assets
                {
                    LargeImageKey = "ryujinx", LargeImageText = TruncateToByteLength(_description)
                },
                Details = "Main Menu",
                State = "Idling",
                Timestamps = EmulatorStartedAt
            };

            ConfigurationState.Instance.EnableDiscordIntegration.Event += Update;
            TitleIDs.CurrentApplication.Event += (_, e) => Use(e.NewValue);
            HorizonStatic.PlayReport += HandlePlayReport;
            PlayReports.Initialize();
        }

        private static void Update(object sender, ReactiveEventArgs<bool> evnt)
        {
            if (evnt.OldValue != evnt.NewValue)
            {
                // If the integration was active, disable it and unload everything
                if (evnt.OldValue)
                {
                    _discordClient?.Dispose();

                    _discordClient = null;
                }

                // If we need to activate it and the client isn't active, initialize it
                if (evnt.NewValue && _discordClient == null)
                {
                    _discordClient = new DiscordRpcClient(ApplicationId);

                    _discordClient.Initialize();

                    Use(TitleIDs.CurrentApplication);
                }
            }
        }

        public static void Use(Optional<string> titleId)
        {
            if (titleId.TryGet(out string tid))
                SwitchToPlayingState(
                    ApplicationLibrary.LoadAndSaveMetaData(tid),
                    Switch.Shared.Processes.ActiveApplication
                );
            else
                SwitchToMainState();
        }

        private static RichPresence CreatePlayingState(ApplicationMetadata appMeta, ProcessResult procRes) =>
            new()
            {
                Assets = new Assets
                {
                    LargeImageKey = TitleIDs.GetDiscordGameAsset(procRes.ProgramIdText),
                    LargeImageText = TruncateToByteLength($"{appMeta.Title} (v{procRes.DisplayVersion})"),
                    SmallImageKey = "ryujinx",
                    SmallImageText = TruncateToByteLength(_description)
                },
                Details = TruncateToByteLength($"Playing {appMeta.Title}"),
                State = appMeta.LastPlayed.HasValue && appMeta.TimePlayed.TotalSeconds > 5
                    ? $"Total play time: {ValueFormatUtils.FormatTimeSpan(appMeta.TimePlayed)}"
                    : "Never played",
                Timestamps = GuestAppStartedAt ??= Timestamps.Now
            };

        private static void SwitchToPlayingState(ApplicationMetadata appMeta, ProcessResult procRes)
        {
            _discordClient?.SetPresence(_discordPresencePlaying ??= CreatePlayingState(appMeta, procRes));
            _currentApp = appMeta;
        }

        private static void SwitchToMainState()
        {
            _discordClient?.SetPresence(_discordPresenceMain);
            _discordPresencePlaying = null;
            _currentApp = null;
        }

        private static void HandlePlayReport(PlayReport playReport)
        {
            if (_discordClient is null) return;
            if (!TitleIDs.CurrentApplication.Value.HasValue) return;
            if (_discordPresencePlaying is null) return;

            FormattedValue formattedValue =
                PlayReports.Analyzer.Format(TitleIDs.CurrentApplication.Value, _currentApp, playReport);

            if (!formattedValue.Handled) return;

            _discordPresencePlaying.Details = TruncateToByteLength(
                formattedValue.Reset
                    ? $"Playing {_currentApp.Title}"
                    : formattedValue.FormattedString
            );

            if (_discordClient.CurrentPresence.Details.Equals(_discordPresencePlaying.Details))
                return; //don't trigger an update if the set presence Details are identical to current

            _discordClient.SetPresence(_discordPresencePlaying);
            Logger.Info?.Print(LogClass.UI, "Updated Discord RPC based on a supported play report.");
        }

        private static string TruncateToByteLength(string input)
        {
            if (Encoding.UTF8.GetByteCount(input) <= ApplicationByteLimit)
            {
                return input;
            }

            // Find the length to trim the string to guarantee we have space for the trailing ellipsis.
            int trimLimit = ApplicationByteLimit - Encoding.UTF8.GetByteCount(Ellipsis);

            // Make sure the string is long enough to perform the basic trim.
            // Amount of bytes != Length of the string
            if (input.Length > trimLimit)
            {
                // Basic trim to best case scenario of 1 byte characters.
                input = input[..trimLimit];
            }

            while (Encoding.UTF8.GetByteCount(input) > trimLimit)
            {
                // Remove one character from the end of the string at a time.
                input = input[..^1];
            }

            return input.TrimEnd() + Ellipsis;
        }

        public static void Exit()
        {
            _discordClient?.Dispose();
        }
    }
}
