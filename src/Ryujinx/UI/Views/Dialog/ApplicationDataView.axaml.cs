using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
using FluentAvalonia.UI.Controls;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Controls;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Ava.UI.Windows;
using Ryujinx.Ava.Systems.AppLibrary;
using System.Linq;
using System.Threading.Tasks;

namespace Ryujinx.Ava.UI.Views.Dialog
{
    public partial class ApplicationDataView : RyujinxControl<ApplicationDataViewModel>
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
                Content = new ApplicationDataView { ViewModel = new ApplicationDataViewModel(appData) }
            };

            await ContentDialogHelper.ShowAsync(contentDialog.ApplyStyles(160, HorizontalAlignment.Center));
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
            
            await CompatibilityListWindow.Show((string)playabilityLabel.Tag);
        }

        private async void IdString_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Content: TextBlock idText })
                return;

            if (!RyujinxApp.IsClipboardAvailable(out IClipboard clipboard))
                return;
            
            ApplicationData appData = RyujinxApp.MainWindow.ViewModel.Applications.FirstOrDefault(it => it.IdString == idText.Text);
            if (appData is null)
                return;
            
            await clipboard.SetTextAsync(appData.IdString);
                
            NotificationHelper.ShowInformation(
                "Copied Title ID", 
                $"{appData.Name} ({appData.IdString})");
        }
    }
}

