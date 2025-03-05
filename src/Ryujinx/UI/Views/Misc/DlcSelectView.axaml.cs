using Avalonia.Controls;
using Avalonia.Styling;
using FluentAvalonia.UI.Controls;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Common.Models;
using Ryujinx.Ava.UI.Controls;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Ava.Systems.AppLibrary;
using System.Threading.Tasks;

namespace Ryujinx.Ava.UI.Views.Misc
{
    public partial class DlcSelectView : RyujinxControl<DlcSelectViewModel>
    {
        public DlcSelectView()
        {
            InitializeComponent();
        }

#nullable enable
        public static async Task<DownloadableContentModel?> Show(ulong selectedTitleId, ApplicationLibrary appLibrary)
#nullable disable
        {
            DlcSelectViewModel viewModel = new(selectedTitleId, appLibrary);

            ContentDialog contentDialog = new()
            {
                PrimaryButtonText = LocaleManager.Instance[LocaleKeys.Continue],
                SecondaryButtonText = string.Empty,
                CloseButtonText = string.Empty,
                Content = new DlcSelectView { ViewModel = viewModel }
            };

            await ContentDialogHelper.ShowAsync(contentDialog.ApplyStyles());

            return viewModel.SelectedDlc;
        }
    }
}
