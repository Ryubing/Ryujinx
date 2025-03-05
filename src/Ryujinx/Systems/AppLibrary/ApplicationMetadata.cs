using System;
using System.Text.Json.Serialization;

namespace Ryujinx.Ava.Systems.AppLibrary
{
    public class ApplicationMetadata
    {
        public string Title { get; set; }
        public bool Favorite { get; set; }

        [JsonPropertyName("timespan_played")]
        public TimeSpan TimePlayed { get; set; } = TimeSpan.Zero;

        [JsonPropertyName("last_played_utc")]
        public DateTime? LastPlayed { get; set; } = null;

        [JsonPropertyName("time_played")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public double TimePlayedOld { get; set; }

        [JsonPropertyName("last_played")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string LastPlayedOld { get; set; }

        /// <summary>
        /// Updates <see cref="LastPlayed"/>. Call this before launching a game.
        /// </summary>
        public void UpdatePreGame()
        {
            LastPlayed = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates <see cref="LastPlayed"/> and <see cref="TimePlayed"/>. Call this after a game ends.
        /// </summary>
        public void UpdatePostGame()
        {
            DateTime? prevLastPlayed = LastPlayed;
            UpdatePreGame();

            if (!prevLastPlayed.HasValue)
            {
                return;
            }

            TimeSpan diff = DateTime.UtcNow - prevLastPlayed.Value;
            double newTotalSeconds = TimePlayed.Add(diff).TotalSeconds;
            TimePlayed = TimeSpan.FromSeconds(Math.Round(newTotalSeconds, MidpointRounding.AwayFromZero));
        }
    }
}
