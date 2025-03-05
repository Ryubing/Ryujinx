using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using FluentAvalonia.UI.Controls;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Common.Models;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Ava.Systems.AppLibrary;
using Ryujinx.Common.Helper;
using System.Threading.Tasks;

namespace Ryujinx.Ava.UI.Windows
{
    public partial class TitleUpdateWindow : UserControl
    {
        public readonly TitleUpdateViewModel ViewModel;

        public TitleUpdateWindow()
        {
            DataContext = this;

            InitializeComponent();
        }

        public TitleUpdateWindow(ApplicationLibrary applicationLibrary, ApplicationData applicationData)
        {
            DataContext = ViewModel = new TitleUpdateViewModel(applicationLibrary, applicationData);

            InitializeComponent();
        }

        public static async Task Show(ApplicationLibrary applicationLibrary, ApplicationData applicationData)
        {
            ContentDialog contentDialog = new()
            {
                PrimaryButtonText = string.Empty,
                SecondaryButtonText = string.Empty,
                CloseButtonText = string.Empty,
                Content = new TitleUpdateWindow(applicationLibrary, applicationData),
                Title = LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.GameUpdateWindowHeading, applicationData.Name, applicationData.IdBaseString),
            };

            Style bottomBorder = new(x => x.OfType<Grid>().Name("DialogSpace").Child().OfType<Border>());
            bottomBorder.Setters.Add(new Setter(IsVisibleProperty, false));

            contentDialog.Styles.Add(bottomBorder);

            await contentDialog.ShowAsync();
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            ((ContentDialog)Parent).Hide();
        }

        public void Save(object sender, RoutedEventArgs e)
        {
            ViewModel.Save();

            ((ContentDialog)Parent).Hide();
        }

        private void OpenLocation(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: TitleUpdateModel model })
                OpenHelper.LocateFile(model.Path);
        }

        private void RemoveUpdate(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: TitleUpdateModel model })
                ViewModel.RemoveUpdate(model);
        }

        private void RemoveAll(object sender, RoutedEventArgs e)
        {
            ViewModel.TitleUpdates.Clear();

            ViewModel.SortUpdates();
        }
    }
}
