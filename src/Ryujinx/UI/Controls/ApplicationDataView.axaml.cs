using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Styling;
using FluentAvalonia.UI.Controls;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Ava.UI.Windows;
using Ryujinx.Ava.Utilities.AppLibrary;
using Ryujinx.Ava.Utilities.Compat;
using System.Linq;
using System.Threading.Tasks;

namespace Ryujinx.Ava.UI.Controls
{
    public partial class ApplicationDataView : UserControl
    {
        public static async Task Show(ApplicationData appData)
        {
            ContentDialog contentDialog = new()
            {
                Title = appData.Name,
                PrimaryButtonText = string.Empty,
                SecondaryButtonText = string.Empty,
                CloseButtonText = LocaleManager.Instance[LocaleKeys.SettingsButtonClose],
                MinWidth = 256,
                Content = new ApplicationDataView { DataContext = new ApplicationDataViewModel(appData) }
            };

            Style closeButton = new(x => x.Name("CloseButton"));
            closeButton.Setters.Add(new Setter(WidthProperty, 160d));

            Style closeButtonParent = new(x => x.Name("CommandSpace"));
            closeButtonParent.Setters.Add(new Setter(HorizontalAlignmentProperty,
                Avalonia.Layout.HorizontalAlignment.Center));

            contentDialog.Styles.Add(closeButton);
            contentDialog.Styles.Add(closeButtonParent);

            await ContentDialogHelper.ShowAsync(contentDialog);
        }
        
        public ApplicationDataView()
        {
            InitializeComponent();
        }
        
        private async void PlayabilityStatus_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Content: TextBlock playabilityLabel })
                return;

            if (RyujinxApp.AppLifetime.Windows.TryGetFirst(x => x is ContentDialogOverlayWindow, out Window window))
                window.Close(ContentDialogResult.None);
            
            await CompatibilityList.Show((string)playabilityLabel.Tag);
        }

        private async void IdString_OnClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MainWindowViewModel mwvm)
                return;
            
            if (sender is not Button { Content: TextBlock idText })
                return;

            if (!RyujinxApp.IsClipboardAvailable(out IClipboard clipboard))
                return;
            
            ApplicationData appData = mwvm.Applications.FirstOrDefault(it => it.IdString == idText.Text);
            if (appData is null)
                return;
            
            await clipboard.SetTextAsync(appData.IdString);
                
            NotificationHelper.ShowInformation(
                "Copied Title ID", 
                $"{appData.Name} ({appData.IdString})");
        }
    }
}

