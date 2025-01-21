using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using FluentAvalonia.UI.Controls;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Common.Models;
using Ryujinx.Ava.UI.ViewModels;
using System;
using System.Threading.Tasks;

namespace Ryujinx.Ava.UI.Windows
{
    public partial class XCITrimmerWindow : UserControl
    {
        public XCITrimmerViewModel ViewModel;

        public XCITrimmerWindow()
        {
            DataContext = this;

            InitializeComponent();
        }

        public XCITrimmerWindow(MainWindowViewModel mainWindowViewModel)
        {
            DataContext = ViewModel = new XCITrimmerViewModel(mainWindowViewModel);

            InitializeComponent();
        }

        public static async Task Show()
        {
            ContentDialog contentDialog = new()
            {
                PrimaryButtonText = string.Empty,
                SecondaryButtonText = string.Empty,
                CloseButtonText = string.Empty,
                Content = new XCITrimmerWindow(RyujinxApp.MainWindow.ViewModel),
                Title = string.Format(LocaleManager.Instance[LocaleKeys.XCITrimmerWindowTitle]),
            };

            Style bottomBorder = new(x => x.OfType<Grid>().Name("DialogSpace").Child().OfType<Border>());
            bottomBorder.Setters.Add(new Setter(IsVisibleProperty, false));

            contentDialog.Styles.Add(bottomBorder);

            await contentDialog.ShowAsync();
        }

        private void Trim(object sender, RoutedEventArgs e)
        {
            ViewModel.TrimSelected();
        }

        private void Untrim(object sender, RoutedEventArgs e)
        {
            ViewModel.UntrimSelected();
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            ((ContentDialog)Parent).Hide();
        }

        private void Cancel(Object sender, RoutedEventArgs e)
        {
            ViewModel.Cancel = true;
        }

        public void Sort_Checked(object sender, RoutedEventArgs args)
        {
            if (sender is RadioButton { Tag: string sortField })
                ViewModel.SortingField = Enum.Parse<XCITrimmerViewModel.SortField>(sortField);
        }

        public void Order_Checked(object sender, RoutedEventArgs args)
        {
            if (sender is RadioButton { Tag: string sortOrder })
                ViewModel.SortingAscending = sortOrder is "Ascending";
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (var content in e.AddedItems)
            {
                if (content is XCITrimmerFileModel applicationData)
                {
                    ViewModel.Select(applicationData);
                }
            }

            foreach (var content in e.RemovedItems)
            {
                if (content is XCITrimmerFileModel applicationData)
                {
                    ViewModel.Deselect(applicationData);
                }
            }
        }
    }
}
