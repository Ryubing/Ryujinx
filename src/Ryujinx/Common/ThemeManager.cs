using System;

namespace Ryujinx.Ava.Common
{
    public static class ThemeManager
    {
        public static event Action ThemeChanged;

        public static void OnThemeChanged()
        {
            ThemeChanged?.Invoke();
        }
    }
}
