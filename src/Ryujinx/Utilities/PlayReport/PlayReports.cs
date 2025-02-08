using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Ava.Utilities.PlayReport
{
    public static class PlayReports
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
            ).AddSpec(
                "01006a800016e000",
                spec => spec
                    .AddSparseMultiValueFormatter(
                        new []{
                            // Metadata to figure out what PlayReport we have.
                            "match_mode", "match_submode", "anniversary", "fighter", "reason", "challenge_count", "adv_slot",
                            // List of Fighters
                            "player_1_fighter", "player_2_fighter", "player_3_fighter", "player_4_fighter", "player_5_fighter", "player_6_fighter", "player_7_fighter", "player_8_fighter",
                            // List of rankings/placements
                            "player_1_rank", "player_2_rank", "player_3_rank", "player_4_rank", "player_5_rank", "player_6_rank", "player_7_rank", "player_8_rank"
                        },
                        SuperSmashBrosUltimate_Mode
                    )
            );

        private static FormattedValue BreathOfTheWild_MasterMode(SingleValue value)
            => value.Matched.BoxedValue is 1 ? "Playing Master Mode" : FormattedValue.ForceReset;

        private static FormattedValue TearsOfTheKingdom_CurrentField(SingleValue value) =>
            value.Matched.DoubleValue switch
            {
                > 800d => "Exploring the Sky Islands",
                < -201d => "Exploring the Depths",
                _ => "Roaming Hyrule"
            };

        private static FormattedValue SuperMarioOdyssey_AssistMode(SingleValue value)
            => value.Matched.BoxedValue is 1 ? "Playing in Assist Mode" : "Playing in Regular Mode";

        private static FormattedValue SuperMarioOdysseyChina_AssistMode(SingleValue value)
            => value.Matched.BoxedValue is 1 ? "Playing in 帮助模式" : "Playing in 普通模式";

        private static FormattedValue SuperMario3DWorldOrBowsersFury(SingleValue value)
            => value.Matched.BoxedValue is 0 ? "Playing Super Mario 3D World" : "Playing Bowser's Fury";
        
        private static FormattedValue MarioKart8Deluxe_Mode(SingleValue value) 
            => value.Matched.StringValue switch
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
                _ => FormattedValue.ForceReset
            };

        private static FormattedValue PokemonSVUnionCircle(SingleValue value)
            => value.Matched.BoxedValue is 0 ? "Playing Alone" : "Playing in a group";

        private static FormattedValue PokemonSVArea(SingleValue value) 
            => value.Matched.StringValue switch
            {
                // Base Game Locations
                "a_w01" => "South Area One",
                "a_w02" => "Mesagoza",
                "a_w03" => "The Pokemon League",
                "a_w04" => "South Area Two",
                "a_w05" => "South Area Four",
                "a_w06" => "South Area Six",
                "a_w07" => "South Area Five",
                "a_w08" => "South Area Three",
                "a_w09" => "West Area One",
                "a_w10" => "Asado Desert",
                "a_w11" => "West Area Two",
                "a_w12" => "Medali",
                "a_w13" => "Tagtree Thicket",
                "a_w14" => "East Area Three",
                "a_w15" => "Artazon",
                "a_w16" => "East Area Two",
                "a_w18" => "Casseroya Lake",
                "a_w19" => "Glaseado Mountain",
                "a_w20" => "North Area Three",
                "a_w21" => "North Area One",
                "a_w22" => "North Area Two",
                "a_w23" => "The Great Crater of Paldea",
                "a_w24" => "South Paldean Sea",
                "a_w25" => "West Paldean Sea",
                "a_w26" => "East Paldean Sea",
                "a_w27" => "Nouth Paldean Sea",
                //TODO DLC Locations
                _ => FormattedValue.ForceReset
            };
        private static FormattedValue SuperSmashBrosUltimate_Mode(SparseMultiValue valuesWrapper)
            {
                var values = valuesWrapper.Matched;
                // Check if the PlayReport is for a challenger approach or an achievement.
                if (values.TryGetValue("fighter", out var fighter) && values.ContainsKey("reason"))
                {
                    return $"Challenger Approaches - {SuperSmashBrosUltimate_Character(fighter)}";
                }
                if (values.TryGetValue("fighter", out fighter) && values.ContainsKey("challenge_count"))
                {
                    return $"Fighter Unlocked - {SuperSmashBrosUltimate_Character(fighter)}";
                }
                if (values.TryGetValue("anniversary", out var anniversary)) {
                    return $"Achievement Unlocked - ID: {anniversary}";
                }
                if (values.ContainsKey("adv_slot")) {
                    return $"Playing Adventure Mode"; // Doing this as it can be a placeholder until we can grab the character.
                }
                // Check if we have a match_mode at this point, if not, go to default.
                if (!values.TryGetValue("match_mode", out var matchMode))
                {
                    return "Smashing";
                }
                return matchMode.BoxedValue switch
                {
                    0 when values.TryGetValue("player_1_fighter", out var player) && values.TryGetValue("player_2_fighter", out var challenger) 
                        => $"Last Smashed: {SuperSmashBrosUltimate_Character(challenger)}'s Fighter Challenge - {SuperSmashBrosUltimate_Character(player)}",
                    1 => $"Last Smashed: Normal Battle - {SuperSmashBrosUltimate_PlayerListing(values)}",
                    2 when values.TryGetValue("player_1_rank", out var team)
                        => team.BoxedValue is 0 ? "Last Smashed: Squad Strike - Red Team Wins"
                        : "Last Smashed: Squad Strike - Blue Team Wins",
                    3 => $"Last Smashed: Custom Smash - {SuperSmashBrosUltimate_PlayerListing(values)}",
                    4 => $"Last Smashed: Super Sudden Death - {SuperSmashBrosUltimate_PlayerListing(values)}",
                    5 => $"Last Smashed: Smashdown - {SuperSmashBrosUltimate_PlayerListing(values)}",
                    6 => $"Last Smashed: Tourney Battle - {SuperSmashBrosUltimate_PlayerListing(values)}",
                    7 when values.TryGetValue("player_1_fighter", out var player)
                        => $"Last Smashed: Spirit Board Battle as {SuperSmashBrosUltimate_Character(player)}",
                    8 when values.TryGetValue("player_1_fighter", out var player)
                        => $"Playing Adventure Mode as {SuperSmashBrosUltimate_Character(player)}",
                    10 when values.TryGetValue("match_submode", out var battle) && values.TryGetValue("player_1_fighter", out var player)
                        => $"Last Smashed: Classic Mode, Battle {(int)battle.BoxedValue + 1}/8 as {SuperSmashBrosUltimate_Character(player)}",
                    12 => $"Last Smashed: Century Smash - {SuperSmashBrosUltimate_PlayerListing(values)}",
                    13 => $"Last Smashed: All-Star Smash - {SuperSmashBrosUltimate_PlayerListing(values)}",
                    14 => $"Last Smashed: Cruel Smash - {SuperSmashBrosUltimate_PlayerListing(values)}",
                    15 when values.TryGetValue("player_1_fighter", out var player)
                        => $"Last Smashed: Home-Run Contest - {SuperSmashBrosUltimate_Character(player)}",
                    16 when values.TryGetValue("player_1_fighter", out var player1) && values.TryGetValue("player_2_fighter", out var player2) 
                        => $"Last Smashed: Home-Run Content (Co-op) - {SuperSmashBrosUltimate_Character(player1)} and {SuperSmashBrosUltimate_Character(player2)}",
                    17 => $"Last Smashed: Home-Run Contest (Versus) - {SuperSmashBrosUltimate_PlayerListing(values)}",
                    18 when values.TryGetValue("player_1_fighter", out var player1) && values.TryGetValue("player_2_fighter", out var player2) 
                        => $"Fresh out of Training mode - {SuperSmashBrosUltimate_Character(player1)} with {SuperSmashBrosUltimate_Character(player2)}",
                    58 => $"Last Smashed: LDN Battle - {SuperSmashBrosUltimate_PlayerListing(values)}",
                    63 when values.TryGetValue("player_1_fighter", out var player)
                        => $"Last Smashed: DLC Spirit Board Battle as {SuperSmashBrosUltimate_Character(player)}",
                    _ => "Smashing"
                };
            }
        private static string SuperSmashBrosUltimate_Character(Value value) =>
            BinaryPrimitives.ReverseEndianness(BitConverter.ToInt64(((MsgPack.MessagePackExtendedTypeObject)value.BoxedValue).GetBody(), 0)) switch
            {
                0x0 => "Mario",
                0x1 => "Donkey Kong",
                0x2 => "Link",
                0x3 => "Samus",
                0x4 => "Dark Samus",
                0x5 => "Yoshi",
                0x6 => "Kirby",
                0x7 => "Fox",
                0x8 => "Pikachu",
                0x9 => "Luigi",
                0xA => "Ness",
                0xB => "Captain Falcon",
                0xC => "Jigglypuff",
                0xD => "Peach",
                0xE => "Daisy",
                0xF => "Bowser",
                0x10 => "Ice Climbers",
                0x11 => "Sheik",
                0x12 => "Zelda",
                0x13 => "Dr. Mario",
                0x14 => "Pichu",
                0x15 => "Falco",
                0x16 => "Marth",
                0x17 => "Lucina",
                0x18 => "Young Link",
                0x19 => "Ganondorf",
                0x1A => "Mewtwo",
                0x1B => "Roy",
                0x1C => "Chrom",
                0x1D => "Mr Game & Watch",
                0x1E => "Meta Knight",
                0x1F => "Pit",
                0x20 => "Dark Pit",
                0x21 => "Zero Suit Samus",
                0x22 => "Wario",
                0x23 => "Snake",
                0x24 => "Ike",
                0x25 => "Pokémon Trainer",
                0x26 => "Diddy Kong",
                0x27 => "Lucas",
                0x28 => "Sonic",
                0x29 => "King Dedede",
                0x2A => "Olimar",
                0x2B => "Lucario",
                0x2C => "R.O.B.",
                0x2D => "Toon Link",
                0x2E => "Wolf",
                0x2F => "Villager",
                0x30 => "Mega Man",
                0x31 => "Wii Fit Trainer",
                0x32 => "Rosalina & Luma",
                0x33 => "Little Mac",
                0x34 => "Greninja",
                0x35 => "Palutena",
                0x36 => "Pac-Man",
                0x37 => "Robin",
                0x38 => "Shulk",
                0x39 => "Bowser Jr.",
                0x3A => "Duck Hunt",
                0x3B => "Ryu",
                0x3C => "Ken",
                0x3D => "Cloud",
                0x3E => "Corrin",
                0x3F => "Bayonetta",
                0x40 => "Richter",
                0x41 => "Inkling",
                0x42 => "Ridley",
                0x43 => "King K. Rool",
                0x44 => "Simon",
                0x45 => "Isabelle",
                0x46 => "Incineroar",
                0x47 => "Mii Brawler",
                0x48 => "Mii Swordfighter",
                0x49 => "Mii Gunner",
                0x4A => "Piranha Plant",
                0x4B => "Joker",
                0x4C => "Hero",
                0x4D => "Banjo",
                0x4E => "Terry",
                0x4F => "Byleth",
                0x50 => "Min Min",
                0x51 => "Steve",
                0x52 => "Sephiroth",
                0x53 => "Pyra/Mythra",
                0x54 => "Kazuya",
                0x55 => "Sora",
                0xFE => "Random",
                0xFF => "Scripted Entity",
                _ => "Unknown"
            };
            private static string SuperSmashBrosUltimate_PlayerListing(Dictionary<string, Value> values)
            {
                var players = new List<(string Character, int PlayerNumber, int? Rank)>();
    
                foreach (var player in values)
                {
                    if (player.Key.StartsWith("player_") && player.Key.EndsWith("_fighter") && player.Value.BoxedValue is not null)
                    {
                        int playerNumber = int.Parse(player.Key.Split('_')[1]);
                        string character = SuperSmashBrosUltimate_Character(player.Value);
                        int? rank = values.TryGetValue($"player_{playerNumber}_rank", out var rankValue) ? (int)rankValue.BoxedValue : null;

                        players.Add((character, playerNumber, rank));
                    }
                }

                players = players.OrderBy(p => p.Rank ?? int.MaxValue).ToList();
                string RankMedal(int? rank) => rank switch
                {
                    0 => "🥇",
                    1 => "🥈",
                    2 => "🥉",
                    _ => ""
                };
            
                return players.Count > 4 
                    ? $"{players.Count} Players - " + string.Join(", ", players.Take(3).Select(p => $"{p.Character}({p.PlayerNumber}){RankMedal(p.Rank)}"))
                    : string.Join(", ", players.Select(p => $"{p.Character}({p.PlayerNumber}){RankMedal(p.Rank)}"));
            }
    }
}
