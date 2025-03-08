using Ryujinx.Common.Utilities;
using System.Text.Json.Serialization;

namespace Ryujinx.Ava.Systems.Configuration.UI
{
    [JsonConverter(typeof(TypedStringEnumConverter<UpdaterType>))]
    public enum UpdaterType
    {
        Off,
        PromptAtStartup,
        CheckInBackground
    }
}
