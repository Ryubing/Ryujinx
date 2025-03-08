using System.Text.Json.Serialization;

namespace Ryujinx.Ava.Systems.AppLibrary
{
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(ApplicationMetadata))]
    internal partial class ApplicationJsonSerializerContext : JsonSerializerContext
    {
    }
}
