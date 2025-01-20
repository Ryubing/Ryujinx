using Avalonia.Data.Converters;
using Avalonia.Media;
using Gommon;
using Ryujinx.Ava.Common.Locale;
using System;
using System.Globalization;

namespace Ryujinx.Ava.UI.Helpers
{
    public class PlayabilityStatusConverter : IValueConverter
    {
        private static readonly Lazy<PlayabilityStatusConverter> _shared = new(() => new());
        public static PlayabilityStatusConverter Shared => _shared.Value;

        public object Convert(object value, Type _, object __, CultureInfo ___)
            => value.Cast<LocaleKeys>() switch
            {
                LocaleKeys.CompatibilityListNothing or 
                    LocaleKeys.CompatibilityListBoots or 
                    LocaleKeys.CompatibilityListMenus => Brushes.Red,
                LocaleKeys.CompatibilityListIngame => Brushes.Yellow,
                _ => Brushes.ForestGreen
            };

        public object ConvertBack(object value, Type _, object __, CultureInfo ___)
            => throw new NotSupportedException();
    }
}
