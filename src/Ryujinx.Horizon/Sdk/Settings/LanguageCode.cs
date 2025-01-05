using Ryujinx.Common.Memory;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Ryujinx.Horizon.Sdk.Settings
{
    [StructLayout(LayoutKind.Sequential, Size = 0x8, Pack = 0x1)]
    struct LanguageCode
    {
        private static readonly string[] _languageCodes =
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

        public Array8<byte> Value;

        public bool IsValid()
        {
            int length = Value.AsSpan().IndexOf((byte)0);
            if (length < 0)
            {
                return false;
            }

            string str = Encoding.ASCII.GetString(Value.AsSpan()[..length]);

            return _languageCodes.AsSpan().Contains(str);
        }

        public LanguageCode(Language language)
        {
            if ((uint)language >= _languageCodes.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(language));
            }

            Value = new LanguageCode(_languageCodes[(int)language]).Value;
        }

        public LanguageCode(string strCode)
        {
            Encoding.ASCII.GetBytes(strCode, Value.AsSpan());
        }
    }
}
