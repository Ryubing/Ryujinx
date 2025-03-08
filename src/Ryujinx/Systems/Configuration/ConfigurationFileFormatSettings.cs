using Ryujinx.Common.Utilities;

namespace Ryujinx.Ava.Systems.Configuration
{
    internal static class ConfigurationFileFormatSettings
    {
        public static readonly ConfigurationJsonSerializerContext SerializerContext = new(JsonHelper.GetDefaultSerializerOptions());
    }
}
