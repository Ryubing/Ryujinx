using System;

namespace Ryujinx.HLE.HOS.SystemState
{
    public class SystemStateMgr
    {
        internal static string[] LanguageCodes =
        [
            "ja",
            "en-US",
            "fr",
            "de",
            "it",
            "es",
            "zh-CN",
            "ko",
            "nl",
            "pt",
            "ru",
            "zh-TW",
            "en-GB",
            "fr-CA",
            "es-419",
            "zh-Hans",
            "zh-Hant",
            "pt-BR"
        ];

        internal long DesiredKeyboardLayout { get; private set; }

        internal SystemLanguage DesiredSystemLanguage { get; private set; }

        internal long DesiredLanguageCode { get; private set; }

        internal uint DesiredRegionCode { get; private set; }

        public TitleLanguage DesiredTitleLanguage { get; private set; }

        public bool DockedMode { get; set; }

        public ColorSet ThemeColor { get; set; }

        public string DeviceNickName { get; set; }

        public SystemStateMgr()
        {
            // TODO: Let user specify fields.
            DesiredKeyboardLayout = (long)KeyboardLayout.Default;
            DeviceNickName = "Ryujinx's Switch";
        }

        public void SetLanguage(SystemLanguage language)
        {
            DesiredSystemLanguage = language;
            DesiredLanguageCode = GetLanguageCode((int)DesiredSystemLanguage);

            DesiredTitleLanguage = language switch
            {
                SystemLanguage.Taiwanese or
                SystemLanguage.TraditionalChinese => TitleLanguage.TraditionalChinese,
                SystemLanguage.Chinese or
                SystemLanguage.SimplifiedChinese => TitleLanguage.SimplifiedChinese,
                _ => Enum.Parse<TitleLanguage>(Enum.GetName<SystemLanguage>(language)),
            };
        }

        public void SetRegion(RegionCode region)
        {
            DesiredRegionCode = (uint)region;
        }

        internal static long GetLanguageCode(int index)
        {
            if ((uint)index >= LanguageCodes.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            long code = 0;
            int shift = 0;

            foreach (char chr in LanguageCodes[index])
            {
                code |= (long)(byte)chr << shift++ * 8;
            }

            return code;
        }
    }
}
