using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Common.Configuration.Hid;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

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

        public ObservableCollection<CycleController> CycleControllers { get; set; } = new ObservableCollection<CycleController>();
        public ICommand AddCycleController { get; set; }
        public ICommand RemoveCycleController { get; set; }
        public bool CanRemoveCycleController => CycleControllers.Count > 0 && CycleControllers.Count < 8;

        public HotkeyConfig(KeyboardHotkeys config)
        {
            AddCycleController = MiniCommand.Create(() => CycleControllers.Add(new CycleController(CycleControllers.Count + 1, Key.Unbound)));
            RemoveCycleController = MiniCommand.Create(() => CycleControllers.Remove(CycleControllers.Last()));
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
            CycleControllers.AddRange((config.CycleControllers ?? []).Select((x, i) => new CycleController(i + 1, x)));
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
                CycleControllers = CycleControllers.Select(x => x.Hotkey).ToList()
            };
    }
}
