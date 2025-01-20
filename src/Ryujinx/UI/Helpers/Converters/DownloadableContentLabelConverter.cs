using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Ryujinx.Ava.Common.Locale;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Ryujinx.Ava.UI.Helpers
{
    internal class DownloadableContentLabelConverter : IMultiValueConverter
    {
        public static DownloadableContentLabelConverter Instance = new();

        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Any(it => it is UnsetValueType))
            {
                return BindingOperations.DoNothing;
            }

            if (values.Count != 2 || !targetType.IsAssignableFrom(typeof(string)))
            {
                return null;
            }

            if (values is not [string label, bool isBundled])
            {
                return null;
            }

            return isBundled ? $"{LocaleManager.Instance[LocaleKeys.TitleBundledDlcLabel]} {label}" : label;
        }

        public object[] ConvertBack(object[] values, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
}
