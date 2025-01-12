using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Ryujinx.Ava.UI.Controls
{
    public class MiniVerticalSeparator : Border
    {
        public MiniVerticalSeparator()
        {
            Width = 2;
            Height = 12;
            Margin = new Thickness();
            BorderBrush = Brushes.Gray;
            Background = Brushes.Gray;
            BorderThickness = new Thickness(1);
        }
    }
}
