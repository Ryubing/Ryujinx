using Avalonia.Controls;
using Avalonia.Styling;
using FluentAvalonia.UI.Controls;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Common.Models;
using Ryujinx.Ava.UI.Controls;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Ava.Utilities.AppLibrary;
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

            Style closeButton = new(x => x.Name("CloseButton"));
            closeButton.Setters.Add(new Setter(WidthProperty, 80d));

            Style closeButtonParent = new(x => x.Name("CommandSpace"));
            closeButtonParent.Setters.Add(new Setter(HorizontalAlignmentProperty,
                Avalonia.Layout.HorizontalAlignment.Right));

            contentDialog.Styles.Add(closeButton);
            contentDialog.Styles.Add(closeButtonParent);

            await ContentDialogHelper.ShowAsync(contentDialog);

            return viewModel.SelectedDlc;
        }
    }
}
