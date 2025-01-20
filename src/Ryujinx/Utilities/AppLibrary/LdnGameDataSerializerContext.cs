using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ryujinx.Ava.Utilities.AppLibrary
{
    [JsonSerializable(typeof(IEnumerable<LdnGameData>))]
    internal partial class LdnGameDataSerializerContext : JsonSerializerContext;
}
