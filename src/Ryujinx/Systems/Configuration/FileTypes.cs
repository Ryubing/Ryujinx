using System;

using static Ryujinx.Ava.Systems.Configuration.ConfigurationState.UISection;

namespace Ryujinx.Ava.Systems.Configuration
{
    public enum FileTypes
    {
        NSP,
        PFS0,
        XCI,
        NCA,
        NRO,
        NSO
    }
    
    public static class FileTypesExtensions
    {
        /// <summary>
        /// Gets the current <see cref="ShownFileTypeSettings"/> value for the correlating FileType name.
        /// </summary>
        /// <param name="type">The name of the <see cref="ShownFileTypeSettings"/> parameter to get the value of.</param>
        /// <param name="config">The config instance to get the value from.</param>
        /// <returns>The current value of the setting. Value is <see langword="true"/> if the file type is to be shown on the games list, <see langword="false"/> otherwise.</returns>
        public static bool GetConfigValue(this FileTypes type, ShownFileTypeSettings config) => type switch
        {
            FileTypes.NSP => config.NSP.Value,
            FileTypes.PFS0 => config.PFS0.Value,
            FileTypes.XCI => config.XCI.Value,
            FileTypes.NCA => config.NCA.Value,
            FileTypes.NRO => config.NRO.Value,
            FileTypes.NSO => config.NSO.Value,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
    }
}
