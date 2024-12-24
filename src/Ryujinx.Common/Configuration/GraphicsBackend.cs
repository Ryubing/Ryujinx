using Ryujinx.Common.Utilities;
using System.Text.Json.Serialization;

namespace Ryujinx.Common.Configuration
{
    [JsonConverter(typeof(TypedStringEnumConverter<GraphicsBackend>))]
    public enum GraphicsBackend
    {
        Auto,
        Vulkan,
        OpenGl,
        Metal
    }
}
