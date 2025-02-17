using Ryujinx.Ava.UI.Models.Input;
using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Common.Configuration.Hid.Controller;
using Ryujinx.Common.Configuration.Hid.Controller.Motion;
using Ryujinx.Common.Logging;
using Ryujinx.Input;
using System.Collections.Generic;
using System.Linq;
using StickInputId = Ryujinx.Common.Configuration.Hid.Controller.StickInputId;

namespace Ryujinx.Ava.Input
{
    public class ControllerConfigurator
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

    public List<InputConfig> ReorderControllers(List<InputConfig> newConfig, List<InputConfig> oldConfig)
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
    
    public List<InputConfig> GetConfiguredControllers(
        List<IGamepad> controllers,
        List<InputConfig> oldConfig,
        HashSet<int> usedIndices,
        out bool hasNewControllersConnected)
    {
        Dictionary<string, InputConfig> oldConfigMap = oldConfig
            .Where(c => c?.Id != null)
            .ToDictionary(x => x.Id);

        Dictionary<int, InputConfig> playerIndexMap = new();
        int recognizedControllersCount = 0;

        List<IGamepad> remainingControllers = controllers.Where(c => c?.Id != null).ToList();

        // Add controllers with existing configurations
        AddExistingControllers(remainingControllers, oldConfigMap, playerIndexMap, usedIndices, ref recognizedControllersCount);

        // Add new controllers
        AddNewControllers(remainingControllers, playerIndexMap, usedIndices);

        List<InputConfig> orderedConfigs = playerIndexMap
            .OrderBy(x => x.Key)
            .Select(x => x.Value)
            .ToList();

        // Update player indices and LED colors
        UpdatePlayerIndicesAndLEDs(orderedConfigs);

        hasNewControllersConnected = controllers.Count > recognizedControllersCount;
        return orderedConfigs;
    }

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

    private int GetFirstAvailableIndex(HashSet<int> usedIndices)
    {
        for (int i = 0; i < MaxControllers; i++)
        {
            if (!usedIndices.Contains(i)) return i;
        }
        return -1; // Should not happen unless MaxControllers is exceeded
    }

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
                LeftJoycon = new LeftJoyconCommonConfig<GamepadInputId>
                {
                    DpadUp = (controllerType == ControllerType.JoyconLeft) ? GamepadInputId.Y : GamepadInputId.DpadUp,
                    DpadDown = (controllerType == ControllerType.JoyconLeft) ? GamepadInputId.A : GamepadInputId.DpadDown,
                    DpadLeft = (controllerType == ControllerType.JoyconLeft) ? GamepadInputId.B : GamepadInputId.DpadLeft,
                    DpadRight = (controllerType == ControllerType.JoyconLeft) ? GamepadInputId.X : GamepadInputId.DpadRight,
                    ButtonMinus = (controllerType == ControllerType.JoyconLeft) ? GamepadInputId.Plus : GamepadInputId.Minus,
                    ButtonL = GamepadInputId.LeftShoulder,
                    ButtonZl = GamepadInputId.LeftTrigger,
                    ButtonSl = (controllerType == ControllerType.JoyconLeft) ? GamepadInputId.LeftShoulder : GamepadInputId.Unbound,
                    ButtonSr = (controllerType == ControllerType.JoyconLeft) ? GamepadInputId.RightShoulder : GamepadInputId.Unbound,
                },
                LeftJoyconStick = new JoyconConfigControllerStick<GamepadInputId, StickInputId>
                {
                    Joystick = StickInputId.Left,
                    StickButton = GamepadInputId.LeftStick,
                    InvertStickX = false,
                    InvertStickY = false,
                    Rotate90CW = (controllerType == ControllerType.JoyconLeft),
                },
                RightJoycon = new RightJoyconCommonConfig<GamepadInputId>
                {
                    ButtonA = GamepadInputId.B,
                    ButtonB = (controllerType == ControllerType.JoyconRight) ? GamepadInputId.Y : GamepadInputId.A,
                    ButtonX = (controllerType == ControllerType.JoyconRight) ? GamepadInputId.A : GamepadInputId.Y,
                    ButtonY = GamepadInputId.X,
                    ButtonPlus = GamepadInputId.Plus,
                    ButtonR = GamepadInputId.RightShoulder,
                    ButtonZr = GamepadInputId.RightTrigger,
                    ButtonSl = (controllerType == ControllerType.JoyconRight) ? GamepadInputId.LeftShoulder : GamepadInputId.Unbound,
                    ButtonSr = (controllerType == ControllerType.JoyconRight) ? GamepadInputId.RightShoulder : GamepadInputId.Unbound,
                },
                RightJoyconStick = new JoyconConfigControllerStick<GamepadInputId, StickInputId>
                {
                    Joystick = (controllerType == ControllerType.JoyconRight) ? StickInputId.Left : StickInputId.Right,
                    StickButton = GamepadInputId.RightStick,
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
