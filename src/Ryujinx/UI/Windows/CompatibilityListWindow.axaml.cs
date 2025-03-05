using Avalonia.Controls;
using Avalonia.Styling;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Systems.Configuration;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Ava.UI.Windows;
using System.Threading.Tasks;

namespace Ryujinx.Ava.UI.Windows
{
    public partial class CompatibilityListWindow : StyleableAppWindow
    {
        public static Task Show(string titleId = null) =>
            ShowAsync(new CompatibilityListWindow
            {
                DataContext = new CompatibilityViewModel(RyujinxApp.MainWindow.ViewModel.ApplicationLibrary),
                SearchBoxFlush = { Text = titleId ?? string.Empty },
                SearchBoxNormal = { Text = titleId ?? string.Empty }
            });

        public CompatibilityListWindow() : base(useCustomTitleBar: true)
        {
            Title = RyujinxApp.FormatTitle(LocaleKeys.CompatibilityListTitle);
            
            TitleBar.Height = 37;
            
            InitializeComponent();

            RyuLogo.Source = MainWindowViewModel.IconBitmap;
            FlushControls.IsVisible = !ConfigurationState.Instance.ShowTitleBar;
            NormalControls.IsVisible = ConfigurationState.Instance.ShowTitleBar;
        }

        // ReSharper disable once UnusedMember.Local
        // its referenced in the axaml but rider keeps yelling at me that its unused so
        private void TextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (DataContext is not CompatibilityViewModel cvm)
                return;

            if (sender is not TextBox searchBox)
                return;
        
            cvm.Search(searchBox.Text);
        }
    }
}
