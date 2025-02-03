using DiscordRPC;
using Gommon;
using MsgPack;
using Ryujinx.Ava.Utilities;
using Ryujinx.Ava.Utilities.AppLibrary;
using Ryujinx.Ava.Utilities.Configuration;
using Ryujinx.Common;
using Ryujinx.Common.Helper;
using Ryujinx.Common.Logging;
using Ryujinx.HLE;
using Ryujinx.HLE.Loaders.Processes;
using Ryujinx.Horizon;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
                ? $"{VersionString} {ReleaseInformation.ReleaseChannelOwner}/{ReleaseInformation.ReleaseChannelSourceRepo}@{ReleaseInformation.BuildGitHash}"
                : "dev build";

        private const string ApplicationId = "1293250299716173864";

        private const int ApplicationByteLimit = 128;
        private const string Ellipsis = "…";

        private static DiscordRpcClient _discordClient;
        private static RichPresence _discordPresenceMain;
        private static RichPresence _discordPresencePlaying;

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
            HorizonStatic.PlayReportPrinted += HandlePlayReport;
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
        }

        private static void UpdatePlayingState()
        {
            _discordClient?.SetPresence(_discordPresencePlaying);
        }

        private static void SwitchToMainState()
        {
            _discordClient?.SetPresence(_discordPresenceMain);
            _discordPresencePlaying = null;
        }
        
        private static readonly PlayReportAnalyzer _playReportAnalyzer = new PlayReportAnalyzer()
            .AddSpec( // Breath of the Wild
                "01007ef00011e000",
                gameSpec =>
                    gameSpec.AddValueFormatter("IsHardMode", val => val is 1 ? "Playing Master Mode" : "Playing Normal Mode")
            )
            .AddSpec( // Super Mario Odyssey
                "0100000000010000",
                gameSpec =>
                    gameSpec.AddValueFormatter("is_kids_mode", val => val is 1 ? "Playing in Assist Mode" : "Playing in Regular Mode")
            )
            .AddSpec( // Super Mario Odyssey (China)
                "010075000ECBE000",
                gameSpec =>
                    gameSpec.AddValueFormatter("is_kids_mode", val => val is 1 ? "Playing in 帮助模式" : "Playing in 普通模式")
            )
            .AddSpec( // Super Mario 3D World + Bowser's Fury
                "010028600EBDA000",
                gameSpec =>
                    gameSpec.AddValueFormatter("mode", val => val is 0 ? "Playing Super Mario 3D World" : "Playing Bowser's Fury")
            );

        private static void HandlePlayReport(MessagePackObject playReport)
        {
            if (!TitleIDs.CurrentApplication.Value.HasValue) return;
            if (_discordPresencePlaying is null) return;

            Optional<string> details = _playReportAnalyzer.Run(TitleIDs.CurrentApplication.Value, playReport);

            if (!details.HasValue) return;
            
            _discordPresencePlaying.Details = details;
            UpdatePlayingState();
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
