using Avalonia.Controls;
using Avalonia.Styling;
using FluentAvalonia.UI.Controls;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Helpers;
using System.Threading.Tasks;

namespace Ryujinx.Ava.Utilities.Compat
{
    public partial class CompatibilityList : UserControl
    {
        public static async Task Show(string titleId = null)
        {
            ContentDialog contentDialog = new()
            {
                PrimaryButtonText = string.Empty,
                SecondaryButtonText = string.Empty,
                CloseButtonText = LocaleManager.Instance[LocaleKeys.SettingsButtonClose],
                Content = new CompatibilityList
                {
                    DataContext = new CompatibilityViewModel(RyujinxApp.MainWindow.ViewModel.ApplicationLibrary), 
                    SearchBox = {
                        Text = titleId ?? ""
                    }
                }
            };

            await ContentDialogHelper.ShowAsync(contentDialog.ApplyStyles());
        }
        
        public CompatibilityList()
        {
            InitializeComponent();
        }

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
