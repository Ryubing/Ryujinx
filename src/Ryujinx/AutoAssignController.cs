using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Ava.UI.Windows;
using Ryujinx.Ava.Utilities.Configuration;
using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Common.Configuration.Hid.Controller;
using Ryujinx.Common.Configuration.Hid.Controller.Motion;
using ConfigGamepadInputId = Ryujinx.Common.Configuration.Hid.Controller.GamepadInputId;
using ConfigStickInputId = Ryujinx.Common.Configuration.Hid.Controller.StickInputId;
using Ryujinx.Common.Logging;
using Ryujinx.Input;
using Ryujinx.Input.HLE;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Ava
{
    public class AutoAssignController
    {
        private readonly InputManager _inputManager;
        private readonly MainWindowViewModel _viewModel;
        private readonly ConfigurationState _configurationState;
        
        private readonly IGamepad[] _controllers;
        
        public AutoAssignController(InputManager inputManager, MainWindowViewModel mainWindowViewModel)
        {
            _inputManager = inputManager;
            _viewModel = mainWindowViewModel;
            _configurationState = ConfigurationState.Instance;
            _inputManager.GamepadDriver.OnGamepadConnected += HandleOnGamepadConnected;
            _inputManager.GamepadDriver.OnGamepadDisconnected += HandleOnGamepadDisconnected;
            
            RefreshControllers();
        }

        public void RefreshControllers(List<InputConfig> inputConfig = null)
        {
            if (!_configurationState.Hid.EnableAutoAssign) return;
            
            List<IGamepad> controllers = _inputManager.GamepadDriver.GetGamepads().ToList();
            //if (controllers.Count == 0) return;
            
            // Get every controller config and update the configuration state
            List<InputConfig> newConfig = new();
            
            Logger.Warning?.Print(LogClass.Application, $"inputConfig: {inputConfig != null}");

            if (inputConfig != null)
            {
                newConfig = inputConfig;
            }
            else
            {
                if (!_configurationState.Hid.EnableAutoAssign) return;
                List<InputConfig> oldConfig = _configurationState.Hid.InputConfig.Value.Where(x => x != null).ToList();
                
                int index = 0;
                foreach (var controller in controllers)
                {
                    if(controller == null) continue;
                    InputConfig config = oldConfig.FirstOrDefault(x => x.Id == controller.Id) ?? CreateConfigFromController(controller);
                    config.PlayerIndex = (PlayerIndex)index;
                    newConfig.Add(config);
                    index++;
                }
            }
            
            _viewModel.AppHost?.NpadManager.ReloadConfiguration(newConfig, _configurationState.Hid.EnableKeyboard, _configurationState.Hid.EnableMouse);
            
            Logger.Warning?.Print(LogClass.Application, $"(AAC) NpadManager: {_viewModel.AppHost?.NpadManager.GetHashCode()}");

            _configurationState.Hid.InputConfig.Value = newConfig;
            
            Logger.Warning?.Print(LogClass.Application, $"inputConfig: {newConfig.Count}");
            
            ConfigurationState.Instance.ToFileFormat().SaveConfig(Program.ConfigurationPath);
        }
        
        private void HandleOnGamepadConnected(string id)
        {
            Logger.Warning?.Print(LogClass.Application, $"Gamepad connected: {id}");
            RefreshControllers();
        }
        
        private void HandleOnGamepadDisconnected(string id)
        {
            Logger.Warning?.Print(LogClass.Application, $"Gamepad disconnected: {id}");
            RefreshControllers();
        }
        
        private InputConfig CreateConfigFromController(IGamepad controller)
        {
            if (controller == null) return null;
            
            Logger.Warning?.Print(LogClass.Application, $"Creating config for controller: {controller.Id}");
            
            string id = controller.Id.Split(" ")[0];
            bool isNintendoStyle = controller.Name.Contains("Nintendo");
            ControllerType controllerType;
            
            if (isNintendoStyle && !controller.Name.Contains("(L/R)"))
            {
                if (controller.Name.Contains("(L)"))
                {
                    controllerType = ControllerType.JoyconLeft;
                }
                else if (controller.Name.Contains("(R)"))
                {
                    controllerType = ControllerType.JoyconRight;
                }
                else
                {
                    controllerType = ControllerType.ProController;
                }
            }
            else
            {
                // if it's not a nintendo controller, we assume it's a pro controller or a joycon pair
                controllerType = ControllerType.ProController;
            }
            
            InputConfig config = new StandardControllerInputConfig
            {
                Version = InputConfig.CurrentVersion,
                Backend = InputBackendType.GamepadSDL2,
                Id = id,
                ControllerType = controllerType,
                DeadzoneLeft = 0.1f,
                DeadzoneRight = 0.1f,
                RangeLeft = 1.0f,
                RangeRight = 1.0f,
                TriggerThreshold = 0.5f,
                LeftJoycon = new LeftJoyconCommonConfig<ConfigGamepadInputId>
                {
                    DpadUp = (controllerType == ControllerType.JoyconLeft) ? ConfigGamepadInputId.Y : ConfigGamepadInputId.DpadUp,
                    DpadDown = (controllerType == ControllerType.JoyconLeft) ? ConfigGamepadInputId.A : ConfigGamepadInputId.DpadDown,
                    DpadLeft = (controllerType == ControllerType.JoyconLeft) ? ConfigGamepadInputId.B : ConfigGamepadInputId.DpadLeft,
                    DpadRight = (controllerType == ControllerType.JoyconLeft) ? ConfigGamepadInputId.X : ConfigGamepadInputId.DpadRight,
                    ButtonMinus = (controllerType == ControllerType.JoyconLeft) ? ConfigGamepadInputId.Plus : ConfigGamepadInputId.Minus,
                    ButtonL = ConfigGamepadInputId.LeftShoulder,
                    ButtonZl = ConfigGamepadInputId.LeftTrigger,
                    ButtonSl = (controllerType == ControllerType.JoyconLeft) ? ConfigGamepadInputId.LeftShoulder : ConfigGamepadInputId.Unbound,
                    ButtonSr = (controllerType == ControllerType.JoyconLeft) ? ConfigGamepadInputId.RightShoulder : ConfigGamepadInputId.Unbound,
                },
                LeftJoyconStick = new JoyconConfigControllerStick<ConfigGamepadInputId, ConfigStickInputId>
                {
                    Joystick = ConfigStickInputId.Left,
                    StickButton = ConfigGamepadInputId.LeftStick,
                    InvertStickX = false,
                    InvertStickY = false,
                    Rotate90CW = (controllerType == ControllerType.JoyconLeft),
                },
                RightJoycon = new RightJoyconCommonConfig<ConfigGamepadInputId>
                {
                    ButtonA = ConfigGamepadInputId.B,
                    ButtonB = (controllerType == ControllerType.JoyconRight) ? ConfigGamepadInputId.Y : ConfigGamepadInputId.A,
                    ButtonX = (controllerType == ControllerType.JoyconRight) ? ConfigGamepadInputId.A : ConfigGamepadInputId.Y,
                    ButtonY = ConfigGamepadInputId.X,
                    ButtonPlus = ConfigGamepadInputId.Plus,
                    ButtonR = ConfigGamepadInputId.RightShoulder,
                    ButtonZr = ConfigGamepadInputId.RightTrigger,
                    ButtonSl = (controllerType == ControllerType.JoyconRight) ? ConfigGamepadInputId.LeftShoulder : ConfigGamepadInputId.Unbound,
                    ButtonSr = (controllerType == ControllerType.JoyconRight) ? ConfigGamepadInputId.RightShoulder : ConfigGamepadInputId.Unbound,
                },
                RightJoyconStick = new JoyconConfigControllerStick<ConfigGamepadInputId, ConfigStickInputId>
                {
                    Joystick = (controllerType == ControllerType.JoyconRight) ? ConfigStickInputId.Left : ConfigStickInputId.Right,
                    StickButton = ConfigGamepadInputId.RightStick,
                    InvertStickX = (controllerType == ControllerType.JoyconRight),
                    InvertStickY = (controllerType == ControllerType.JoyconRight),
                    Rotate90CW = (controllerType == ControllerType.JoyconRight),
                },
                Motion = new StandardMotionConfigController
                {
                    MotionBackend = MotionInputBackendType.GamepadDriver,
                    EnableMotion = true,
                    Sensitivity = 100,
                    GyroDeadzone = 1,
                },
                Rumble = new RumbleConfigController
                {
                    StrongRumble = 1f,
                    WeakRumble = 1f,
                    EnableRumble = false,
                },
                Led = new LedConfigController
                {
                    EnableLed = false,
                    TurnOffLed = false,
                    UseRainbow = false,
                    LedColor = 0,
                },
            };
            
            return config;
        }
    }
}
