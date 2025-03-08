using Ryujinx.Common.Utilities;
using System.Text.Json.Serialization;

namespace Ryujinx.Ava.Systems.Configuration.System
{
    [JsonConverter(typeof(TypedStringEnumConverter<Language>))]
    public enum Language
    {
        Japanese,
        AmericanEnglish,
        French,
        German,
        Italian,
        Spanish,
        Chinese,
        Korean,
        Dutch,
        Portuguese,
        Russian,
        Taiwanese,
        BritishEnglish,
        CanadianFrench,
        LatinAmericanSpanish,
        SimplifiedChinese,
        TraditionalChinese,
        BrazilianPortuguese,
    }

    public static class LanguageEnumHelper
    {
        public static Language ToUI(this HLE.HOS.SystemState.SystemLanguage hleLanguage)
            => (Language)hleLanguage;

        public static HLE.HOS.SystemState.SystemLanguage ToHLE(this Language uiLanguage)
            => (HLE.HOS.SystemState.SystemLanguage)uiLanguage;
    }
}
