using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Common.Configuration.Hid.Controller;
using Ryujinx.Common.Configuration.Hid.Controller.Motion;

namespace Ryujinx.Ava.UI.Models.Input
{
    public partial class GamepadInputConfig : BaseModel
    {
        public bool EnableCemuHookMotion { get; set; }
        public string DsuServerHost { get; set; }
        public int DsuServerPort { get; set; }
        public int Slot { get; set; }
        public int AltSlot { get; set; }
        public bool MirrorInput { get; set; }
        public int Sensitivity { get; set; }
        public double GyroDeadzone { get; set; }

        public float WeakRumble { get; set; }
        public float StrongRumble { get; set; }

        public string Id { get; set; }
        public ControllerType ControllerType { get; set; }
        public PlayerIndex PlayerIndex { get; set; }

        [ObservableProperty] private StickInputId _leftJoystick;
        [ObservableProperty] private bool _leftInvertStickX;
        [ObservableProperty] private bool _leftInvertStickY;
        [ObservableProperty] private bool _leftRotate90;
        [ObservableProperty] private GamepadInputId _leftStickButton;

        [ObservableProperty] private StickInputId _rightJoystick;
        [ObservableProperty] private bool _rightInvertStickX;
        [ObservableProperty] private bool _rightInvertStickY;
        [ObservableProperty] private bool _rightRotate90;
        [ObservableProperty] private GamepadInputId _rightStickButton;

        [ObservableProperty] private GamepadInputId _dpadUp;
        [ObservableProperty] private GamepadInputId _dpadDown;
        [ObservableProperty] private GamepadInputId _dpadLeft;
        [ObservableProperty] private GamepadInputId _dpadRight;

        [ObservableProperty] private GamepadInputId _buttonMinus;
        [ObservableProperty] private GamepadInputId _buttonPlus;
        
        [ObservableProperty] private GamepadInputId _buttonA;
        [ObservableProperty] private GamepadInputId _buttonB;
        [ObservableProperty] private GamepadInputId _buttonX;
        [ObservableProperty] private GamepadInputId _buttonY;
        
        [ObservableProperty] private GamepadInputId _buttonZl;
        [ObservableProperty] private GamepadInputId _buttonZr;
        
        [ObservableProperty] private GamepadInputId _buttonL;
        [ObservableProperty] private GamepadInputId _buttonR;
        
        [ObservableProperty] private GamepadInputId _leftButtonSl;
        [ObservableProperty] private GamepadInputId _leftButtonSr;
        
        [ObservableProperty] private GamepadInputId _rightButtonSl;
        [ObservableProperty] private GamepadInputId _rightButtonSr;

        [ObservableProperty] private float _deadzoneLeft;
        [ObservableProperty] private float _deadzoneRight;

        [ObservableProperty] private float _rangeLeft;
        [ObservableProperty] private float _rangeRight;

        [ObservableProperty] private float _triggerThreshold;

        [ObservableProperty] private bool _enableMotion;
        
        [ObservableProperty] private bool _enableRumble;
        
        [ObservableProperty] private bool _enableLedChanging;
        
        [ObservableProperty] private Color _ledColor;
        
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

        public GamepadInputConfig(InputConfig config)
        {
            if (config != null)
            {
                Id = config.Id;
                ControllerType = config.ControllerType;
                PlayerIndex = config.PlayerIndex;

                if (config is not StandardControllerInputConfig controllerInput)
                {
                    return;
                }

                LeftJoystick = controllerInput.LeftJoyconStick.Joystick;
                LeftInvertStickX = controllerInput.LeftJoyconStick.InvertStickX;
                LeftInvertStickY = controllerInput.LeftJoyconStick.InvertStickY;
                LeftRotate90 = controllerInput.LeftJoyconStick.Rotate90CW;
                LeftStickButton = controllerInput.LeftJoyconStick.StickButton;

                RightJoystick = controllerInput.RightJoyconStick.Joystick;
                RightInvertStickX = controllerInput.RightJoyconStick.InvertStickX;
                RightInvertStickY = controllerInput.RightJoyconStick.InvertStickY;
                RightRotate90 = controllerInput.RightJoyconStick.Rotate90CW;
                RightStickButton = controllerInput.RightJoyconStick.StickButton;

                DpadUp = controllerInput.LeftJoycon.DpadUp;
                DpadDown = controllerInput.LeftJoycon.DpadDown;
                DpadLeft = controllerInput.LeftJoycon.DpadLeft;
                DpadRight = controllerInput.LeftJoycon.DpadRight;
                ButtonL = controllerInput.LeftJoycon.ButtonL;
                ButtonMinus = controllerInput.LeftJoycon.ButtonMinus;
                LeftButtonSl = controllerInput.LeftJoycon.ButtonSl;
                LeftButtonSr = controllerInput.LeftJoycon.ButtonSr;
                ButtonZl = controllerInput.LeftJoycon.ButtonZl;

                ButtonA = controllerInput.RightJoycon.ButtonA;
                ButtonB = controllerInput.RightJoycon.ButtonB;
                ButtonX = controllerInput.RightJoycon.ButtonX;
                ButtonY = controllerInput.RightJoycon.ButtonY;
                ButtonR = controllerInput.RightJoycon.ButtonR;
                ButtonPlus = controllerInput.RightJoycon.ButtonPlus;
                RightButtonSl = controllerInput.RightJoycon.ButtonSl;
                RightButtonSr = controllerInput.RightJoycon.ButtonSr;
                ButtonZr = controllerInput.RightJoycon.ButtonZr;

                DeadzoneLeft = controllerInput.DeadzoneLeft;
                DeadzoneRight = controllerInput.DeadzoneRight;
                RangeLeft = controllerInput.RangeLeft;
                RangeRight = controllerInput.RangeRight;
                TriggerThreshold = controllerInput.TriggerThreshold;

                if (controllerInput.Motion != null)
                {
                    EnableMotion = controllerInput.Motion.EnableMotion;
                    GyroDeadzone = controllerInput.Motion.GyroDeadzone;
                    Sensitivity = controllerInput.Motion.Sensitivity;

                    if (controllerInput.Motion is CemuHookMotionConfigController cemuHook)
                    {
                        EnableCemuHookMotion = true;
                        DsuServerHost = cemuHook.DsuServerHost;
                        DsuServerPort = cemuHook.DsuServerPort;
                        Slot = cemuHook.Slot;
                        AltSlot = cemuHook.AltSlot;
                        MirrorInput = cemuHook.MirrorInput;
                    }
                }

                if (controllerInput.Rumble != null)
                {
                    EnableRumble = controllerInput.Rumble.EnableRumble;
                    WeakRumble = controllerInput.Rumble.WeakRumble;
                    StrongRumble = controllerInput.Rumble.StrongRumble;
                }
                
                if (controllerInput.Led != null)
                {
                    EnableLedChanging = controllerInput.Led.EnableLed;
                    TurnOffLed = controllerInput.Led.TurnOffLed;
                    UseRainbowLed = controllerInput.Led.UseRainbow;
                    uint rawColor = controllerInput.Led.LedColor;
                    byte alpha = (byte)(rawColor >> 24);
                    byte red = (byte)(rawColor >> 16);
                    byte green = (byte)(rawColor >> 8);
                    byte blue = (byte)(rawColor % 256);
                    LedColor = new Color(alpha, red, green, blue);
                }
            }
        }

        public InputConfig GetConfig()
        {
            StandardControllerInputConfig config = new()
            {
                Id = Id,
                Backend = InputBackendType.GamepadSDL2,
                PlayerIndex = PlayerIndex,
                ControllerType = ControllerType,
                LeftJoycon = new LeftJoyconCommonConfig<GamepadInputId>
                {
                    DpadUp = DpadUp,
                    DpadDown = DpadDown,
                    DpadLeft = DpadLeft,
                    DpadRight = DpadRight,
                    ButtonL = ButtonL,
                    ButtonMinus = ButtonMinus,
                    ButtonSl = LeftButtonSl,
                    ButtonSr = LeftButtonSr,
                    ButtonZl = ButtonZl,
                },
                RightJoycon = new RightJoyconCommonConfig<GamepadInputId>
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
                LeftJoyconStick = new JoyconConfigControllerStick<GamepadInputId, StickInputId>
                {
                    Joystick = LeftJoystick,
                    InvertStickX = LeftInvertStickX,
                    InvertStickY = LeftInvertStickY,
                    Rotate90CW = LeftRotate90,
                    StickButton = LeftStickButton,
                },
                RightJoyconStick = new JoyconConfigControllerStick<GamepadInputId, StickInputId>
                {
                    Joystick = RightJoystick,
                    InvertStickX = RightInvertStickX,
                    InvertStickY = RightInvertStickY,
                    Rotate90CW = RightRotate90,
                    StickButton = RightStickButton,
                },
                Rumble = new RumbleConfigController
                {
                    EnableRumble = EnableRumble,
                    WeakRumble = WeakRumble,
                    StrongRumble = StrongRumble,
                },
                Led = new LedConfigController
                {
                    EnableLed = EnableLedChanging,
                    TurnOffLed = this.TurnOffLed,
                    UseRainbow = UseRainbowLed,
                    LedColor = LedColor.ToUInt32()
                },
                Version = InputConfig.CurrentVersion,
                DeadzoneLeft = DeadzoneLeft,
                DeadzoneRight = DeadzoneRight,
                RangeLeft = RangeLeft,
                RangeRight = RangeRight,
                TriggerThreshold = TriggerThreshold,
            };

            if (EnableCemuHookMotion)
            {
                config.Motion = new CemuHookMotionConfigController
                {
                    EnableMotion = EnableMotion,
                    MotionBackend = MotionInputBackendType.CemuHook,
                    GyroDeadzone = GyroDeadzone,
                    Sensitivity = Sensitivity,
                    DsuServerHost = DsuServerHost,
                    DsuServerPort = DsuServerPort,
                    Slot = Slot,
                    AltSlot = AltSlot,
                    MirrorInput = MirrorInput,
                };
            }
            else
            {
                config.Motion = new StandardMotionConfigController
                {
                    EnableMotion = EnableMotion,
                    MotionBackend = MotionInputBackendType.GamepadDriver,
                    GyroDeadzone = GyroDeadzone,
                    Sensitivity = Sensitivity,
                };
            }

            return config;
        }
    }
}
