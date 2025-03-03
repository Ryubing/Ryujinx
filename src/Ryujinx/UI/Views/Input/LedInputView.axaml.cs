using Avalonia;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Controls;
using Ryujinx.Ava.UI.Models.Input;
using Ryujinx.Ava.UI.ViewModels.Input;
using System.Threading.Tasks;

namespace Ryujinx.UI.Views.Input
{
    public partial class LedInputView : RyujinxControl<LedInputViewModel>
    {
        public LedInputView(ControllerInputViewModel viewModel)
        {
            ViewModel = new LedInputViewModel
            {
                ParentModel = viewModel.ParentModel,
                TurnOffLed = viewModel.Config.TurnOffLed,
                EnableLedChanging = viewModel.Config.EnableLedChanging,
                LedColor = viewModel.Config.LedColor,
                UseRainbowLed = viewModel.Config.UseRainbowLed,
            };
            
            InitializeComponent();
        }
        
        private void ColorPickerButton_OnColorChanged(ColorPickerButton sender, ColorButtonColorChangedEventArgs args)
        {
            if (!args.NewColor.HasValue) return;
            if (!ViewModel.EnableLedChanging) return;
            if (ViewModel.TurnOffLed) return;
            
            ViewModel.ParentModel.SelectedGamepad.SetLed(args.NewColor.Value.ToUInt32());
        }

        private void ColorPickerButton_OnAttachedToVisualTree(object sender, VisualTreeAttachmentEventArgs e)
        {
            if (!ViewModel.EnableLedChanging) return;
            if (ViewModel.TurnOffLed) return;
            
            ViewModel.ParentModel.SelectedGamepad.SetLed(ViewModel.LedColor.ToUInt32());
        }
        
        public static async Task Show(ControllerInputViewModel viewModel)
        {
            LedInputView content = new(viewModel);

            ContentDialog contentDialog = new()
            {
                Title = LocaleManager.Instance[LocaleKeys.ControllerLedTitle],
                PrimaryButtonText = LocaleManager.Instance[LocaleKeys.ControllerSettingsSave],
                SecondaryButtonText = string.Empty,
                CloseButtonText = LocaleManager.Instance[LocaleKeys.ControllerSettingsClose],
                Content = content,
            };
            contentDialog.PrimaryButtonClick += (_, _) =>
            {
                GamepadInputConfig config = viewModel.Config;
                config.EnableLedChanging = content.ViewModel.EnableLedChanging;
                config.LedColor = content.ViewModel.LedColor;
                config.UseRainbowLed = content.ViewModel.UseRainbowLed;
                config.TurnOffLed = content.ViewModel.TurnOffLed;
            };

            await contentDialog.ShowAsync();
        }
    }
}

