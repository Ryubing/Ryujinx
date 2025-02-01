using CommunityToolkit.Mvvm.ComponentModel;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Common.Configuration.Hid.Keyboard;

namespace Ryujinx.Ava.UI.Models.Input
{
    public partial class KeyboardInputConfig : BaseModel
    {
        public string Id { get; set; }
        public ControllerType ControllerType { get; set; }
        public PlayerIndex PlayerIndex { get; set; }

        [ObservableProperty] private Key _leftStickUp;
        [ObservableProperty] private Key _leftStickDown;
        [ObservableProperty] private Key _leftStickLeft;
        [ObservableProperty] private Key _leftStickRight;
        [ObservableProperty] private Key _leftStickButton;

        [ObservableProperty] private Key _rightStickUp;
        [ObservableProperty] private Key _rightStickDown;
        [ObservableProperty] private Key _rightStickLeft;
        [ObservableProperty] private Key _rightStickRight;
        [ObservableProperty] private Key _rightStickButton;

        [ObservableProperty] private Key _dpadUp;
        [ObservableProperty] private Key _dpadDown;
        [ObservableProperty] private Key _dpadLeft;
        [ObservableProperty] private Key _dpadRight;
        
        [ObservableProperty] private Key _buttonMinus;
        [ObservableProperty] private Key _buttonPlus;
        
        [ObservableProperty] private Key _buttonA;
        [ObservableProperty] private Key _buttonB;
        [ObservableProperty] private Key _buttonX;
        [ObservableProperty] private Key _buttonY;
        
        [ObservableProperty] private Key _buttonL;
        [ObservableProperty] private Key _buttonR;
        
        [ObservableProperty] private Key _buttonZl;
        [ObservableProperty] private Key _buttonZr;
        
        [ObservableProperty] private Key _leftButtonSl;
        [ObservableProperty] private Key _leftButtonSr;
        
        [ObservableProperty] private Key _rightButtonSl;
        [ObservableProperty] private Key _rightButtonSr;

        public KeyboardInputConfig(InputConfig config)
        {
            if (config != null)
            {
                Id = config.Id;
                ControllerType = config.ControllerType;
                PlayerIndex = config.PlayerIndex;

                if (config is not StandardKeyboardInputConfig keyboardConfig)
                {
                    return;
                }

                LeftStickUp = keyboardConfig.LeftJoyconStick.StickUp;
                LeftStickDown = keyboardConfig.LeftJoyconStick.StickDown;
                LeftStickLeft = keyboardConfig.LeftJoyconStick.StickLeft;
                LeftStickRight = keyboardConfig.LeftJoyconStick.StickRight;
                LeftStickButton = keyboardConfig.LeftJoyconStick.StickButton;

                RightStickUp = keyboardConfig.RightJoyconStick.StickUp;
                RightStickDown = keyboardConfig.RightJoyconStick.StickDown;
                RightStickLeft = keyboardConfig.RightJoyconStick.StickLeft;
                RightStickRight = keyboardConfig.RightJoyconStick.StickRight;
                RightStickButton = keyboardConfig.RightJoyconStick.StickButton;

                DpadUp = keyboardConfig.LeftJoycon.DpadUp;
                DpadDown = keyboardConfig.LeftJoycon.DpadDown;
                DpadLeft = keyboardConfig.LeftJoycon.DpadLeft;
                DpadRight = keyboardConfig.LeftJoycon.DpadRight;
                ButtonL = keyboardConfig.LeftJoycon.ButtonL;
                ButtonMinus = keyboardConfig.LeftJoycon.ButtonMinus;
                LeftButtonSl = keyboardConfig.LeftJoycon.ButtonSl;
                LeftButtonSr = keyboardConfig.LeftJoycon.ButtonSr;
                ButtonZl = keyboardConfig.LeftJoycon.ButtonZl;

                ButtonA = keyboardConfig.RightJoycon.ButtonA;
                ButtonB = keyboardConfig.RightJoycon.ButtonB;
                ButtonX = keyboardConfig.RightJoycon.ButtonX;
                ButtonY = keyboardConfig.RightJoycon.ButtonY;
                ButtonR = keyboardConfig.RightJoycon.ButtonR;
                ButtonPlus = keyboardConfig.RightJoycon.ButtonPlus;
                RightButtonSl = keyboardConfig.RightJoycon.ButtonSl;
                RightButtonSr = keyboardConfig.RightJoycon.ButtonSr;
                ButtonZr = keyboardConfig.RightJoycon.ButtonZr;
            }
        }

        public InputConfig GetConfig()
        {
            StandardKeyboardInputConfig config = new()
            {
                Id = Id,
                Backend = InputBackendType.WindowKeyboard,
                PlayerIndex = PlayerIndex,
                ControllerType = ControllerType,
                LeftJoycon = new LeftJoyconCommonConfig<Key>
                {
                    DpadUp = DpadUp,
                    DpadDown = DpadDown,
                    DpadLeft = DpadLeft,
                    DpadRight = DpadRight,
                    ButtonL = ButtonL,
                    ButtonMinus = ButtonMinus,
                    ButtonZl = ButtonZl,
                    ButtonSl = LeftButtonSl,
                    ButtonSr = LeftButtonSr,
                },
                RightJoycon = new RightJoyconCommonConfig<Key>
                {
                    ButtonA = ButtonA,
                    ButtonB = ButtonB,
                    ButtonX = ButtonX,
                    ButtonY = ButtonY,
                    ButtonPlus = ButtonPlus,
                    ButtonSl = RightButtonSl,
                    ButtonSr = RightButtonSr,
                    ButtonR = ButtonR,
                    ButtonZr = ButtonZr,
                },
                LeftJoyconStick = new JoyconConfigKeyboardStick<Key>
                {
                    StickUp = LeftStickUp,
                    StickDown = LeftStickDown,
                    StickRight = LeftStickRight,
                    StickLeft = LeftStickLeft,
                    StickButton = LeftStickButton,
                },
                RightJoyconStick = new JoyconConfigKeyboardStick<Key>
                {
                    StickUp = RightStickUp,
                    StickDown = RightStickDown,
                    StickLeft = RightStickLeft,
                    StickRight = RightStickRight,
                    StickButton = RightStickButton,
                },
                Version = InputConfig.CurrentVersion,
            };

            return config;
        }
    }
}
