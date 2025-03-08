using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Humanizer;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.Systems.Configuration;
using System.Globalization;

namespace Ryujinx.Ava.UI.ViewModels.Input
{
    public partial class LedInputViewModel : BaseModel
    {
        public required InputViewModel ParentModel { get; init; }
        
        public RelayCommand LedDisabledChanged => Commands.Create(() =>
        {
            if (!EnableLedChanging) return;

            if (TurnOffLed)
                ParentModel.SelectedGamepad.ClearLed();
            else
                ParentModel.SelectedGamepad.SetLed(LedColor.ToUInt32());
        });
        
        [ObservableProperty] private bool _enableLedChanging;
        [ObservableProperty] private Color _ledColor;

        public string RainbowSpeedText => RainbowSpeed.ToString(CultureInfo.CurrentCulture).Truncate(4, string.Empty);
        
        public float RainbowSpeed
        {
            get => ConfigurationState.Instance.Hid.RainbowSpeed;
            set
            {
                ConfigurationState.Instance.Hid.RainbowSpeed.Value = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RainbowSpeedText));
            }
        }
        
        public bool ShowLedColorPicker => !TurnOffLed && !UseRainbowLed;
        
        private bool _turnOffLed;
        
        public bool TurnOffLed
        {
            get => _turnOffLed;
            set
            {
                _turnOffLed = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowLedColorPicker));
            }
        }
        
        private bool _useRainbowLed;
        
        public bool UseRainbowLed
        {
            get => _useRainbowLed;
            set
            {
                _useRainbowLed = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowLedColorPicker));
            }
        }
    }
}
