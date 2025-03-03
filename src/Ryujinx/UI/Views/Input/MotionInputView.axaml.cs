using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Controls;
using Ryujinx.Ava.UI.Models.Input;
using Ryujinx.Ava.UI.ViewModels.Input;
using System.Threading.Tasks;

namespace Ryujinx.Ava.UI.Views.Input
{
    public partial class MotionInputView : RyujinxControl<MotionInputViewModel>
    {
        public MotionInputView()
        {
            InitializeComponent();
        }

        public MotionInputView(ControllerInputViewModel viewModel)
        {
            GamepadInputConfig config = viewModel.Config;

            ViewModel = new MotionInputViewModel
            {
                Slot = config.Slot,
                AltSlot = config.AltSlot,
                DsuServerHost = config.DsuServerHost,
                DsuServerPort = config.DsuServerPort,
                MirrorInput = config.MirrorInput,
                Sensitivity = config.Sensitivity,
                GyroDeadzone = config.GyroDeadzone,
                EnableCemuHookMotion = config.EnableCemuHookMotion,
            };

            InitializeComponent();
        }

        public static async Task Show(ControllerInputViewModel viewModel)
        {
            MotionInputView content = new(viewModel);

            ContentDialog contentDialog = new()
            {
                Title = LocaleManager.Instance[LocaleKeys.ControllerMotionTitle],
                PrimaryButtonText = LocaleManager.Instance[LocaleKeys.ControllerSettingsSave],
                SecondaryButtonText = string.Empty,
                CloseButtonText = LocaleManager.Instance[LocaleKeys.ControllerSettingsClose],
                Content = content,
            };
            contentDialog.PrimaryButtonClick += (_, _) =>
            {
                GamepadInputConfig config = viewModel.Config;
                config.Slot = content.ViewModel.Slot;
                config.Sensitivity = content.ViewModel.Sensitivity;
                config.GyroDeadzone = content.ViewModel.GyroDeadzone;
                config.AltSlot = content.ViewModel.AltSlot;
                config.DsuServerHost = content.ViewModel.DsuServerHost;
                config.DsuServerPort = content.ViewModel.DsuServerPort;
                config.EnableCemuHookMotion = content.ViewModel.EnableCemuHookMotion;
                config.MirrorInput = content.ViewModel.MirrorInput;
            };

            await contentDialog.ShowAsync();
        }
    }
}
