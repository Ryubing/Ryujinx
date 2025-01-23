using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using FluentAvalonia.UI.Controls;
using LibHac.Tools.FsSystem.NcaUtils;
using Ryujinx.Ava.Common;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Common.Models;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Ava.Utilities.AppLibrary;
using Ryujinx.Common.Helper;
using System.Threading.Tasks;

namespace Ryujinx.Ava.UI.Windows
{
    public partial class DownloadableContentManagerWindow : UserControl
    {
        public DownloadableContentManagerViewModel ViewModel;

        public DownloadableContentManagerWindow()
        {
            DataContext = this;

            InitializeComponent();
        }

        public DownloadableContentManagerWindow(ApplicationLibrary applicationLibrary, ApplicationData applicationData)
        {
            DataContext = ViewModel = new DownloadableContentManagerViewModel(applicationLibrary, applicationData);

            InitializeComponent();
        }

        public static async Task Show(ApplicationLibrary applicationLibrary, ApplicationData applicationData)
        {
            ContentDialog contentDialog = new()
            {
                PrimaryButtonText = string.Empty,
                SecondaryButtonText = string.Empty,
                CloseButtonText = string.Empty,
                Content = new DownloadableContentManagerWindow(applicationLibrary, applicationData),
                Title = string.Format(LocaleManager.Instance[LocaleKeys.DlcWindowTitle], applicationData.Name, applicationData.IdBaseString),
            };

            Style bottomBorder = new(x => x.OfType<Grid>().Name("DialogSpace").Child().OfType<Border>());
            bottomBorder.Setters.Add(new Setter(IsVisibleProperty, false));

            contentDialog.Styles.Add(bottomBorder);

            await contentDialog.ShowAsync();
        }

        private void SaveAndClose(object sender, RoutedEventArgs routedEventArgs)
        {
            ViewModel.Save();
            ((ContentDialog)Parent).Hide();
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            ((ContentDialog)Parent).Hide();
        }

        private void RemoveDLC(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: DownloadableContentModel dlc })
            {
                ViewModel.Remove(dlc);
            }
        }

        private void OpenLocation(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: DownloadableContentModel dlc })
            {
                OpenHelper.LocateFile(dlc.ContainerPath);
            }
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (var content in e.AddedItems)
            {
                if (content is DownloadableContentModel model)
                {
                    ViewModel.Enable(model);
                }
            }

            foreach (var content in e.RemovedItems)
            {
                if (content is DownloadableContentModel model)
                {
                    ViewModel.Disable(model);
                }
            }
        }
    }
}
