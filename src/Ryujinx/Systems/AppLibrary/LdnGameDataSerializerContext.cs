using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ryujinx.Ava.Systems.AppLibrary
{
    [JsonSerializable(typeof(IEnumerable<LdnGameData>))]
    internal partial class LdnGameDataSerializerContext : JsonSerializerContext;
}
