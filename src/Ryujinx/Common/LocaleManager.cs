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
            string localeLanguageCode = !string.IsNullOrEmpty(ConfigurationState.Instance.UI.LanguageCode.Value) ?
                ConfigurationState.Instance.UI.LanguageCode.Value : CultureInfo.CurrentCulture.Name.Replace('-', '_');
            
            LoadLanguage(localeLanguageCode);

            // Save whatever we ended up with.
            if (Program.PreviewerDetached)
            {
                ConfigurationState.Instance.UI.LanguageCode.Value = _localeLanguageCode;

                ConfigurationState.Instance.ToFileFormat().SaveConfig(Program.ConfigurationPath);
            }
            
            SetDynamicValues(LocaleKeys.DialogConfirmationTitle, RyujinxApp.FullAppName);
            SetDynamicValues(LocaleKeys.DialogUpdaterTitle, RyujinxApp.FullAppName);
            SetDynamicValues(LocaleKeys.DialogErrorTitle, RyujinxApp.FullAppName);
            SetDynamicValues(LocaleKeys.DialogWarningTitle, RyujinxApp.FullAppName);
            SetDynamicValues(LocaleKeys.DialogExitTitle, RyujinxApp.FullAppName);
            SetDynamicValues(LocaleKeys.DialogStopEmulationTitle, RyujinxApp.FullAppName);
            SetDynamicValues(LocaleKeys.RyujinxInfo, RyujinxApp.FullAppName);
            SetDynamicValues(LocaleKeys.RyujinxConfirm, RyujinxApp.FullAppName);
            SetDynamicValues(LocaleKeys.RyujinxUpdater, RyujinxApp.FullAppName);
            SetDynamicValues(LocaleKeys.RyujinxRebooter, RyujinxApp.FullAppName);
        }

        public string this[LocaleKeys key]
        {
            get
            {
                // Check if the locale contains the key.
                if (_localeStrings.TryGetValue(key, out string value))
                {
                    // Check if the localized string needs to be formatted.
                    if (_dynamicValues.TryGetValue(key, out object[] dynamicValue))
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

        public void SetDynamicValues(LocaleKeys key, params object[] values)
        {
            _dynamicValues[key] = values;

            OnPropertyChanged("Translation");
        }
        
        public string UpdateAndGetDynamicValue(LocaleKeys key, params object[] values)
        {
            SetDynamicValues(key, values);

            return this[key];
        }

        public void LoadLanguage(string languageCode)
        {
            Dictionary<LocaleKeys, string> locale = LoadJsonLanguage(languageCode);

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
            Dictionary<LocaleKeys, string> localeStrings = new();

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

                if (!Enum.TryParse<LocaleKeys>(locale.ID, out LocaleKeys localeKey))
                    continue;

                string str = locale.Translations.TryGetValue(languageCode, out string val) && !string.IsNullOrEmpty(val)
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
