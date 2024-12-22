using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Ryujinx.Common.Utilities
{
    public class JsonHelper
    {
        private static readonly JsonNamingPolicy _snakeCasePolicy = new SnakeCaseNamingPolicy();
        private const int DefaultFileWriteBufferSize = 4096;

        /// <summary>
        /// Creates new serializer options with default settings.
        /// </summary>
        /// <remarks>
        /// It is REQUIRED for you to save returned options statically or as a part of static serializer context
        /// in order to avoid performance issues. You can safely modify returned options for your case before storing.
        /// </remarks>
        public static JsonSerializerOptions GetDefaultSerializerOptions(bool indented = true) =>
            new()
            {
                DictionaryKeyPolicy = _snakeCasePolicy,
                PropertyNamingPolicy = _snakeCasePolicy,
                WriteIndented = indented,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

        public static string Serialize<T>(T value, JsonTypeInfo<T> typeInfo) 
            => JsonSerializer.Serialize(value, typeInfo);

        public static T Deserialize<T>(string value, JsonTypeInfo<T> typeInfo) 
            => JsonSerializer.Deserialize(value, typeInfo);

        public static T Deserialize<T>(ReadOnlySpan<byte> utf8Value, JsonTypeInfo<T> typeInfo) 
            => JsonSerializer.Deserialize<T>(utf8Value, typeInfo);

        public static void SerializeToFile<T>(string filePath, T value, JsonTypeInfo<T> typeInfo)
        {
            using FileStream file = File.Create(filePath, DefaultFileWriteBufferSize, FileOptions.WriteThrough);
            JsonSerializer.Serialize(file, value, typeInfo);
        }

        public static T DeserializeFromFile<T>(string filePath, JsonTypeInfo<T> typeInfo)
        {
            using FileStream file = File.OpenRead(filePath);
            return JsonSerializer.Deserialize(file, typeInfo);
        }

        public static void SerializeToStream<T>(Stream stream, T value, JsonTypeInfo<T> typeInfo) => JsonSerializer.Serialize(stream, value, typeInfo);

        private class SnakeCaseNamingPolicy : JsonNamingPolicy
        {
            public override string ConvertName(string name)
            {
                if (string.IsNullOrEmpty(name))
                {
                    return name;
                }

                StringBuilder builder = new();

                for (int i = 0; i < name.Length; i++)
                {
                    char c = name[i];

                    if (char.IsUpper(c))
                    {
                        if (!(i == 0 || char.IsUpper(name[i - 1])))
                            builder.Append('_');

                        builder.Append(char.ToLowerInvariant(c));
                    }
                    else
                    {
                        builder.Append(c);
                    }
                }

                return builder.ToString();
            }
        }
    }
}
