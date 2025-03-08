using Gommon;
using Humanizer;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Ava.Systems.PlayReport
{
    public partial class PlayReports
    {
        private static FormattedValue BreathOfTheWild_MasterMode(SingleValue value)
            => value.Matched.BoxedValue is 1 ? "Playing Master Mode" : FormattedValue.ForceReset;

        private static FormattedValue TearsOfTheKingdom_CurrentField(SingleValue value) =>
            value.Matched.DoubleValue switch
            {
                > 800d => "Exploring the Sky Islands",
                < -201d => "Exploring the Depths",
                _ => "Roaming Hyrule"
            };
        
        private static FormattedValue SkywardSwordHD_Rupees(SingleValue value)
            => "rupee".ToQuantity(value.Matched.IntValue);

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
        
        private static FormattedValue PokemonSV(MultiValue values)
        {
            
            string playStatus = values.Matched[0].BoxedValue is 0 ? "Playing Alone" : "Playing in a group";
            
            FormattedValue locations = values.Matched[1].ToString()  switch
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
                "a_w27" => "North Paldean Sea",
                //TODO DLC Locations
                _ => FormattedValue.ForceReset
            };
            
            return$"{playStatus} in {locations}";
        }

        private static FormattedValue SuperSmashBrosUltimate_Mode(SparseMultiValue values)
        {
            // Check if the PlayReport is for a challenger approach or an achievement.
            if (values.Matched.TryGetValue("fighter", out Value fighter) && values.Matched.ContainsKey("reason"))
            {
                return $"Challenger Approaches - {SuperSmashBrosUltimate_Character(fighter)}";
            }

            if (values.Matched.TryGetValue("fighter", out fighter) && values.Matched.ContainsKey("challenge_count"))
            {
                return $"Fighter Unlocked - {SuperSmashBrosUltimate_Character(fighter)}";
            }

            if (values.Matched.TryGetValue("anniversary", out Value anniversary))
            {
                return $"Achievement Unlocked - ID: {anniversary}";
            }

            if (values.Matched.ContainsKey("is_created"))
            {
                return "Edited a Custom Stage!"; 
            }

            if (values.Matched.ContainsKey("adv_slot"))
            {
                return
                    "Playing Adventure Mode"; // Doing this as it can be a placeholder until we can grab the character.
            }

            // Check if we have a match_mode at this point, if not, go to default.
            if (!values.Matched.TryGetValue("match_mode", out Value matchMode))
            {
                return "Smashing";
            }

            return matchMode.BoxedValue switch
            {
                0 when values.Matched.TryGetValue("player_1_fighter", out Value player) &&
                       values.Matched.TryGetValue("player_2_fighter", out Value challenger)
                    => $"Last Smashed: {SuperSmashBrosUltimate_Character(challenger)}'s Fighter Challenge - {SuperSmashBrosUltimate_Character(player)}",
                1 => $"Last Smashed: Normal Battle - {SuperSmashBrosUltimate_PlayerListing(values)}",
                2 when values.Matched.TryGetValue("player_1_rank", out Value team)
                    => team.BoxedValue is 0
                        ? "Last Smashed: Squad Strike - Red Team Wins"
                        : "Last Smashed: Squad Strike - Blue Team Wins",
                3 => $"Last Smashed: Custom Smash - {SuperSmashBrosUltimate_PlayerListing(values)}",
                4 => $"Last Smashed: Super Sudden Death - {SuperSmashBrosUltimate_PlayerListing(values)}",
                5 => $"Last Smashed: Smashdown - {SuperSmashBrosUltimate_PlayerListing(values)}",
                6 => $"Last Smashed: Tourney Battle - {SuperSmashBrosUltimate_PlayerListing(values)}",
                7 when values.Matched.TryGetValue("player_1_fighter", out Value player)
                    => $"Last Smashed: Spirit Board Battle as {SuperSmashBrosUltimate_Character(player)}",
                8 when values.Matched.TryGetValue("player_1_fighter", out Value player)
                    => $"Playing Adventure Mode as {SuperSmashBrosUltimate_Character(player)}",
                10 when values.Matched.TryGetValue("match_submode", out Value battle) &&
                        values.Matched.TryGetValue("player_1_fighter", out Value player)
                    => $"Last Smashed: Classic Mode, Battle {(int)battle.BoxedValue + 1}/8 as {SuperSmashBrosUltimate_Character(player)}",
                12 => $"Last Smashed: Century Smash - {SuperSmashBrosUltimate_PlayerListing(values)}",
                13 => $"Last Smashed: All-Star Smash - {SuperSmashBrosUltimate_PlayerListing(values)}",
                14 => $"Last Smashed: Cruel Smash - {SuperSmashBrosUltimate_PlayerListing(values)}",
                15 when values.Matched.TryGetValue("player_1_fighter", out Value player)
                    => $"Last Smashed: Home-Run Contest - {SuperSmashBrosUltimate_Character(player)}",
                16 when values.Matched.TryGetValue("player_1_fighter", out Value player1) &&
                        values.Matched.TryGetValue("player_2_fighter", out Value player2)
                    => $"Last Smashed: Home-Run Content (Co-op) - {SuperSmashBrosUltimate_Character(player1)} and {SuperSmashBrosUltimate_Character(player2)}",
                17 => $"Last Smashed: Home-Run Contest (Versus) - {SuperSmashBrosUltimate_PlayerListing(values)}",
                18 when values.Matched.TryGetValue("player_1_fighter", out Value player1) &&
                        values.Matched.TryGetValue("player_2_fighter", out Value player2)
                    => $"Fresh out of Training mode - {SuperSmashBrosUltimate_Character(player1)} with {SuperSmashBrosUltimate_Character(player2)}",
                58 => $"Last Smashed: LDN Battle - {SuperSmashBrosUltimate_PlayerListing(values)}",
                63 when values.Matched.TryGetValue("player_1_fighter", out Value player)
                    => $"Last Smashed: DLC Spirit Board Battle as {SuperSmashBrosUltimate_Character(player)}",
                _ => "Smashing"
            };
        }

        private static string SuperSmashBrosUltimate_Character(Value value) =>
            BinaryPrimitives.ReverseEndianness(
                    BitConverter.ToInt64(((MsgPack.MessagePackExtendedTypeObject)value.BoxedValue).GetBody(), 0)) switch
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

        private static string SuperSmashBrosUltimate_PlayerListing(SparseMultiValue values)
        {
            List<(string Character, int PlayerNumber, int? Rank)> players = [];

            foreach (KeyValuePair<string, Value> player in values.Matched)
            {
                if (player.Key.StartsWith("player_") && player.Key.EndsWith("_fighter") &&
                    player.Value.BoxedValue is not null)
                {
                    if (!int.TryParse(player.Key.Split('_')[1], out int playerNumber))
                        continue;

                    string character = SuperSmashBrosUltimate_Character(player.Value);
                    int? rank = values.Matched.TryGetValue($"player_{playerNumber}_rank", out Value rankValue)
                        ? rankValue.IntValue
                        : null;

                    players.Add((character, playerNumber, rank));
                }
            }

            players = players.OrderBy(p => p.Rank ?? int.MaxValue).ToList();

            return players.Count > 4
                ? $"{players.Count} Players - {
                    players.Take(3)
                        .Select(p => $"{p.Character}({p.PlayerNumber}){RankMedal(p.Rank)}")
                        .JoinToString(", ")
                }"
                : players
                    .Select(p => $"{p.Character}({p.PlayerNumber}){RankMedal(p.Rank)}")
                    .JoinToString(", ");

            string RankMedal(int? rank) => rank switch
            {
                0 => "🥇",
                1 => "🥈",
                2 => "🥉",
                _ => ""
            };
        }

        private static FormattedValue NsoEmulator_LaunchedGame(SingleValue value) => value.Matched.StringValue switch
        {
            #region SEGA Genesis

            "m_0054_e" => Playing("Alien Soldier"),
            "m_3978_e" => Playing("Alien Storm"),
            "m_5234_e" => Playing("ALISIA DRAGOON"),
            "m_5003_e" => Playing("Streets of Rage 2"),
            "m_4843_e" => Playing("Kid Chameleon"),
            "m_2874_e" => Playing("Columns"),
            "m_3167_e" => Playing("Comix Zone"),
            "m_5007_e" => Playing("Contra: Hard Corps"),
            "m_0865_e" => Playing("Ghouls 'n Ghosts"),
            "m_0935_e" => Playing("Dynamite Headdy"),
            "m_8314_e" => Playing("Earthworm Jim"),
            "m_5012_e" => Playing("Ecco the Dolphin"),
            "m_2207_e" => Playing("Flicky"),
            "m_9432_e" => Playing("Golden Axe II"),
            "m_5015_e" => Playing("Golden Axe"),
            "m_5017_e" => Playing("Gunstar Heroes"),
            "m_0732_e" => Playing("Altered Beast"),
            "m_2245_e" or "m_2245_pd" or "m_2245_pf" => Playing("Landstalker"),
            "m_1654_e" => Playing("Target Earth"),
            "m_7050_e" => Playing("Light Crusader"),
            "m_5027_e" => Playing("M.U.S.H.A."),
            "m_5028_e" => Playing("Phantasy Star IV"),
            "m_9155_e" => Playing("Pulseman"),
            "m_5030_e" => Playing("Dr. Robotnik's Mean Bean Machine"),
            "m_0098_e" => Playing("Crusader of Centy"),
            "m_0098_k" => Playing("신창세기 라그나센티"),
            "m_0098_pd" or "m_0098_pf" or "m_0098_ps" => Playing("Soleil"),
            "m_5033_e" => Playing("Ristar"),
            "m_1987_e" => Playing("MEGA MAN: THE WILY WARS"),
            "m_2609_e" => Playing("WOLF OF THE BATTLEFIELD: MERCS"),
            "m_3353_e" => Playing("Shining Force II"),
            "m_5036_e" => Playing("Shining Force"),
            "m_9866_e" => Playing("Sonic The Hedgehog Spinball"),
            "m_5041_e" => Playing("Sonic The Hedgehog 2"),
            "m_5523_e" => Playing("Space Harrier II"),
            "m_0041_e" => Playing("STREET FIGHTER II' : SPECIAL CHAMPION EDITION"),
            "m_5044_e" => Playing("STRIDER"),
            "m_6353_e" => Playing("Super Fantasy Zone"),
            "m_9569_e" => Playing("Beyond Oasis"),
            "m_9569_k" => Playing("스토리 오브 도어"),
            "m_9569_pd" or "m_9569_ps" => Playing("The Story of Thor"),
            "m_9569_pf" => Playing("La Légende de Thor"),
            "m_5049_e" => Playing("Shinobi III: Return of the Ninja Master"),
            "m_6811_e" => Playing("The Revenge of Shinobi"),
            "m_4372_e" => Playing("Thunder Force II"),
            "m_1535_e" => Playing("ToeJam & Earl in Panic on Funkotron"),
            "m_0432_e" => Playing("ToeJam & Earl"),
            "m_5052_e" => Playing("Castlevania: BLOODLINES"),
            "m_3626_e" => Playing("VectorMan"),
            "m_7955_e" => Playing("Sword of Vermilion"),
            "m_0394_e" => Playing("Virtua Fighter 2"),
            "m_9417_e" => Playing("Zero Wing"),

            #endregion
            
            #region Nintendo 64

            "n_1653_e" or "n_1653_p" => Playing("1080º ™ Snowboarding"),
            "n_4868_e" or "n_4868_p" => Playing("Banjo Kazooie™"),
            "n_1226_e" or "n_1226_p" => Playing("Banjo-Tooie™"),
            "n_3083_e" or "n_3083_p" => Playing("Blast Corps"),
            "n_3007_e" => Playing("Dr. Mario™ 64"),
            "n_4238_e" => Playing("Excitebike™ 64"),
            "n_1870_e" => Playing("Extreme G"),
            "n_2456_e" => Playing("F-Zero™ X"),
            "n_4631_e" => Playing("GoldenEye 007"),
            "n_1635_e" => Playing("Harvest Moon 64"),
            "n_2225_e" => Playing("Iggy’s Reckin’ Balls"),
            "n_1625_e" or "n_1625_p" => Playing("JET FORCE GEMINI™"),
            "n_3052_e" => Playing("Kirby 64™: The Crystal Shards"),
            "n_4371_e" => Playing("Mario Golf™"),
            "n_3013_e" => Playing("Mario Kart™ 64"),
            "n_1053_e" or "n_1053_p" => Playing("Mario Party™ 2"),
            "n_2965_e" or "n_2965_p" => Playing("Mario Party™ 3"),
            "n_4737_e" or "n_4737_p" => Playing("Mario Party™"),
            "n_3017_e" => Playing("Mario Tennis™"),
            "n_2992_e" or "n_2992_p" => Playing("Paper Mario™"),
            "n_3783_e" or "n_3783_p" => Playing("Pilotwings™ 64"),
            "n_1848_e" or "n_1848_pd" or "n_1848_pf" => Playing("Pokémon™ Puzzle League"),
            "n_3240_e" or "n_3240_pd" or "n_3240_pf" or "n_3240_pi" or "n_3240_ps" => Playing("Pokémon Snap™"),
            "n_4590_e" or "n_4590_pd" or "n_4590_pf" or "n_4590_pi" or "n_4590_ps" => Playing("Pokémon Stadium™"),
            "n_3309_e" or "n_3309_pd" or "n_3309_pf" or "n_3309_pi" or "n_3309_ps" => Playing("Pokémon Stadium 2™"),
            "n_3029_e" => Playing("Sin & Punishment™"),
            "n_3030_e" => Playing("Star Fox™ 64"),
            "n_3030_p" => Playing("Lylat Wars™"),
            "n_3031_e" or "n_3031_p" => Playing("Super Mario 64™"),
            "n_4813_e" or "n_4813_p" => Playing("Wave Race™ 64"),
            "n_3034_e" => Playing("WIN BACK: COVERT OPERATIONS"),
            "n_3034_p" => Playing("OPERATION: WIN BACK"),
            "n_3036_e" or "n_3036_p" => Playing("Yoshi's Story™"),
            "n_1407_e" or "n_1407_p" => Playing("The Legend of Zelda™: Majora's Mask™"),
            "n_3038_e" or "n_3038_p" => Playing("The Legend of Zelda™: Ocarina of Time™"),

            #endregion

            #region NES

            "clv_p_naaae" => Playing("Super Mario Bros.™"),
            "clv_p_naabe" => Playing("Super Mario Bros.™: The Lost Levels"),
            "clv_p_naace" or "clv_p_naace_sp1" => Playing("Super Mario Bros.™ 3"),
            "clv_p_naade" => Playing("Super Mario Bros.™ 2"),
            "clv_p_naaee" => Playing("Donkey Kong™"),
            "clv_p_naafe" => Playing("Donkey Kong Jr.™"),
            "clv_p_naage" => Playing("Donkey Kong™ 3"),
            "clv_p_naahe" => Playing("Excitebike™"),
            "clv_p_naaje" => Playing("EarthBound Beginnings"),
            "clv_p_naame" => Playing("NES™ Open Tournament Golf"),
            "clv_p_naane" or "clv_p_naane_sp1" => Playing("The Legend of Zelda™"),
            "clv_p_naape" or "clv_p_naape_sp1" => Playing("Kirby's Adventure™"),
            "clv_p_naaqe" or "clv_p_naaqe_sp1" or "clv_p_naaqe_sp2" => Playing("Metroid™"),
            "clv_p_naare" => Playing("Balloon Fight™"),
            "clv_p_naase" or "clv_p_naase_sp1" => Playing("Zelda II - The Adventure of Link™"),
            "clv_p_naate" => Playing("Punch-Out!!™ Featuring Mr. Dream"),
            "clv_p_naaue" => Playing("Ice Climber™"),
            "clv_p_naave" or "clv_p_naave_sp1" => Playing("Kid Icarus™"),
            "clv_p_naawe" => Playing("Mario Bros.™"),
            "clv_p_naaxe" or "clv_p_naaxe_sp1" => Playing("Dr. Mario™"),
            "clv_p_naaye" => Playing("Yoshi™"),
            "clv_p_naaze" => Playing("StarTropics™"),
            "clv_p_nabce" or "clv_p_nabce_sp1" => Playing("Ghosts'n Goblins™"),
            "clv_p_nabre" or "clv_p_nabre_sp1" or "clv_p_nabre_sp2" => Playing("Gradius"),
            "clv_p_nacbe" or "clv_p_nacbe_sp1" => Playing("Ninja Gaiden"),
            "clv_p_nacce" => Playing("Solomon's Key"),
            "clv_p_nacde" => Playing("Tecmo Bowl"),
            "clv_p_nacfe" => Playing("Double Dragon"),
            "clv_p_nache" => Playing("Double Dragon II: The Revenge"),
            "clv_p_nacje" => Playing("River City Ransom"),
            "clv_p_nacke" => Playing("Super Dodge Ball"),
            "clv_p_nacle" => Playing("Downtown Nekketsu March Super-Awesome Field Day!"),
            "clv_p_nacpe" => Playing("The Mystery of Atlantis"),
            "clv_p_nacre" => Playing("Soccer"),
            "clv_p_nacse" or "clv_p_nacse_sp1" => Playing("Ninja JaJaMaru-kun"),
            "clv_p_nacte" => Playing("Ice Hockey"),
            "clv_p_nacue" or "clv_p_nacue_sp1" => Playing("Blaster Master"),
            "clv_p_nacwe" => Playing("ADVENTURES OF LOLO"),
            "clv_p_nacxe" => Playing("Wario's Woods™"),
            "clv_p_nacye" => Playing("Tennis"),
            "clv_p_nacze" => Playing("Wrecking Crew™"),
            "clv_p_nadbe" => Playing("Joy Mech Fight™"),
            "clv_p_nadde" or "clv_p_nadde_sp1" => Playing("Star Soldier"),
            "clv_p_nadke" => Playing("Tetris®"),
            "clv_p_nadle" => Playing("Pro Wrestling"),
            "clv_p_nadpe" => Playing("Baseball"),
            "clv_p_nadte" or "clv_p_nadte_sp1" => Playing("TwinBee"),
            "clv_p_nadue" or "clv_p_nadue_sp1" => Playing("Mighty Bomb Jack"),
            "clv_p_nadve" => Playing("Kung-Fu Heroes"),
            "clv_p_nadxe" => Playing("City Connection"),
            "clv_p_nadye" => Playing("Rygar"),
            "clv_p_naeae" => Playing("Crystalis"),
            "clv_p_naece" => Playing("Vice: Project Doom"),
            "clv_p_naehe" => Playing("Clu Clu Land™"),
            "clv_p_naeie" => Playing("VS. Excitebike™"),
            "clv_p_naeje" => Playing("Volleyball™"),
            "clv_p_naeke" => Playing("JOURNEY TO SILIUS"),
            "clv_p_naele" => Playing("S.C.A.T.: Special Cybernetic Attack Team"),
            "clv_p_naeme" => Playing("Shadow of the Ninja"),
            "clv_p_naene" => Playing("Nightshade"),
            "clv_p_naepe" => Playing("The Immortal"),
            "clv_p_naeqe" => Playing("Eliminator Boat Duel"),
            "clv_p_naere" => Playing("Fire 'n Ice"),
            "clv_p_nafce" => Playing("XEVIOUS"),
            "clv_p_nagpe" => Playing("DAIVA STORY 6 IMPERIAL OF NIRSARTIA"),
            "clv_p_nagqe" => Playing("DIG DUGⅡ"),
            "clv_p_nague" => Playing("MAPPY-LAND"),
            "clv_p_nahhe" => Playing("Mach Rider™"),
            "clv_p_nahje" => Playing("Pinball"),
            "clv_p_nahre" => Playing("Mystery Tower"),
            "clv_p_nahte" => Playing("Urban Champion™"),
            "clv_p_nahue" => Playing("Donkey Kong Jr.™ Math"),
            "clv_p_nahve" => Playing("The Mysterious Murasame Castle"),
            "clv_p_najae" => Playing("DEVIL WORLD™"),
            "clv_p_najbe" => Playing("Golf"),
            "clv_p_najpe" => Playing("R.C. PRO-AM™"),
            "clv_p_najre" => Playing("COBRA TRIANGLE™"),
            "clv_p_najse" => Playing("SNAKE RATTLE N ROLL™"),
            "clv_p_najte" => Playing("SOLAR® JETMAN"),

            #endregion

            #region SNES

            "s_2180_e" => Playing("BATTLETOADS™ DOUBLE DRAGON™"),
            "s_2179_e" => Playing("BATTLETOADS™ IN BATTLEMANIACS"),
            "s_2182_e" => Playing("BIG RUN"),
            "s_2156_e" => Playing("Bombuzal"),
            "s_2002_e" => Playing("BRAWL BROTHERS"),
            "s_2025_e" => Playing("Breath of Fire II"),
            "s_2003_e" => Playing("Breath Of Fire"),
            "s_2163_e" => Playing("Claymates"),
            "s_2150_e" => Playing("Congo's Caper"),
            "s_2171_e" => Playing("COSMO GANG THE PUZZLE"),
            "s_2004_e" => Playing("Demon's Crest"),
            "s_2026_e" => Playing("Kunio-kun no Dodgeball da yo Zen'in Shūgō!"),
            "s_2060_e" => Playing("Donkey Kong Country 2: Diddy's Kong Quest"),
            "s_2061_e" => Playing("Donkey Kong Country 3: Dixie Kong's Double Trouble!"),
            "s_2055_e" => Playing("Donkey Kong Country"),
            "s_2139_e" => Playing("DOOMSDAY WARRIOR"),
            "s_2051_e" => Playing("EarthBound"),
            "s_2162_e" => Playing("Earthworm Jim™ 2"),
            "s_2005_e" => Playing("F-ZERO™"),
            "s_2183_e" => Playing("FATAL FURY 2"),
            "s_2174_e" => Playing("Fighter's History"),
            "s_2037_e" => Playing("Harvest Moon"),
            "s_2161_e" => Playing("Jelly Boy"),
            "s_2006_e" => Playing("Joe & Mac 2: Lost in the Tropics"),
            "s_2169_e" => Playing("Caveman Ninja"),
            "s_2181_e" => Playing("KILLER INSTINCT™"),
            "s_2029_e" or "s_2029_e_sp1" => Playing("Kirby Super Star™"),
            "s_2121_e" => Playing("Kirby's Avalanche™"),
            "s_2007_e" or "s_2007_e_sp1" => Playing("Kirby's Dream Course™"),
            "s_2008_e" or "s_2008_e_sp1" => Playing("Kirby's Dream Land™ 3"),
            "s_2172_e" => Playing("Kirby’s Star Stacker™"),
            "s_2151_e" => Playing("Magical Drop2"),
            "s_2044_e" => Playing("Mario's Super Picross"),
            "s_2038_e" => Playing("Natsume Championship Wrestling"),
            "s_2140_e" => Playing("Operation Logic Bomb"),
            "s_2034_e" => Playing("Panel de Pon"),
            "s_2009_e" => Playing("Pilotwings™"),
            "s_2010_e" => Playing("Pop'n TwinBee"),
            "s_2157_e" => Playing("Prehistorik Man"),
            "s_2145_e" => Playing("Psycho Dream"),
            "s_2141_e" => Playing("Rival Turf!"),
            "s_2152_e" => Playing("SIDE POCKET"),
            "s_2158_e" => Playing("Spanky’s™ Quest"),
            "s_2031_e" => Playing("Star Fox™ 2"),
            "s_2011_e" => Playing("Star Fox™"),
            "s_2012_e" => Playing("Stunt Race FX™"),
            "s_2032_e" => Playing("Amazing Hebereke"),
            "s_2159_e" => Playing("Super Baseball Simulator 1.000"),
            "s_2013_e" => Playing("SUPER E.D.F. EARTH DEFENSE FORCE"),
            "s_2014_e" => Playing("Smash Tennis"),
            "s_2015_e" => Playing("Super Ghouls'n Ghosts™"),
            "s_2033_e" => Playing("Super Mario All-Stars™"),
            "s_2016_e" or "s_2016_e_sp1" => Playing("Super Mario Kart™"),
            "s_2017_e" or "s_2017_e_sp1" => Playing("Super Mario World™"),
            "s_2018_e" or "s_2018_e_sp1" => Playing("Super Metroid™"),
            "s_2184_e" => Playing("Super Ninja Boy"),
            "s_2019_e" or "s_2019_e_sp1" => Playing("Super Punch-Out!!™"),
            "s_2020_e" => Playing("Super Puyo Puyo 2"),
            "s_2133_e" => Playing("SUPER R-TYPE"),
            "s_2021_e" => Playing("Super Soccer"),
            "s_2022_e" => Playing("Super Tennis"),
            "s_2136_e" => Playing("Sutte Hakkun"),
            "s_2142_e" => Playing("The Ignition Factor"),
            "s_2143_e" => Playing("The Peace Keepers"),
            "s_2146_e" => Playing("Tuff E Nuff"),
            "s_2144_e" => Playing("SUPER VALIS Ⅳ"),
            "s_2049_e" => Playing("Wild Guns"),
            "s_2096_e" => Playing("Wrecking Crew™ '98"),
            "s_2023_e" => Playing("Super Mario World™ 2: Yoshi's Island™"),
            "s_2024_e" => Playing("The Legend of Zelda™: A Link to the Past™"),

            #endregion

            #region GameBoy

            "c_7224_e" or "c_7224_p" => Playing("Alone in the Dark: The New Nightmare"),
            "c_5022_e" => Playing("Blaster Master: Enemy Below"),
            "c_3381_e" => Playing("Game & Watch™ Gallery 3"),
            "c_0282_e" => Playing("Kirby Tilt ‘n’ Tumble™"),
            "c_4471_e" or "c_4471_p" => Playing("Mario Golf™"),
            "c_9947_e" => Playing("Mario Tennis™"),
            "c_3191_e" or "c_3191_p" or "c_3191_x" => Playing("Pokémon™ Trading Card Game"),
            "c_8914_e" or "c_8914_p" => Playing("Quest for Camelot™"),
            "c_2648_e" => Playing("Tetris® DX"),
            "c_5928_e" => Playing("Wario Land™ 3"),
            "c_3996_e" or "c_3996_pd" or "c_3996_pf" => Playing("The Legend of Zelda™: Link's Awakening DX™"),
            "c_8852_e" or "c_8852_p" => Playing("The Legend of Zelda™: Oracle of Ages™"),
            "c_9130_e" or "c_9130_p" => Playing("The Legend of Zelda™: Oracle of Seasons™"),
            "d_6879_e" => Playing("Alleyway™"),
            "d_7618_e" => Playing("Baseball"),
            "d_6005_e" => Playing("BurgerTime Deluxe"),
            "d_7120_e" => Playing("Castlevania Legends"),
            "d_2744_e" => Playing("Dr. Mario™"),
            "d_1593_e" => Playing("Donkey Kong Land 2™"),
            "d_7216_e" => Playing("Donkey Kong Land III™"),
            "d_4971_e" => Playing("Donkey Kong Land™"),
            "d_7984_e" => Playing("GARGOYLE'S QUEST"),
            "d_8212_e" => Playing("Kirby's Dream Land™ 2"),
            "d_5661_e" => Playing("Kirby's Dream Land™"),
            "d_3837_e" => Playing("MEGA MAN II"),
            "d_1965_e" => Playing("MEGA MAN III"),
            "d_0194_e" => Playing("MEGA MAN IV"),
            "d_1425_e" => Playing("MEGA MAN V"),
            "d_9324_e" => Playing("MEGA MAN: DR. WILY'S REVENGE"),
            "d_1577_e" => Playing("Metroid™ II - Return of Samus™"),
            "d_5124_e" => Playing("Super Mario Land™ 2 - 6 Golden Coins™"),
            "d_7970_e" => Playing("Super Mario Land™"),
            "d_8484_e" => Playing("Tetris®"),

            #endregion

            #region GameBoy Advance

            "a_9694_e" => Playing("Densetsu no Starfy 1"),
            "a_5600_e" => Playing("Densetsu no Starfy 2"),
            "a_7565_e" => Playing("Densetsu no Starfy 3"),
            "a_6553_e" => Playing("F-ZERO CLIMAX"),
            "a_7842_e" or "a_7842_p" => Playing("F-Zero™- GP Legend"),
            "a_9283_e" => Playing("F-Zero™ Maximum Velocity"),
            "a_3744_e" or "a_3744_x" or "a_3744_y" => Playing("Fire Emblem™"),
            "a_8978_d" or "a_8978_e" or "a_8978_f" or "a_8978_i" or "a_8978_s" => Playing("Golden Sun™: The Lost Age"),
            "a_3108_d" or "a_3108_e" or "a_3108_f" or "a_3108_i" or "a_3108_s" => Playing("Golden Sun™"),
            "a_3654_e" or "a_3654_p" => Playing("Kirby™ & The Amazing Mirror"),
            "a_7279_p" => Playing("Kuru Kuru Kururin™"),
            "a_7311_e" or "a_7311_p" => Playing("Mario & Luigi™: Superstar Saga"),
            "a_6845_e" => Playing("Mario Kart™: Super Circuit™"),
            "a_4139_e" or "a_4139_p" => Playing("Metroid™ Fusion"),
            "a_6834_e" or "a_6834_p" => Playing("Metroid™: Zero Mission"),
            "a_8989_e" or "a_8989_p" => Playing("Pokémon™ Mystery Dungeon: Red Rescue Team"),
            "a_9444_e" => Playing("Super Mario™ Advance"),
            "a_9901_e" or "a_9901_p" => Playing("Super Mario™ Advance 4: Super Mario Bros.™ 3"),
            "a_2939_e" => Playing("Super Mario World™: Super Mario Advance 2"),
            "a_2939_p" => Playing("Super Mario World™: Super Mario Advance 2™"),
            "a_1302_e" => Playing("WarioWare™, Inc.: Mega Microgame$!"),
            "a_1302_p" => Playing("WarioWare™, Inc.: Minigame Mania."),
            "a_6960_e" or "a_6960_p" => Playing("Yoshi's Island™: Super Mario™ Advance 3"),
            "a_5190_e" or "a_5190_p" => Playing("The Legend of Zelda™: A Link to the Past™ Four Swords"),
            "a_8665_e" or "a_8665_p" => Playing("The Legend of Zelda™: The Minish Cap"),

            #endregion

            _ => FormattedValue.ForceReset
        };
    }
}
