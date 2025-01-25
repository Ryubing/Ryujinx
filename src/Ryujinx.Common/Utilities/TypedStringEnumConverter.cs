#nullable enable
using Ryujinx.Common.Logging;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ryujinx.Common.Utilities
{
    /// <summary>
    /// Specifies that value of <see cref="TEnum"/> will be serialized as string in JSONs
    /// </summary>
    /// <remarks>
    /// Trimming friendly alternative to <see cref="JsonStringEnumConverter"/>.
    /// Get rid of this converter if dotnet supports similar functionality out of the box.
    /// </remarks>
    /// <typeparam name="TEnum">Type of enum to serialize</typeparam>
    public sealed class TypedStringEnumConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
    {
        public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? enumValue = reader.GetString();

            if (Enum.TryParse(enumValue, out TEnum value))
            {
                return value;
            }

            Logger.Warning?.Print(LogClass.Configuration, $"Failed to parse enum value \"{enumValue}\" for {typeof(TEnum)}, using default \"{default(TEnum)}\"");
            return default;
        }

        public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
