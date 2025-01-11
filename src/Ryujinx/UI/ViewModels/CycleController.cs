using Ryujinx.Ava.Common.Locale;
using Ryujinx.Common.Configuration.Hid;

namespace Ryujinx.Ava.UI.ViewModels
{
    public class CycleController : BaseModel
    {
        private string _player;
        private Key _hotkey;

        public string Player
        {
            get => _player;
            set
            {
                _player = value;
                OnPropertyChanged(nameof(Player));
            }
        }

        public Key Hotkey
        {
            get => _hotkey;
            set
            {
                _hotkey = value;
                OnPropertyChanged(nameof(Hotkey));
            }
        }

        public CycleController(int v, Key x)
        {
            Player = v switch
            {
                1 => LocaleManager.Instance[LocaleKeys.ControllerSettingsPlayer1],
                2 => LocaleManager.Instance[LocaleKeys.ControllerSettingsPlayer2],
                3 => LocaleManager.Instance[LocaleKeys.ControllerSettingsPlayer3],
                4 => LocaleManager.Instance[LocaleKeys.ControllerSettingsPlayer4],
                5 => LocaleManager.Instance[LocaleKeys.ControllerSettingsPlayer5],
                6 => LocaleManager.Instance[LocaleKeys.ControllerSettingsPlayer6],
                7 => LocaleManager.Instance[LocaleKeys.ControllerSettingsPlayer7],
                8 => LocaleManager.Instance[LocaleKeys.ControllerSettingsPlayer8],
                _ => LocaleManager.Instance[LocaleKeys.ControllerSettingsPlayer] + " " + v
            };
            Hotkey = x;
        }
    }
}
