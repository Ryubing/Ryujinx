using Avalonia;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Models.Input;
using Ryujinx.Ava.UI.ViewModels.Input;
using System.Threading.Tasks;

namespace Ryujinx.UI.Views.Input
{
    public partial class LedInputView : UserControl
    {
        private readonly LedInputViewModel _viewModel;
        
        public LedInputView(ControllerInputViewModel viewModel)
        {
            DataContext = _viewModel = new LedInputViewModel
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
            if (DataContext is not LedInputViewModel lvm) return;
            if (!lvm.EnableLedChanging) return;
            if (lvm.TurnOffLed) return;
            
            lvm.ParentModel.SelectedGamepad.SetLed(args.NewColor.Value.ToUInt32());
        }

        private void ColorPickerButton_OnAttachedToVisualTree(object sender, VisualTreeAttachmentEventArgs e)
        {
            if (DataContext is not LedInputViewModel lvm) return;
            if (!lvm.EnableLedChanging) return;
            if (lvm.TurnOffLed) return;
            
            lvm.ParentModel.SelectedGamepad.SetLed(lvm.LedColor.ToUInt32());
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
            contentDialog.PrimaryButtonClick += (sender, args) =>
            {
                GamepadInputConfig config = viewModel.Config;
                config.EnableLedChanging = content._viewModel.EnableLedChanging;
                config.LedColor = content._viewModel.LedColor;
                config.UseRainbowLed = content._viewModel.UseRainbowLed;
                config.TurnOffLed = content._viewModel.TurnOffLed;
            };

            await contentDialog.ShowAsync();
        }
    }
}

