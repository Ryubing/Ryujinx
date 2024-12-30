using System.Text.Json.Serialization;

namespace Ryujinx.Ava.Common.Models.Amiibo
{
    [JsonSerializable(typeof(AmiiboJson))]
    public partial class AmiiboJsonSerializerContext : JsonSerializerContext;
}
