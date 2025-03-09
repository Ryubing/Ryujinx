using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Common.Configuration.Hid.Controller;
using Ryujinx.Common.Logging;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using static SDL2.SDL;

namespace Ryujinx.Input.SDL2
{
    internal class SDL2JoyCon : IGamepad
    {
        private bool HasConfiguration => _configuration != null;

        private readonly record struct ButtonMappingEntry(GamepadButtonInputId To, GamepadButtonInputId From)
        {
            public bool IsValid => To is not GamepadButtonInputId.Unbound && From is not GamepadButtonInputId.Unbound;
        }

        private StandardControllerInputConfig _configuration;

        private readonly Dictionary<GamepadButtonInputId,SDL_GameControllerButton> _leftButtonsDriverMapping = new()
        {
            { GamepadButtonInputId.LeftStick , SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSTICK },
             {GamepadButtonInputId.DpadUp ,SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y},
             {GamepadButtonInputId.DpadDown ,SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A},
             {GamepadButtonInputId.DpadLeft ,SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B},
             {GamepadButtonInputId.DpadRight ,SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X},
             {GamepadButtonInputId.Minus ,SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_START},
             {GamepadButtonInputId.LeftShoulder,SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_PADDLE2},
             {GamepadButtonInputId.LeftTrigger,SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_PADDLE4},
             {GamepadButtonInputId.SingleRightTrigger0,SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER},
             {GamepadButtonInputId.SingleLeftTrigger0,SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER},
        };
        private readonly Dictionary<GamepadButtonInputId,SDL_GameControllerButton> _rightButtonsDriverMapping = new()
        {
             {GamepadButtonInputId.RightStick,SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSTICK},
             {GamepadButtonInputId.A,SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B},
             {GamepadButtonInputId.B,SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y},
             {GamepadButtonInputId.X,SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A},
             {GamepadButtonInputId.Y,SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X},
             {GamepadButtonInputId.Plus,SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_START},
             {GamepadButtonInputId.RightShoulder,SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_PADDLE1},
             {GamepadButtonInputId.RightTrigger,SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_PADDLE3},
             {GamepadButtonInputId.SingleRightTrigger1,SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER},
             {GamepadButtonInputId.SingleLeftTrigger1,SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER}
        };

        private readonly Dictionary<GamepadButtonInputId, SDL_GameControllerButton> _buttonsDriverMapping;
        private readonly Lock _userMappingLock = new();

        private readonly List<ButtonMappingEntry> _buttonsUserMapping;

        private readonly StickInputId[] _stickUserMapping = new StickInputId[(int)StickInputId.Count]
        {
            StickInputId.Unbound, StickInputId.Left, StickInputId.Right,
        };

        public GamepadFeaturesFlag Features { get; }

        private nint _gamepadHandle;

        private enum JoyConType
        {
            Left, Right
        }

        public const string Prefix = "Nintendo Switch Joy-Con";
        public const string LeftName = "Nintendo Switch Joy-Con (L)";
        public const string RightName = "Nintendo Switch Joy-Con (R)";

        private readonly JoyConType _joyConType;

        public SDL2JoyCon(nint gamepadHandle, string driverId)
        {
            _gamepadHandle = gamepadHandle;
            _buttonsUserMapping = new List<ButtonMappingEntry>(10);

            Name = SDL_GameControllerName(_gamepadHandle);
            Id = driverId;
            Features = GetFeaturesFlag();

            // Enable motion tracking
            if (Features.HasFlag(GamepadFeaturesFlag.Motion))
            {
                if (SDL_GameControllerSetSensorEnabled(_gamepadHandle, SDL_SensorType.SDL_SENSOR_ACCEL,
                        SDL_bool.SDL_TRUE) != 0)
                {
                    Logger.Error?.Print(LogClass.Hid,
                        $"Could not enable data reporting for SensorType {SDL_SensorType.SDL_SENSOR_ACCEL}.");
                }

                if (SDL_GameControllerSetSensorEnabled(_gamepadHandle, SDL_SensorType.SDL_SENSOR_GYRO,
                        SDL_bool.SDL_TRUE) != 0)
                {
                    Logger.Error?.Print(LogClass.Hid,
                        $"Could not enable data reporting for SensorType {SDL_SensorType.SDL_SENSOR_GYRO}.");
                }
            }

            switch (Name)
            {
                case LeftName:
                    {
                        _buttonsDriverMapping = _leftButtonsDriverMapping;
                        _joyConType = JoyConType.Left;
                        break;
                    }
                case RightName:
                    {
                        _buttonsDriverMapping = _rightButtonsDriverMapping;
                        _joyConType = JoyConType.Right;
                        break;
                    }
            }
        }

        private GamepadFeaturesFlag GetFeaturesFlag()
        {
            GamepadFeaturesFlag result = GamepadFeaturesFlag.None;

            if (SDL_GameControllerHasSensor(_gamepadHandle, SDL_SensorType.SDL_SENSOR_ACCEL) == SDL_bool.SDL_TRUE &&
                SDL_GameControllerHasSensor(_gamepadHandle, SDL_SensorType.SDL_SENSOR_GYRO) == SDL_bool.SDL_TRUE)
            {
                result |= GamepadFeaturesFlag.Motion;
            }

            int error = SDL_GameControllerRumble(_gamepadHandle, 0, 0, 100);

            if (error == 0)
            {
                result |= GamepadFeaturesFlag.Rumble;
            }

            return result;
        }

        public string Id { get; }
        public string Name { get; }
        public bool IsConnected => SDL_GameControllerGetAttached(_gamepadHandle) == SDL_bool.SDL_TRUE;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && _gamepadHandle != nint.Zero)
            {
                SDL_GameControllerClose(_gamepadHandle);

                _gamepadHandle = nint.Zero;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }


        public void SetTriggerThreshold(float triggerThreshold)
        {

        }

        public void Rumble(float lowFrequency, float highFrequency, uint durationMs)
        {
            if (!Features.HasFlag(GamepadFeaturesFlag.Rumble))
                return;

            ushort lowFrequencyRaw = (ushort)(lowFrequency * ushort.MaxValue);
            ushort highFrequencyRaw = (ushort)(highFrequency * ushort.MaxValue);

            if (durationMs == uint.MaxValue)
            {
                if (SDL_GameControllerRumble(_gamepadHandle, lowFrequencyRaw, highFrequencyRaw, SDL_HAPTIC_INFINITY) !=
                    0)
                    Logger.Error?.Print(LogClass.Hid, "Rumble is not supported on this game controller.");
            }
            else if (durationMs > SDL_HAPTIC_INFINITY)
            {
                Logger.Error?.Print(LogClass.Hid, $"Unsupported rumble duration {durationMs}");
            }
            else
            {
                if (SDL_GameControllerRumble(_gamepadHandle, lowFrequencyRaw, highFrequencyRaw, durationMs) != 0)
                    Logger.Error?.Print(LogClass.Hid, "Rumble is not supported on this game controller.");
            }
        }

        public Vector3 GetMotionData(MotionInputId inputId)
        {
            SDL_SensorType sensorType = inputId switch
            {
                MotionInputId.Accelerometer => SDL_SensorType.SDL_SENSOR_ACCEL,
                MotionInputId.Gyroscope => SDL_SensorType.SDL_SENSOR_GYRO,
                _ => SDL_SensorType.SDL_SENSOR_INVALID
            };

            if (!Features.HasFlag(GamepadFeaturesFlag.Motion) || sensorType is SDL_SensorType.SDL_SENSOR_INVALID)
                return Vector3.Zero;

            const int ElementCount = 3;

            unsafe
            {
                float* values = stackalloc float[ElementCount];

                int result = SDL_GameControllerGetSensorData(_gamepadHandle, sensorType, (nint)values, ElementCount);

                if (result != 0)
                    return Vector3.Zero;

                Vector3 value = _joyConType switch
                {
                    JoyConType.Left => new Vector3(-values[2], values[1], values[0]),
                    JoyConType.Right => new Vector3(values[2], values[1], -values[0])
                };

                return inputId switch
                {
                    MotionInputId.Gyroscope => RadToDegree(value),
                    MotionInputId.Accelerometer => GsToMs2(value),
                    _ => value
                };
            }
        }

        private static Vector3 RadToDegree(Vector3 rad) => rad * (180 / MathF.PI);

        private static Vector3 GsToMs2(Vector3 gs) => gs / SDL_STANDARD_GRAVITY;

        public void SetConfiguration(InputConfig configuration)
        {
            lock (_userMappingLock)
            {
                _configuration = (StandardControllerInputConfig)configuration;

                _buttonsUserMapping.Clear();

                // First update sticks
                _stickUserMapping[(int)StickInputId.Left] = (StickInputId)_configuration.LeftJoyconStick.Joystick;
                _stickUserMapping[(int)StickInputId.Right] = (StickInputId)_configuration.RightJoyconStick.Joystick;


                switch (_joyConType)
                {
                    case JoyConType.Left:
                        _buttonsUserMapping.Add(new ButtonMappingEntry(GamepadButtonInputId.LeftStick, (GamepadButtonInputId)_configuration.LeftJoyconStick.StickButton));
                        _buttonsUserMapping.Add(new ButtonMappingEntry(GamepadButtonInputId.DpadUp, (GamepadButtonInputId)_configuration.LeftJoycon.DpadUp));
                        _buttonsUserMapping.Add(new ButtonMappingEntry(GamepadButtonInputId.DpadDown, (GamepadButtonInputId)_configuration.LeftJoycon.DpadDown));
                        _buttonsUserMapping.Add(new ButtonMappingEntry(GamepadButtonInputId.DpadLeft, (GamepadButtonInputId)_configuration.LeftJoycon.DpadLeft));
                        _buttonsUserMapping.Add(new ButtonMappingEntry(GamepadButtonInputId.DpadRight, (GamepadButtonInputId)_configuration.LeftJoycon.DpadRight));
                        _buttonsUserMapping.Add(new ButtonMappingEntry(GamepadButtonInputId.Minus, (GamepadButtonInputId)_configuration.LeftJoycon.ButtonMinus));
                        _buttonsUserMapping.Add(new ButtonMappingEntry(GamepadButtonInputId.LeftShoulder, (GamepadButtonInputId)_configuration.LeftJoycon.ButtonL));
                        _buttonsUserMapping.Add(new ButtonMappingEntry(GamepadButtonInputId.LeftTrigger, (GamepadButtonInputId)_configuration.LeftJoycon.ButtonZl));
                        _buttonsUserMapping.Add(new ButtonMappingEntry(GamepadButtonInputId.SingleRightTrigger0, (GamepadButtonInputId)_configuration.LeftJoycon.ButtonSr));
                        _buttonsUserMapping.Add(new ButtonMappingEntry(GamepadButtonInputId.SingleLeftTrigger0, (GamepadButtonInputId)_configuration.LeftJoycon.ButtonSl));
                        break;
                    case JoyConType.Right:
                        _buttonsUserMapping.Add(new ButtonMappingEntry(GamepadButtonInputId.RightStick, (GamepadButtonInputId)_configuration.RightJoyconStick.StickButton));
                        _buttonsUserMapping.Add(new ButtonMappingEntry(GamepadButtonInputId.A, (GamepadButtonInputId)_configuration.RightJoycon.ButtonA));
                        _buttonsUserMapping.Add(new ButtonMappingEntry(GamepadButtonInputId.B, (GamepadButtonInputId)_configuration.RightJoycon.ButtonB));
                        _buttonsUserMapping.Add(new ButtonMappingEntry(GamepadButtonInputId.X, (GamepadButtonInputId)_configuration.RightJoycon.ButtonX));
                        _buttonsUserMapping.Add(new ButtonMappingEntry(GamepadButtonInputId.Y, (GamepadButtonInputId)_configuration.RightJoycon.ButtonY));
                        _buttonsUserMapping.Add(new ButtonMappingEntry(GamepadButtonInputId.Plus, (GamepadButtonInputId)_configuration.RightJoycon.ButtonPlus));
                        _buttonsUserMapping.Add(new ButtonMappingEntry(GamepadButtonInputId.RightShoulder, (GamepadButtonInputId)_configuration.RightJoycon.ButtonR));
                        _buttonsUserMapping.Add(new ButtonMappingEntry(GamepadButtonInputId.RightTrigger, (GamepadButtonInputId)_configuration.RightJoycon.ButtonZr));
                        _buttonsUserMapping.Add(new ButtonMappingEntry(GamepadButtonInputId.SingleRightTrigger1, (GamepadButtonInputId)_configuration.RightJoycon.ButtonSr));
                        _buttonsUserMapping.Add(new ButtonMappingEntry(GamepadButtonInputId.SingleLeftTrigger1, (GamepadButtonInputId)_configuration.RightJoycon.ButtonSl));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
                SetTriggerThreshold(_configuration.TriggerThreshold);
            }
        }

        public void SetLed(uint packedRgb)
        {
        }

        public GamepadStateSnapshot GetStateSnapshot()
        {
            return IGamepad.GetStateSnapshot(this);
        }

        public GamepadStateSnapshot GetMappedStateSnapshot()
        {
            GamepadStateSnapshot rawState = GetStateSnapshot();
            GamepadStateSnapshot result = default;

            lock (_userMappingLock)
            {
                if (_buttonsUserMapping.Count == 0)
                    return rawState;


                // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
                foreach (ButtonMappingEntry entry in _buttonsUserMapping)
                {
                    if (!entry.IsValid)
                        continue;

                    // Do not touch state of button already pressed
                    if (!result.IsPressed(entry.To))
                    {
                        result.SetPressed(entry.To, rawState.IsPressed(entry.From));
                    }
                }

                (float leftStickX, float leftStickY) = rawState.GetStick(_stickUserMapping[(int)StickInputId.Left]);
                (float rightStickX, float rightStickY) = rawState.GetStick(_stickUserMapping[(int)StickInputId.Right]);

                result.SetStick(StickInputId.Left, leftStickX, leftStickY);
                result.SetStick(StickInputId.Right, rightStickX, rightStickY);
            }

            return result;
        }


        private static float ConvertRawStickValue(short value)
        {
            const float ConvertRate = 1.0f / (short.MaxValue + 0.5f);

            return value * ConvertRate;
        }

        private JoyconConfigControllerStick<GamepadInputId, Common.Configuration.Hid.Controller.StickInputId>
            GetLogicalJoyStickConfig(StickInputId inputId)
        {
            switch (inputId)
            {
                case StickInputId.Left:
                    if (_configuration.RightJoyconStick.Joystick ==
                        Common.Configuration.Hid.Controller.StickInputId.Left)
                        return _configuration.RightJoyconStick;
                    else
                        return _configuration.LeftJoyconStick;
                case StickInputId.Right:
                    if (_configuration.LeftJoyconStick.Joystick ==
                        Common.Configuration.Hid.Controller.StickInputId.Right)
                        return _configuration.LeftJoyconStick;
                    else
                        return _configuration.RightJoyconStick;
            }

            return null;
        }


        public (float, float) GetStick(StickInputId inputId)
        {
            if (inputId == StickInputId.Unbound)
                return (0.0f, 0.0f);

            if (inputId == StickInputId.Left && _joyConType == JoyConType.Right || inputId == StickInputId.Right && _joyConType == JoyConType.Left)
            {
                return (0.0f, 0.0f);
            }
            
            (short stickX, short stickY) = GetStickXY();

            float resultX = ConvertRawStickValue(stickX);
            float resultY = -ConvertRawStickValue(stickY);

            if (HasConfiguration)
            {
                var joyconStickConfig = GetLogicalJoyStickConfig(inputId);

                if (joyconStickConfig != null)
                {
                    if (joyconStickConfig.InvertStickX)
                        resultX = -resultX;

                    if (joyconStickConfig.InvertStickY)
                        resultY = -resultY;

                    if (joyconStickConfig.Rotate90CW)
                    {
                        float temp = resultX;
                        resultX = resultY;
                        resultY = -temp;
                    }
                }
            }

            return inputId switch
            {
                StickInputId.Left when _joyConType == JoyConType.Left => (resultY, -resultX),
                StickInputId.Right when _joyConType == JoyConType.Right => (-resultY, resultX),
                _ => (0.0f, 0.0f)
            };
        }

        private (short, short) GetStickXY()
        {
            return (
                SDL_GameControllerGetAxis(_gamepadHandle, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX),
                SDL_GameControllerGetAxis(_gamepadHandle, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY));
        }
        
        public bool IsPressed(GamepadButtonInputId inputId)
        {
            if (!_buttonsDriverMapping.TryGetValue(inputId, out var button))
            {
                return false;
            }

            return SDL_GameControllerGetButton(_gamepadHandle, button) == 1;
        }
    }
}
