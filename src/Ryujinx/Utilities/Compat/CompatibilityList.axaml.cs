using Avalonia.Controls;
using Avalonia.Styling;
using FluentAvalonia.UI.Controls;
using nietras.SeparatedValues;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Helpers;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Ryujinx.Ava.Utilities.Compat
{
    public partial class CompatibilityList : UserControl
    {
        public static async Task Show()
        {
            if (CompatibilityCsv.Shared is null)
            {
                await using Stream csvStream = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("RyujinxGameCompatibilityList")!;
                csvStream.Position = 0;

                CompatibilityCsv.Shared = new CompatibilityCsv(Sep.Reader().From(csvStream));
            }
            
            ContentDialog contentDialog = new()
            {
                PrimaryButtonText = string.Empty,
                SecondaryButtonText = string.Empty,
                CloseButtonText = LocaleManager.Instance[LocaleKeys.SettingsButtonClose],
                Content = new CompatibilityList
                {
                    DataContext = new CompatibilityViewModel(RyujinxApp.MainWindow.ViewModel.ApplicationLibrary)
                }
            };

            Style closeButton = new(x => x.Name("CloseButton"));
            closeButton.Setters.Add(new Setter(WidthProperty, 80d));
            
            Style closeButtonParent = new(x => x.Name("CommandSpace"));
            closeButtonParent.Setters.Add(new Setter(HorizontalAlignmentProperty, Avalonia.Layout.HorizontalAlignment.Right));

            contentDialog.Styles.Add(closeButton);
            contentDialog.Styles.Add(closeButtonParent);

            await ContentDialogHelper.ShowAsync(contentDialog);
        }
        
        public CompatibilityList()
        {
            InitializeComponent();
        }

        private void TextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (DataContext is not CompatibilityViewModel cvm)
                return;

            if (sender is not TextBox searchBox)
                return;
        
            cvm.Search(searchBox.Text);
        }
    }
}
