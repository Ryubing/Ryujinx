using Ryujinx.Ava.UI.Models.Input;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Ava.Utilities.Configuration;
using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Common.Configuration.Hid.Controller;
using Ryujinx.Common.Configuration.Hid.Controller.Motion;
using ConfigGamepadInputId = Ryujinx.Common.Configuration.Hid.Controller.GamepadInputId;
using ConfigStickInputId = Ryujinx.Common.Configuration.Hid.Controller.StickInputId;
using Ryujinx.Common.Logging;
using Ryujinx.Input;
using Ryujinx.Input.HLE;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Ava.Input
{
    public class AutoAssignController
    {
        private readonly uint[] _playerColors =
        [
            0xFFFF0000, // Player 1 - Red
            0xFF0000FF, // Player 2 - Blue
            0xFF00FF00, // Player 3 - Green
            0xFFFFFF00, // Player 4 - Yellow
            0xFFFF00FF, // Player 5 - Magenta
            0xFFFFA500, // Player 6 - Orange
            0xFF00FFFF, // Player 7 - Cyan
            0xFF800080  // Player 8 - Purple
        ];
        private const int MaxControllers = 9;
        
        private readonly InputManager _inputManager;
        private readonly MainWindowViewModel _viewModel;
        private readonly ConfigurationState _configurationState;

        public event Action ConfigurationUpdated;
        
        public AutoAssignController(InputManager inputManager, MainWindowViewModel mainWindowViewModel)
        {
            _inputManager = inputManager;
            _viewModel = mainWindowViewModel;
            _configurationState = ConfigurationState.Instance;
            _inputManager.GamepadDriver.OnGamepadConnected += HandleOnGamepadConnected;
            _inputManager.GamepadDriver.OnGamepadDisconnected += HandleOnGamepadDisconnected;
            
            RefreshControllers();
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

        public void RefreshControllers()
        {
            if (!_configurationState.Hid.EnableAutoAssign) return;

            List<IGamepad> controllers = _inputManager.GamepadDriver.GetGamepads().ToList();
            List<InputConfig> oldConfig = _configurationState.Hid.InputConfig.Value.Where(x => x != null).ToList();

            (List<InputConfig> newConfig, bool hasNewControllersConnected) = GetOrderedConfig(controllers, oldConfig);
            
            _viewModel.AppHost?.NpadManager.ReloadConfiguration(newConfig, _configurationState.Hid.EnableKeyboard, _configurationState.Hid.EnableMouse);

            if (!hasNewControllersConnected)
            {
                // there is no *new* controller, we must switch the order of the controllers in
                // oldConfig to match the new order since probably a controller was disconnected
                // or an old controller was reconnected
                newConfig = ReorderControllers(newConfig, oldConfig);
            }

            _configurationState.Hid.InputConfig.Value = newConfig;
            
            // we want to save the configuration only if a *new* controller was connected
            if(hasNewControllersConnected)
            {
                ConfigurationState.Instance.ToFileFormat().SaveConfig(Program.ConfigurationPath);
            }
            ConfigurationUpdated?.Invoke();
        }

        /// <summary>
        /// Ensures that the order of controllers remains the same unless a new controller is connected.
        /// </summary>
        private List<InputConfig> ReorderControllers(List<InputConfig> newConfig, List<InputConfig> oldConfig)
        {
            List<InputConfig> reorderedConfig = oldConfig.Select(config => new GamepadInputConfig(config).GetConfig()).ToList();

            foreach (var config in newConfig)
            {
                InputConfig substitute = reorderedConfig.FirstOrDefault(x => x.Id == config.Id);
                InputConfig toBeReplaced = reorderedConfig.FirstOrDefault(x => x.PlayerIndex == config.PlayerIndex);

                if (substitute == null || toBeReplaced == null || substitute.PlayerIndex == toBeReplaced.PlayerIndex) continue;

                (substitute.PlayerIndex, toBeReplaced.PlayerIndex) = (toBeReplaced.PlayerIndex, substitute.PlayerIndex);
            }

            return reorderedConfig;
        }

        /// <summary>
        /// Orders controllers, preserving existing mappings while assigning new controllers to available slots.
        /// </summary>
        private (List<InputConfig>, bool) GetOrderedConfig(List<IGamepad> controllers, List<InputConfig> oldConfig)
        {
            Dictionary<string, InputConfig> oldConfigMap = oldConfig.Where(c => c?.Id != null)
                .ToDictionary(x => x.Id);

            Dictionary<int, InputConfig> playerIndexMap = new();
            HashSet<int> usedIndices = new();
            int recognizedControllersCount = 0;

            List<IGamepad> remainingControllers = controllers.Where(c => c?.Id != null).ToList();

            // Assign controllers that have an existing configuration
            AddExistingControllers(remainingControllers, oldConfigMap, playerIndexMap, usedIndices, ref recognizedControllersCount);

            // Assign new controllers
            AddNewControllers(remainingControllers, playerIndexMap, usedIndices);

            List<InputConfig> orderedConfigs = playerIndexMap.OrderBy(x => x.Key).Select(x => x.Value).ToList();

            // Sequentially update PlayerIndex and LED colors
            UpdatePlayerIndicesAndLEDs(orderedConfigs);

            bool hasNewControllersConnected = controllers.Count > recognizedControllersCount;
            return (orderedConfigs, hasNewControllersConnected);
        }

        /// <summary>
        /// Adds controllers with existing configurations while keeping their PlayerIndex if available.
        /// </summary>
        private void AddExistingControllers(
            List<IGamepad> controllers,
            Dictionary<string, InputConfig> oldConfigMap,
            Dictionary<int, InputConfig> playerIndexMap,
            HashSet<int> usedIndices,
            ref int recognizedControllersCount)
        {
            foreach (var controller in controllers.ToList())
            {
                if (oldConfigMap.TryGetValue(controller.Id, out InputConfig existingConfig))
                {
                    int desiredIndex = (int)existingConfig.PlayerIndex;

                    // Ensure the index is valid and available
                    if (desiredIndex < 0 || desiredIndex >= MaxControllers || usedIndices.Contains(desiredIndex))
                    {
                        desiredIndex = GetFirstAvailableIndex(usedIndices);
                    }

                    InputConfig config = new GamepadInputConfig(existingConfig).GetConfig();
                    config.PlayerIndex = (PlayerIndex)desiredIndex;
                    usedIndices.Add(desiredIndex);
                    playerIndexMap[desiredIndex] = config;
                    recognizedControllersCount++;

                    controllers.Remove(controller);
                }
            }
        }

        /// <summary>
        /// Adds new controllers to the first available PlayerIndex.
        /// </summary>
        private void AddNewControllers(
            List<IGamepad> controllers,
            Dictionary<int, InputConfig> playerIndexMap,
            HashSet<int> usedIndices)
        {
            foreach (var controller in controllers)
            {
                InputConfig config = CreateConfigFromController(controller);
                int freeIndex = GetFirstAvailableIndex(usedIndices);
                config.PlayerIndex = (PlayerIndex)freeIndex;
                usedIndices.Add(freeIndex);
                playerIndexMap[freeIndex] = config;
            }
        }

        /// <summary>
        /// Finds the first available PlayerIndex that isn't in use.
        /// </summary>
        private int GetFirstAvailableIndex(HashSet<int> usedIndices)
        {
            for (int i = 0; i < MaxControllers; i++)
            {
                if (!usedIndices.Contains(i)) return i;
            }
            return -1; // Should not happen unless MaxControllers is exceeded
        }

        /// <summary>
        /// Updates PlayerIndex and LED configurations sequentially.
        /// </summary>
        private void UpdatePlayerIndicesAndLEDs(List<InputConfig> orderedConfigs)
        {
            for (int index = 0; index < orderedConfigs.Count; index++)
            {
                orderedConfigs[index].PlayerIndex = (PlayerIndex)index;

                if (orderedConfigs[index] is StandardControllerInputConfig standardConfig)
                {
                    Logger.Warning?.Print(LogClass.Application, $"Setting color for Player{index + 1}");
                    standardConfig.Led = new LedConfigController
                    {
                        EnableLed = true,
                        LedColor = _playerColors[index]
                    };
                }
            }
        }

        private static InputConfig CreateConfigFromController(IGamepad controller)
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
                // if it's not a nintendo controller, we assume it's a pro controller or a joy-con pair
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
                    EnableLed = true,
                    TurnOffLed = false,
                    UseRainbow = false,
                    LedColor = 0,
                },
            };
            
            return config;
        }
    }
}
