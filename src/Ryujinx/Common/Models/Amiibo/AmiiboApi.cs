using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ryujinx.Ava.Common.Models.Amiibo
{
    public struct AmiiboApi : IEquatable<AmiiboApi>
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("head")]
        public string Head { get; set; }
        [JsonPropertyName("tail")]
        public string Tail { get; set; }
        [JsonPropertyName("image")]
        public string Image { get; set; }
        [JsonPropertyName("amiiboSeries")]
        public string AmiiboSeries { get; set; }
        [JsonPropertyName("character")]
        public string Character { get; set; }
        [JsonPropertyName("gameSeries")]
        public string GameSeries { get; set; }
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("release")]
        public Dictionary<string, string> Release { get; set; }

        [JsonPropertyName("gamesSwitch")]
        public List<AmiiboApiGamesSwitch> GamesSwitch { get; set; }

        public readonly override string ToString()
        {
            return Name;
        }

        public readonly string GetId()
        {
            return Head + Tail;
        }

        public readonly bool Equals(AmiiboApi other)
        {
            return Head + Tail == other.Head + other.Tail;
        }

        public readonly override bool Equals(object obj)
        {
            return obj is AmiiboApi other && Equals(other);
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(Head, Tail);
        }

        public static bool operator ==(AmiiboApi left, AmiiboApi right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AmiiboApi left, AmiiboApi right)
        {
            return !(left == right);
        }
    }
}
