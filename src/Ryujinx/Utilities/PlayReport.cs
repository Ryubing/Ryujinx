using Gommon;
using MsgPack;
using Ryujinx.Ava.Utilities.AppLibrary;
using Ryujinx.Common.Helper;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Ava.Utilities
{
    public static class PlayReport
    {
        public static PlayReportAnalyzer Analyzer { get; } = new PlayReportAnalyzer()
            .AddSpec(
                "01007ef00011e000",
                spec => spec.AddValueFormatter("IsHardMode", BreathOfTheWild_MasterMode)
            )
            .AddSpec( // Super Mario Odyssey
                "0100000000010000",
                spec =>
                    spec.AddValueFormatter("is_kids_mode", SuperMarioOdyssey_AssistMode)
            )
            .AddSpec( // Super Mario Odyssey (China)
                "010075000ECBE000",
                spec =>
                    spec.AddValueFormatter("is_kids_mode", SuperMarioOdysseyChina_AssistMode)
            )
            .AddSpec( // Super Mario 3D World + Bowser's Fury
                "010028600EBDA000",
                spec => spec.AddValueFormatter("mode", SuperMario3DWorldOrBowsersFury)
            )
            .AddSpec( // Mario Kart 8 Deluxe, Mario Kart 8 Deluxe (China)
                ["0100152000022000", "010075100E8EC000"],
                spec => spec.AddValueFormatter("To", MarioKart8Deluxe_Mode)
            );

        private static PlayReportFormattedValue BreathOfTheWild_MasterMode(ref PlayReportValue value)
            => value.BoxedValue is 1 ? "Playing Master Mode" : PlayReportFormattedValue.ForceReset;

        private static PlayReportFormattedValue SuperMarioOdyssey_AssistMode(ref PlayReportValue value)
            => value.BoxedValue is 1 ? "Playing in Assist Mode" : "Playing in Regular Mode";

        private static PlayReportFormattedValue SuperMarioOdysseyChina_AssistMode(ref PlayReportValue value)
            => value.BoxedValue is 1 ? "Playing in 帮助模式" : "Playing in 普通模式";

        private static PlayReportFormattedValue SuperMario3DWorldOrBowsersFury(ref PlayReportValue value)
            => value.BoxedValue is 0 ? "Playing Super Mario 3D World" : "Playing Bowser's Fury";
        
        private static PlayReportFormattedValue MarioKart8Deluxe_Mode(ref PlayReportValue value) 
            => value.BoxedValue switch
            {
                // Single Player
                "Single" => "Single Player",
                // Multiplayer
                "Multi-2players" => "Multiplayer 2 Players",
                "Multi-3players" => "Multiplayer 3 Players",
                "Multi-4players" => "Multiplayer 4 Players",
                // Wireless/LAN Play
                "Local-Single" => "Wireless/LAN Play",
                "Local-2players" => "Wireless/LAN Play 2 Players",
                // CC Classes
                "50cc" => "50cc",
                "100cc" => "100cc",
                "150cc" => "150cc",
                "Mirror" => "Mirror (150cc)",
                "200cc" => "200cc",
                // Modes
                "GrandPrix" => "Grand Prix",
                "TimeAttack" => "Time Trials",
                "VS" => "VS Races",
                "Battle" => "Battle Mode",
                "RaceStart" => "Selecting a Course",
                "Race" => "Racing",
                _ => PlayReportFormattedValue.ForceReset
            };
    }

    #region Analyzer implementation
    
    public class PlayReportAnalyzer
    {
        private readonly List<PlayReportGameSpec> _specs = [];

        public PlayReportAnalyzer AddSpec(string titleId, Func<PlayReportGameSpec, PlayReportGameSpec> transform)
        {
            _specs.Add(transform(new PlayReportGameSpec { TitleIds = [titleId] }));
            return this;
        }
        
        public PlayReportAnalyzer AddSpec(string titleId, Action<PlayReportGameSpec> transform)
        {
            _specs.Add(new PlayReportGameSpec { TitleIds = [titleId] }.Apply(transform));
            return this;
        }
        
        public PlayReportAnalyzer AddSpec(IEnumerable<string> titleIds, Func<PlayReportGameSpec, PlayReportGameSpec> transform)
        {
            _specs.Add(transform(new PlayReportGameSpec { TitleIds = [..titleIds] }));
            return this;
        }
        
        public PlayReportAnalyzer AddSpec(IEnumerable<string> titleIds, Action<PlayReportGameSpec> transform)
        {
            _specs.Add(new PlayReportGameSpec { TitleIds = [..titleIds] }.Apply(transform));
            return this;
        }

        public PlayReportFormattedValue Run(string runningGameId, ApplicationMetadata appMeta, MessagePackObject playReport)
        {
            if (!playReport.IsDictionary) 
                return PlayReportFormattedValue.Unhandled;

            if (!_specs.TryGetFirst(s => runningGameId.EqualsAnyIgnoreCase(s.TitleIds), out PlayReportGameSpec spec))
                return PlayReportFormattedValue.Unhandled;

            foreach (PlayReportValueFormatterSpec formatSpec in spec.Analyses.OrderBy(x => x.Priority))
            {
                if (!playReport.AsDictionary().TryGetValue(formatSpec.ReportKey, out MessagePackObject valuePackObject))
                    continue;

                PlayReportValue value = new()
                {
                    Application = appMeta, 
                    BoxedValue = valuePackObject.ToObject()
                };

                return formatSpec.ValueFormatter(ref value);
            }
            
            return PlayReportFormattedValue.Unhandled;
        }
        
    }

    public class PlayReportGameSpec
    {
        public required string[] TitleIds { get; init; }
        public List<PlayReportValueFormatterSpec> Analyses { get; } = [];

        public PlayReportGameSpec AddValueFormatter(string reportKey, PlayReportValueFormatter valueFormatter)
        {
            Analyses.Add(new PlayReportValueFormatterSpec
            {
                Priority = Analyses.Count,
                ReportKey = reportKey, 
                ValueFormatter = valueFormatter
            });
            return this;
        }
        
        public PlayReportGameSpec AddValueFormatter(int priority, string reportKey, PlayReportValueFormatter valueFormatter)
        {
            Analyses.Add(new PlayReportValueFormatterSpec
            {
                Priority = priority,
                ReportKey = reportKey, 
                ValueFormatter = valueFormatter
            });
            return this;
        }
    }

    public struct PlayReportValue
    {
        public ApplicationMetadata Application { get; init; }
        public object BoxedValue { get; init; }
    }

    public struct PlayReportFormattedValue
    {
        public bool Handled { get; private init; }
        
        public bool Reset { get; private init; }
        
        public string FormattedString { get; private init; }

        public static implicit operator PlayReportFormattedValue(string formattedValue)
            => new() { Handled = true, FormattedString = formattedValue };

        public static PlayReportFormattedValue Unhandled => default;
        public static PlayReportFormattedValue ForceReset => new() { Handled = true, Reset = true };

        public static PlayReportValueFormatter AlwaysResets = AlwaysResetsImpl;
        
        private static PlayReportFormattedValue AlwaysResetsImpl(ref PlayReportValue _) => ForceReset;
    }

    public struct PlayReportValueFormatterSpec
    {
        public required int Priority { get; init; }
        public required string ReportKey { get; init; }
        public PlayReportValueFormatter ValueFormatter { get; init; }
    }

    public delegate PlayReportFormattedValue PlayReportValueFormatter(ref PlayReportValue value);

    #endregion
}
