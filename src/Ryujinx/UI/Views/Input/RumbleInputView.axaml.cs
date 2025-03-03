using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Controls;
using Ryujinx.Ava.UI.Models.Input;
using Ryujinx.Ava.UI.ViewModels.Input;
using System.Threading.Tasks;

namespace Ryujinx.Ava.UI.Views.Input
{
    public partial class RumbleInputView : RyujinxControl<RumbleInputViewModel>
    {
        public RumbleInputView()
        {
            InitializeComponent();
        }

        public RumbleInputView(ControllerInputViewModel viewModel)
        {
            GamepadInputConfig config = viewModel.Config;

            ViewModel = new RumbleInputViewModel
            {
                StrongRumble = config.StrongRumble,
                WeakRumble = config.WeakRumble,
            };

            InitializeComponent();
        }

        public static async Task Show(ControllerInputViewModel viewModel)
        {
            RumbleInputView content = new(viewModel);

            ContentDialog contentDialog = new()
            {
                Title = LocaleManager.Instance[LocaleKeys.ControllerRumbleTitle],
                PrimaryButtonText = LocaleManager.Instance[LocaleKeys.ControllerSettingsSave],
                SecondaryButtonText = string.Empty,
                CloseButtonText = LocaleManager.Instance[LocaleKeys.ControllerSettingsClose],
                Content = content,
            };

            contentDialog.PrimaryButtonClick += (_, _) =>
            {
                GamepadInputConfig config = viewModel.Config;
                config.StrongRumble = content.ViewModel.StrongRumble;
                config.WeakRumble = content.ViewModel.WeakRumble;
            };

            await contentDialog.ShowAsync();
        }
    }
}
