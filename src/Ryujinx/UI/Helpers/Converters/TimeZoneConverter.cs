using Avalonia.Data.Converters;
using System;
using System.Globalization;
using TimeZone = Ryujinx.Ava.UI.Models.TimeZone;

namespace Ryujinx.Ava.UI.Helpers
{
    internal class TimeZoneConverter : IValueConverter
    {
        public static TimeZoneConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is TimeZone timeZone
                ? $"{timeZone.UtcDifference}  {timeZone.Location}   {timeZone.Abbreviation}"
                : null;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
