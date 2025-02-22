using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Common.Configuration.Hid.Controller;
using Ryujinx.Common.Logging;
using Ryujinx.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using static SDL2.SDL;

namespace Ryujinx.Input.SDL2
{
    public class SDL2Gamepad : IGamepad
    {
        private bool HasConfiguration => _configuration != null;

        private readonly record struct ButtonMappingEntry(GamepadButtonInputId To, GamepadButtonInputId From)
        {
            public bool IsValid => To is not GamepadButtonInputId.Unbound && From is not GamepadButtonInputId.Unbound;
        }

        private StandardControllerInputConfig _configuration;

        private static readonly SDL_GameControllerButton[] _buttonsDriverMapping =
        [
            // Unbound, ignored.
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_INVALID,

            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSTICK,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSTICK,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER,

            // NOTE: The left and right trigger are axis, we handle those differently
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_INVALID,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_INVALID,

            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_LEFT,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_RIGHT,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_START,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_GUIDE,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_MISC1,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_PADDLE1,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_PADDLE2,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_PADDLE3,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_PADDLE4,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_TOUCHPAD,

            // Virtual buttons are invalid, ignored.
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_INVALID,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_INVALID,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_INVALID,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_INVALID
        ];

        private readonly Lock _userMappingLock = new();

        private readonly List<ButtonMappingEntry> _buttonsUserMapping;

        private readonly StickInputId[] _stickUserMapping =
        [
            StickInputId.Unbound,
            StickInputId.Left,
            StickInputId.Right
        ];

        public GamepadFeaturesFlag Features { get; }

        private nint _gamepadHandle;

        private float _triggerThreshold;

        public SDL2Gamepad(nint gamepadHandle, string driverId)
        {
            _gamepadHandle = gamepadHandle;
            _buttonsUserMapping = new List<ButtonMappingEntry>(20);

            Name = SDL_GameControllerName(_gamepadHandle);
            Id = driverId;
            Features = GetFeaturesFlag();
            _triggerThreshold = 0.0f;
            
            // Enable motion tracking
            if (Features.HasFlag(GamepadFeaturesFlag.Motion))
            {
                if (SDL_GameControllerSetSensorEnabled(_gamepadHandle, SDL_SensorType.SDL_SENSOR_ACCEL, SDL_bool.SDL_TRUE) != 0)
                {
                    Logger.Error?.Print(LogClass.Hid, $"Could not enable data reporting for SensorType {SDL_SensorType.SDL_SENSOR_ACCEL}.");
                }

                if (SDL_GameControllerSetSensorEnabled(_gamepadHandle, SDL_SensorType.SDL_SENSOR_GYRO, SDL_bool.SDL_TRUE) != 0)
                {
                    Logger.Error?.Print(LogClass.Hid, $"Could not enable data reporting for SensorType {SDL_SensorType.SDL_SENSOR_GYRO}.");
                }
            }
        }

        public void SetLed(uint packedRgb)
        {
            if (!Features.HasFlag(GamepadFeaturesFlag.Led)) return;

            byte red = packedRgb > 0 ? (byte)(packedRgb >> 16) : (byte)0;
            byte green = packedRgb > 0 ? (byte)(packedRgb >> 8) : (byte)0;
            byte blue = packedRgb > 0 ? (byte)(packedRgb % 256) : (byte)0;
            
            if (SDL_GameControllerSetLED(_gamepadHandle, red, green, blue) != 0)
                Logger.Debug?.Print(LogClass.Hid, "LED setting failed; probably in the middle of disconnecting.");
        }

        private GamepadFeaturesFlag GetFeaturesFlag()
        {
            GamepadFeaturesFlag result = GamepadFeaturesFlag.None;

            if (SDL_GameControllerHasSensor(_gamepadHandle, SDL_SensorType.SDL_SENSOR_ACCEL) == SDL_bool.SDL_TRUE &&
                SDL_GameControllerHasSensor(_gamepadHandle, SDL_SensorType.SDL_SENSOR_GYRO) == SDL_bool.SDL_TRUE)
            {
                result |= GamepadFeaturesFlag.Motion;
            }

            if (SDL_GameControllerHasRumble(_gamepadHandle) == SDL_bool.SDL_TRUE)
            {
                result |= GamepadFeaturesFlag.Rumble;
            }

            if (SDL_GameControllerHasLED(_gamepadHandle) == SDL_bool.SDL_TRUE)
            {
                result |= GamepadFeaturesFlag.Led;
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
            _triggerThreshold = triggerThreshold;
        }

        public void Rumble(float lowFrequency, float highFrequency, uint durationMs)
        {
            if (!Features.HasFlag(GamepadFeaturesFlag.Rumble))
                return;

            ushort lowFrequencyRaw = (ushort)(lowFrequency * ushort.MaxValue);
            ushort highFrequencyRaw = (ushort)(highFrequency * ushort.MaxValue);

            if (durationMs == uint.MaxValue)
            {
                if (SDL_GameControllerRumble(_gamepadHandle, lowFrequencyRaw, highFrequencyRaw, SDL_HAPTIC_INFINITY) != 0)
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

                Vector3 value = new(values[0], values[1], values[2]);

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

                if (Features.HasFlag(GamepadFeaturesFlag.Led) && _configuration.Led.EnableLed)
                {
                    if (_configuration.Led.TurnOffLed)
                        (this as IGamepad).ClearLed();
                    else if (_configuration.Led.UseRainbow)
                        SetLed((uint)Rainbow.Color.ToArgb());
                    
                    if (!_configuration.Led.TurnOffLed && !_configuration.Led.UseRainbow)
                        SetLed(_configuration.Led.LedColor);
                }
                
                _buttonsUserMapping.Clear();

                // First update sticks
                _stickUserMapping[(int)StickInputId.Left] = (StickInputId)_configuration.LeftJoyconStick.Joystick;
                _stickUserMapping[(int)StickInputId.Right] = (StickInputId)_configuration.RightJoyconStick.Joystick;

                // Then left joycon
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

                // Finally right joycon
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

                SetTriggerThreshold(_configuration.TriggerThreshold);
            }
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

        private JoyconConfigControllerStick<GamepadInputId, Common.Configuration.Hid.Controller.StickInputId> GetLogicalJoyStickConfig(StickInputId inputId)
        {
            switch (inputId)
            {
                case StickInputId.Left:
                    if (_configuration.RightJoyconStick.Joystick == Common.Configuration.Hid.Controller.StickInputId.Left)
                        return _configuration.RightJoyconStick;
                    else
                        return _configuration.LeftJoyconStick;
                case StickInputId.Right:
                    if (_configuration.LeftJoyconStick.Joystick == Common.Configuration.Hid.Controller.StickInputId.Right)
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

            (short stickX, short stickY) = GetStickXY(inputId);

            float resultX = ConvertRawStickValue(stickX);
            float resultY = -ConvertRawStickValue(stickY);

            if (HasConfiguration)
            {
                JoyconConfigControllerStick<GamepadInputId, Common.Configuration.Hid.Controller.StickInputId> joyconStickConfig = GetLogicalJoyStickConfig(inputId);

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

            return (resultX, resultY);
        }

        // ReSharper disable once InconsistentNaming
        private (short, short) GetStickXY(StickInputId inputId) =>
            inputId switch
            {
                StickInputId.Left => (
                    SDL_GameControllerGetAxis(_gamepadHandle, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX),
                    SDL_GameControllerGetAxis(_gamepadHandle, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY)),
                StickInputId.Right => (
                    SDL_GameControllerGetAxis(_gamepadHandle, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX),
                    SDL_GameControllerGetAxis(_gamepadHandle, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY)),
                _ => throw new NotSupportedException($"Unsupported stick {inputId}")
            };

        public bool IsPressed(GamepadButtonInputId inputId)
        {
            switch (inputId)
            {
                case GamepadButtonInputId.LeftTrigger:
                    return ConvertRawStickValue(SDL_GameControllerGetAxis(_gamepadHandle, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT)) > _triggerThreshold;
                case GamepadButtonInputId.RightTrigger:
                    return ConvertRawStickValue(SDL_GameControllerGetAxis(_gamepadHandle, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT)) > _triggerThreshold;
            }

            if (_buttonsDriverMapping[(int)inputId] == SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_INVALID)
            {
                return false;
            }

            return SDL_GameControllerGetButton(_gamepadHandle, _buttonsDriverMapping[(int)inputId]) == 1;
        }
    }
}
