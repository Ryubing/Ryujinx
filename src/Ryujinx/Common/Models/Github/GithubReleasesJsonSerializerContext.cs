using System.Text.Json.Serialization;

namespace Ryujinx.Ava.Common.Models.Github
{
    [JsonSerializable(typeof(GithubReleasesJsonResponse), GenerationMode = JsonSourceGenerationMode.Metadata)]
    public partial class GithubReleasesJsonSerializerContext : JsonSerializerContext;
}
