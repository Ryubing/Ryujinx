using PlayReportFormattedValue = Ryujinx.Ava.Utilities.PlayReportAnalyzer.FormattedValue;

namespace Ryujinx.Ava.Utilities
{
    public static class PlayReport
    {
        public static PlayReportAnalyzer Analyzer { get; } = new PlayReportAnalyzer()
            .AddSpec(
                "01007ef00011e000",
                spec => spec
                    .AddValueFormatter("IsHardMode", BreathOfTheWild_MasterMode)
                    // reset to normal status when switching between normal & master mode in title screen
                    .AddValueFormatter("AoCVer", PlayReportFormattedValue.AlwaysResets)
            )
            .AddSpec(
                "0100f2c0115b6000",
                spec => spec.AddValueFormatter("PlayerPosY", TearsOfTheKingdom_CurrentField))
            .AddSpec(
                "0100000000010000",
                spec =>
                    spec.AddValueFormatter("is_kids_mode", SuperMarioOdyssey_AssistMode)
            )
            .AddSpec(
                "010075000ecbe000",
                spec =>
                    spec.AddValueFormatter("is_kids_mode", SuperMarioOdysseyChina_AssistMode)
            )
            .AddSpec(
                "010028600ebda000",
                spec => spec.AddValueFormatter("mode", SuperMario3DWorldOrBowsersFury)
            )
            .AddSpec( // Global & China IDs
                ["0100152000022000", "010075100e8ec000"],
                spec => spec.AddValueFormatter("To", MarioKart8Deluxe_Mode)
            );

        private static PlayReportFormattedValue BreathOfTheWild_MasterMode(PlayReportValue value)
            => value.BoxedValue is 1 ? "Playing Master Mode" : PlayReportFormattedValue.ForceReset;

        private static PlayReportFormattedValue TearsOfTheKingdom_CurrentField(PlayReportValue value) =>
            value.DoubleValue switch
            {
                > 800d => "Exploring the Sky Islands",
                < -201d => "Exploring the Depths",
                _ => "Roaming Hyrule"
            };

        private static PlayReportFormattedValue SuperMarioOdyssey_AssistMode(PlayReportValue value)
            => value.BoxedValue is 1 ? "Playing in Assist Mode" : "Playing in Regular Mode";

        private static PlayReportFormattedValue SuperMarioOdysseyChina_AssistMode(PlayReportValue value)
            => value.BoxedValue is 1 ? "Playing in 帮助模式" : "Playing in 普通模式";

        private static PlayReportFormattedValue SuperMario3DWorldOrBowsersFury(PlayReportValue value)
            => value.BoxedValue is 0 ? "Playing Super Mario 3D World" : "Playing Bowser's Fury";
        
        private static PlayReportFormattedValue MarioKart8Deluxe_Mode(PlayReportValue value) 
            => value.StringValue switch
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
}
