namespace Ryujinx.Ava.Utilities.PlayReport
{
    public static partial class PlayReports
    {
        public static Analyzer Analyzer { get; } = new Analyzer()
            .AddSpec(
                "01007ef00011e000",
                spec => spec
                    .AddValueFormatter("IsHardMode", BreathOfTheWild_MasterMode)
                    // reset to normal status when switching between normal & master mode in title screen
                    .AddValueFormatter("AoCVer", FormattedValue.SingleAlwaysResets)
            )
            .AddSpec(
                "0100f2c0115b6000",
                spec => spec
                    .AddValueFormatter("PlayerPosY", TearsOfTheKingdom_CurrentField))
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
            )
            .AddSpec(
                ["0100a3d008c5c000", "01008f6008c5e000"],
                spec => spec
                    .AddValueFormatter("area_no", PokemonSVArea)
                    .AddValueFormatter("team_circle", PokemonSVUnionCircle)
            )
            .AddSpec(
                "01006a800016e000",
                spec => spec
                    .AddSparseMultiValueFormatter(
                        [
                            // Metadata to figure out what PlayReport we have.
                            "match_mode", "match_submode", "anniversary", "fighter", "reason", "challenge_count",
                            "adv_slot",
                            // List of Fighters
                            "player_1_fighter", "player_2_fighter", "player_3_fighter", "player_4_fighter",
                            "player_5_fighter", "player_6_fighter", "player_7_fighter", "player_8_fighter",
                            // List of rankings/placements
                            "player_1_rank", "player_2_rank", "player_3_rank", "player_4_rank", "player_5_rank",
                            "player_6_rank", "player_7_rank", "player_8_rank"
                        ],
                        SuperSmashBrosUltimate_Mode
                    )
            )
            .AddSpec(
                [
                    "0100c9a00ece6000", "01008d300c50c000", "0100d870045b6000", 
                    "010012f017576000", "0100c62011050000", "0100b3c014bda000"],
                spec => spec.AddValueFormatter("launch_title_id", NsoEmulator_LaunchedGame)
            );

        private static string Playing(string game) => $"Playing {game}";
    }
}
