using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Common.Models;
using System;
using System.Globalization;
using static Ryujinx.Common.Utilities.XCIFileTrimmer;

namespace Ryujinx.Ava.UI.Helpers
{
    internal class XCITrimmerFileStatusConverter : IValueConverter
    {
        public static XCITrimmerFileStatusConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is UnsetValueType)
            {
                return BindingOperations.DoNothing;
            }

            if (!targetType.IsAssignableFrom(typeof(string)))
            {
                return null;
            }

            if (value is not XCITrimmerFileModel app)
            {
                return null;
            }

            return app.PercentageProgress != null ? String.Empty :
                app.ProcessingOutcome != OperationOutcome.Successful && app.ProcessingOutcome != OperationOutcome.Undetermined ? LocaleManager.Instance[LocaleKeys.TitleXCIStatusFailedLabel] :
                app.Trimmable & app.Untrimmable ? LocaleManager.Instance[LocaleKeys.TitleXCIStatusPartialLabel] :
                app.Trimmable ? LocaleManager.Instance[LocaleKeys.TitleXCIStatusTrimmableLabel] :
                app.Untrimmable ? LocaleManager.Instance[LocaleKeys.TitleXCIStatusUntrimmableLabel] :
                String.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
