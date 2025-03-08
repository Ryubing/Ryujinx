using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Ryujinx.Ava.Systems.Configuration;
using Ryujinx.Ava.UI.ViewModels;
using System.Reflection;

namespace Ryujinx.Ava.UI.Controls
{
    public class RyujinxLogo : Image
    {
        // The UI specifically uses a thicker bordered variant of the icon to avoid crunching out the border at lower resolutions.
        // For an example of this, download canary 1.2.95, then open the settings menu, and look at the icon in the top-left.
        // The border gets reduced to colored pixels in the 4 corners.
        public static readonly Bitmap Bitmap =
            new(Assembly.GetAssembly(typeof(MainWindowViewModel))!
                .GetManifestResourceStream("Ryujinx.Assets.UIImages.Logo_Ryujinx_AntiAlias.png")!);
        
        public RyujinxLogo()
        {
            Margin = new Thickness(7, 7, 7, 0);
            Height = 25;
            Width = 25;
            Source = Bitmap;
            IsVisible = !ConfigurationState.Instance.ShowOldUI;
        }
    }
}
