using System;

namespace Ryujinx.Ava.Systems.PlayReport
{
    public static partial class PlayReports
    {
        public static void Initialize()
        {
            // init lazy value
            _ = Analyzer;
        }

        public static Analyzer Analyzer => _analyzerLazy.Value;

        private static readonly Lazy<Analyzer> _analyzerLazy = new(() => new Analyzer()
            .AddSpec(
                "01007ef00011e000",
                spec => spec
                    .WithDescription("based on being in Master Mode.")
                    .AddValueFormatter("IsHardMode", BreathOfTheWild_MasterMode)
                    // reset to normal status when switching between normal & master mode in title screen
                    .AddValueFormatter("AoCVer", FormattedValue.SingleAlwaysResets)
            )
            .AddSpec(
                "0100f2c0115b6000",
                spec => spec
                    .WithDescription("based on where you are in Hyrule (Depths, Surface, Sky).")
                    .AddValueFormatter("PlayerPosY", TearsOfTheKingdom_CurrentField))
            .AddSpec(
                "01002da013484000",
                spec => spec
                    .WithDescription("based on how many Rupees you have.")
                    .AddValueFormatter("rupees", SkywardSwordHD_Rupees))
            .AddSpec(
                "0100000000010000",
                spec => spec
                    .WithDescription("based on if you're playing with Assist Mode.")
                    .AddValueFormatter("is_kids_mode", SuperMarioOdyssey_AssistMode)
            )
            .AddSpec(
                "010075000ecbe000",
                spec => spec
                    .WithDescription("based on if you're playing with Assist Mode.")
                    .AddValueFormatter("is_kids_mode", SuperMarioOdysseyChina_AssistMode)
            )
            .AddSpec(
                "010028600ebda000",
                spec => spec
                    .WithDescription("based on being in either Super Mario 3D World or Bowser's Fury.")
                    .AddValueFormatter("mode", SuperMario3DWorldOrBowsersFury)
            )
            .AddSpec( // Global & China IDs
                ["0100152000022000", "010075100e8ec000"],
                spec => spec
                    .WithDescription(
                        "based on what modes you're selecting in the menu & whether or not you're in a race.")
                    .AddValueFormatter("To", MarioKart8Deluxe_Mode)
            )
            .AddSpec(
                ["0100a3d008c5c000", "01008f6008c5e000"],
                spec => spec
                    .WithDescription("based on if you're playing alone or in a group and what area of Paldea you're exploring.")
                    .AddMultiValueFormatter(["team_circle", "area_no"], PokemonSV)
            )
            .AddSpec(
                "01006a800016e000",
                spec => spec
                    .WithDescription("based on what mode you're playing, who won, and what characters were present.")
                    .AddSparseMultiValueFormatter(
                        [
                            // Metadata to figure out what PlayReport we have.
                            "match_mode", "match_submode", "anniversary", "fighter", "reason", "challenge_count",
                            "adv_slot", "is_created",
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
                    "010012f017576000", "0100c62011050000", "0100b3c014bda000"
                ],
                spec => spec
                    .WithDescription(
                        "based on what game you first launch.\n\nNSO emulators do not print any Play Report information past the first game launch so it's all we got.")
                    .AddValueFormatter("launch_title_id", NsoEmulator_LaunchedGame)
            )
        );

        private static string Playing(string game) => $"Playing {game}";
    }
}
