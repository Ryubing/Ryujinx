using CommunityToolkit.Mvvm.ComponentModel;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Common.Configuration.Hid;

namespace Ryujinx.Ava.UI.Models.Input
{
    public partial class HotkeyConfig : BaseModel
    {
        [ObservableProperty] private Key _toggleVSyncMode;

        [ObservableProperty] private Key _screenshot;

        [ObservableProperty] private Key _showUI;

        [ObservableProperty] private Key _pause;

        [ObservableProperty] private Key _toggleMute;

        [ObservableProperty] private Key _resScaleUp;

        [ObservableProperty] private Key _resScaleDown;

        [ObservableProperty] private Key _volumeUp;

        [ObservableProperty] private Key _volumeDown;

        [ObservableProperty] private Key _customVSyncIntervalIncrement;

        [ObservableProperty] private Key _customVSyncIntervalDecrement;

        public HotkeyConfig(KeyboardHotkeys config)
        {
            if (config == null)
                return;

            ToggleVSyncMode = config.ToggleVSyncMode;
            Screenshot = config.Screenshot;
            ShowUI = config.ShowUI;
            Pause = config.Pause;
            ToggleMute = config.ToggleMute;
            ResScaleUp = config.ResScaleUp;
            ResScaleDown = config.ResScaleDown;
            VolumeUp = config.VolumeUp;
            VolumeDown = config.VolumeDown;
            CustomVSyncIntervalIncrement = config.CustomVSyncIntervalIncrement;
            CustomVSyncIntervalDecrement = config.CustomVSyncIntervalDecrement;
        }

        public KeyboardHotkeys GetConfig() =>
            new()
            {
                ToggleVSyncMode = ToggleVSyncMode,
                Screenshot = Screenshot,
                ShowUI = ShowUI,
                Pause = Pause,
                ToggleMute = ToggleMute,
                ResScaleUp = ResScaleUp,
                ResScaleDown = ResScaleDown,
                VolumeUp = VolumeUp,
                VolumeDown = VolumeDown,
                CustomVSyncIntervalIncrement = CustomVSyncIntervalIncrement,
                CustomVSyncIntervalDecrement = CustomVSyncIntervalDecrement,
            };
    }
}
