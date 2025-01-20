using Ryujinx.HLE.UI;

namespace Ryujinx.Headless
{
    internal class HeadlessHostUiTheme : IHostUITheme
    {
        public string FontFamily => "sans-serif";

        public ThemeColor DefaultBackgroundColor => new(1, 0, 0, 0);
        public ThemeColor DefaultForegroundColor => new(1, 1, 1, 1);
        public ThemeColor DefaultBorderColor => new(1, 1, 1, 1);
        public ThemeColor SelectionBackgroundColor => new(1, 1, 1, 1);
        public ThemeColor SelectionForegroundColor => new(1, 0, 0, 0);
    }
}
