using Gommon;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Ava.Utilities.Configuration;
using Ryujinx.Common;
using Ryujinx.Common.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Serialization;

namespace Ryujinx.Ava.Common.Locale
{
    class LocaleManager : BaseModel
    {
        private const string DefaultLanguageCode = "en_US";

        private readonly Dictionary<LocaleKeys, string> _localeStrings;
        private readonly ConcurrentDictionary<LocaleKeys, object[]> _dynamicValues;
        private string _localeLanguageCode;

        public static LocaleManager Instance { get; } = new();
        public event Action LocaleChanged;

        public LocaleManager()
        {
            _localeStrings = new Dictionary<LocaleKeys, string>();
            _dynamicValues = new ConcurrentDictionary<LocaleKeys, object[]>();

            Load();
        }

        private void Load()
        {
            var localeLanguageCode = !string.IsNullOrEmpty(ConfigurationState.Instance.UI.LanguageCode.Value) ?
                ConfigurationState.Instance.UI.LanguageCode.Value : CultureInfo.CurrentCulture.Name.Replace('-', '_');
            
            LoadLanguage(localeLanguageCode);

            // Save whatever we ended up with.
            if (Program.PreviewerDetached)
            {
                ConfigurationState.Instance.UI.LanguageCode.Value = _localeLanguageCode;

                ConfigurationState.Instance.ToFileFormat().SaveConfig(Program.ConfigurationPath);
            }
        }

        public string this[LocaleKeys key]
        {
            get
            {
                // Check if the locale contains the key.
                if (_localeStrings.TryGetValue(key, out string value))
                {
                    // Check if the localized string needs to be formatted.
                    if (_dynamicValues.TryGetValue(key, out var dynamicValue))
                        try
                        {
                            return string.Format(value, dynamicValue);
                        }
                        catch
                        {
                            // If formatting the text failed,
                            // continue to the below line & return the text without formatting.
                        }

                    return value;
                }
                
                return key.ToString(); // If the locale text doesn't exist return the key.
            }
            set
            {
                _localeStrings[key] = value;

                OnPropertyChanged();
            }
        }

        public bool IsRTL() =>
            _localeLanguageCode switch
            {
                "ar_SA" or "he_IL" => true,
                _ => false
            };

        public static string FormatDynamicValue(LocaleKeys key, params object[] values)
            => Instance.UpdateAndGetDynamicValue(key, values);

        public string UpdateAndGetDynamicValue(LocaleKeys key, params object[] values)
        {
            _dynamicValues[key] = values;

            OnPropertyChanged("Translation");

            return this[key];
        }

        public void LoadLanguage(string languageCode)
        {
            var locale = LoadJsonLanguage(languageCode);

            if (locale == null)
            {
                _localeLanguageCode = DefaultLanguageCode;
                locale = LoadJsonLanguage(_localeLanguageCode);
            }
            else
            {
                _localeLanguageCode = languageCode;
            }

            foreach ((LocaleKeys key, string val) in locale)
            {
                _localeStrings[key] = val;
            }

            OnPropertyChanged("Translation");

            LocaleChanged?.Invoke();
        }

        private static LocalesJson? _localeData;

        private static Dictionary<LocaleKeys, string> LoadJsonLanguage(string languageCode)
        {
            var localeStrings = new Dictionary<LocaleKeys, string>();

            _localeData ??= EmbeddedResources.ReadAllText("Ryujinx/Assets/locales.json")
                .Into(it => JsonHelper.Deserialize(it, LocalesJsonContext.Default.LocalesJson));

            foreach (LocalesEntry locale in _localeData.Value.Locales)
            {
                if (locale.Translations.Count < _localeData.Value.Languages.Count)
                {
                    throw new Exception($"Locale key {{{locale.ID}}} is missing languages! Has {locale.Translations.Count} translations, expected {_localeData.Value.Languages.Count}!");
                } 
                
                if (locale.Translations.Count > _localeData.Value.Languages.Count)
                {
                    throw new Exception($"Locale key {{{locale.ID}}} has too many languages! Has {locale.Translations.Count} translations, expected {_localeData.Value.Languages.Count}!");
                }

                if (!Enum.TryParse<LocaleKeys>(locale.ID, out var localeKey))
                    continue;

                var str = locale.Translations.TryGetValue(languageCode, out string val) && !string.IsNullOrEmpty(val)
                    ? val
                    : locale.Translations[DefaultLanguageCode];
                
                if (string.IsNullOrEmpty(str))
                {
                    throw new Exception($"Locale key '{locale.ID}' has no valid translations for desired language {languageCode}! {DefaultLanguageCode} is an empty string or null");
                }

                localeStrings[localeKey] = str;
            }

            return localeStrings;
        }
    }

    public struct LocalesJson
    {
        public List<string> Languages { get; set; }
        public List<LocalesEntry> Locales { get; set; }
    }

    public struct LocalesEntry
    {
        public string ID { get; set; }
        public Dictionary<string, string> Translations { get; set; }
    }

    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(LocalesJson))]
    internal partial class LocalesJsonContext : JsonSerializerContext;
}
