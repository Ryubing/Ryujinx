using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ryujinx.UI.App.Common
{
    [JsonSerializable(typeof(IEnumerable<LdnGameData>))]
    internal partial class LdnGameDataSerializerContext : JsonSerializerContext
    {

    }
}
