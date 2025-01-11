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
    internal class TitleUpdateLabelConverter : IMultiValueConverter
    {
        public static TitleUpdateLabelConverter Instance = new();

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

            var key = isBundled ? LocaleKeys.TitleBundledUpdateVersionLabel : LocaleKeys.TitleUpdateVersionLabel;
            return LocaleManager.Instance.UpdateAndGetDynamicValue(key, label);
        }

        public object[] ConvertBack(object[] values, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
