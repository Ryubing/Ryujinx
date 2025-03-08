using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Gommon;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Systems.AppLibrary;
using System;
using System.Globalization;
using System.Text;

namespace Ryujinx.Ava.UI.Helpers
{
    internal class MultiplayerInfoConverter : MarkupExtension, IValueConverter
    {
        public static readonly MultiplayerInfoConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not ApplicationData { HasLdnGames: true } applicationData)
                return "";
            
            return new StringBuilder()
                .AppendLine(
                    LocaleManager.Instance[LocaleKeys.GameListHeaderHostedGames]
                        .Format(applicationData.GameCount))
                .Append(
                    LocaleManager.Instance[LocaleKeys.GameListHeaderPlayerCount]
                        .Format(applicationData.PlayerCount))
                .ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Instance;
        }
    }
}
