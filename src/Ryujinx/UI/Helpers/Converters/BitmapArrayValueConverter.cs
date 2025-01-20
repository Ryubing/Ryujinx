using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Globalization;
using System.IO;

namespace Ryujinx.Ava.UI.Helpers
{
    internal class BitmapArrayValueConverter : IValueConverter
    {
        public static BitmapArrayValueConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value)
            {
                case null:
                    return null;
                case byte[] buffer when targetType == typeof(IImage):
                    {
                        MemoryStream mem = new(buffer);

                        return new Bitmap(mem);
                    }
                default:
                    throw new NotSupportedException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
