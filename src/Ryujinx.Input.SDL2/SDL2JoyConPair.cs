using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Common.Configuration.Hid.Controller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static SDL2.SDL;

namespace Ryujinx.Input.SDL2
{
    internal class SDL2JoyConPair(IGamepad left, IGamepad right) : IGamepad
    {
        private StandardControllerInputConfig _configuration;

        private readonly StickInputId[] _stickUserMapping =
        [
            StickInputId.Unbound,
            StickInputId.Left,
            StickInputId.Right
        ];

        public GamepadFeaturesFlag Features => (left?.Features ?? GamepadFeaturesFlag.None) |
                                               (right?.Features ?? GamepadFeaturesFlag.None);

        public const string Id = "JoyConPair";
        string IGamepad.Id => Id;

        public string Name => "* Nintendo Switch Joy-Con (L/R)";
        public bool IsConnected => left is { IsConnected: true } && right is { IsConnected: true };

        public void Dispose()
        {
            left?.Dispose();
            right?.Dispose();
        }

        public GamepadStateSnapshot GetMappedStateSnapshot()
        {
            return GetStateSnapshot();
        }

        public Vector3 GetMotionData(MotionInputId inputId)
        {
            return inputId switch
            {
                MotionInputId.Accelerometer or
                    MotionInputId.Gyroscope => left.GetMotionData(inputId),
                MotionInputId.SecondAccelerometer => right.GetMotionData(MotionInputId.Accelerometer),
                MotionInputId.SecondGyroscope => right.GetMotionData(MotionInputId.Gyroscope),
                _ => Vector3.Zero
            };
        }

        public GamepadStateSnapshot GetStateSnapshot()
        {
            return IGamepad.GetStateSnapshot(this);
        }

        public (float, float) GetStick(StickInputId inputId)
        {
            return inputId switch
            {
                StickInputId.Left => left.GetStick(StickInputId.Left),
                StickInputId.Right => right.GetStick(StickInputId.Right),
                _ => (0, 0)
            };
        }

        public bool IsPressed(GamepadButtonInputId inputId)
        {
            return left.IsPressed(inputId) || right.IsPressed(inputId);
        }

        public void Rumble(float lowFrequency, float highFrequency, uint durationMs)
        {
            if (lowFrequency != 0)
            {
                right.Rumble(lowFrequency, lowFrequency, durationMs);
            }

            if (highFrequency != 0)
            {
                left.Rumble(highFrequency, highFrequency, durationMs);
            }

            if (lowFrequency == 0 && highFrequency == 0)
            {
                left.Rumble(0, 0, durationMs);
                right.Rumble(0, 0, durationMs);
            }
        }

        public void SetConfiguration(InputConfig configuration)
        {
            left.SetConfiguration(configuration);
            right.SetConfiguration(configuration);
        }

        public void SetLed(uint packedRgb)
        {
        }

        public void SetTriggerThreshold(float triggerThreshold)
        {
            left.SetTriggerThreshold(triggerThreshold);
            right.SetTriggerThreshold(triggerThreshold);
        }

        public static bool IsCombinable(List<string> gamepadsIds)
        {
            (int leftIndex, int rightIndex) = DetectJoyConPair(gamepadsIds);
            return leftIndex >= 0 && rightIndex >= 0;
        }

        private static (int leftIndex, int rightIndex) DetectJoyConPair(List<string> gamepadsIds)
        {
            var gamepadNames = gamepadsIds.Where(gamepadId => gamepadId != Id)
                .Select((_, index) => SDL_GameControllerNameForIndex(index)).ToList();
            int leftIndex = gamepadNames.IndexOf(SDL2JoyCon.LeftName);
            int rightIndex = gamepadNames.IndexOf(SDL2JoyCon.RightName);

            return (leftIndex, rightIndex);
        }

        public static IGamepad GetGamepad(List<string> gamepadsIds)
        {
            (int leftIndex, int rightIndex) = DetectJoyConPair(gamepadsIds);
            if (leftIndex == -1 || rightIndex == -1)
            {
                return null;
            }

            nint leftGamepadHandle = SDL_GameControllerOpen(leftIndex);
            nint rightGamepadHandle = SDL_GameControllerOpen(rightIndex);

            if (leftGamepadHandle == nint.Zero || rightGamepadHandle == nint.Zero)
            {
                return null;
            }


            return new SDL2JoyConPair(new SDL2JoyCon(leftGamepadHandle, gamepadsIds[leftIndex]),
                new SDL2JoyCon(rightGamepadHandle, gamepadsIds[rightIndex]));
        }
    }
}
