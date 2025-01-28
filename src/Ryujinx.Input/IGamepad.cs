using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Common.Memory;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Ryujinx.Input
{
    /// <summary>
    /// Represent an emulated gamepad.
    /// </summary>
    public interface IGamepad : IDisposable
    {
        /// <summary>
        /// Features supported by the gamepad.
        /// </summary>
        GamepadFeaturesFlag Features { get; }

        /// <summary>
        /// Unique Id of the gamepad.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// The name of the gamepad.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// True if the gamepad is connected.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Check if a given input button is pressed on the gamepad.
        /// </summary>
        /// <param name="inputId">The button id</param>
        /// <returns>True if the given button is pressed on the gamepad</returns>
        bool IsPressed(GamepadButtonInputId inputId);

        /// <summary>
        /// Get the values of a given input joystick on the gamepad.
        /// </summary>
        /// <param name="inputId">The stick id</param>
        /// <returns>The values of the given input joystick on the gamepad</returns>
        (float, float) GetStick(StickInputId inputId);

        /// <summary>
        /// Get the values of a given motion sensors on the gamepad.
        /// </summary>
        /// <param name="inputId">The motion id</param>
        /// <returns> The values of the given motion sensors on the gamepad.</returns>
        Vector3 GetMotionData(MotionInputId inputId);

        /// <summary>
        /// Configure the threshold of the triggers on the gamepad.
        /// </summary>
        /// <param name="triggerThreshold">The threshold value for the triggers on the gamepad</param>
        void SetTriggerThreshold(float triggerThreshold);

        /// <summary>
        /// Set the configuration of the gamepad.
        /// </summary>
        /// <remarks>This expect config to be in the format expected by the driver</remarks>
        /// <param name="configuration">The configuration of the gamepad</param>
        void SetConfiguration(InputConfig configuration);

        /// <summary>
        /// Set the LED on the gamepad to a given color.
        /// </summary>
        /// <remarks>Does nothing on a controller without LED functionality.</remarks>
        /// <param name="packedRgb">The packed RGB integer.</param>
        void SetLed(uint packedRgb);

        public void ClearLed() => SetLed(0);

        /// <summary>
        /// Starts a rumble effect on the gamepad.
        /// </summary>
        /// <param name="lowFrequency">The intensity of the low frequency from 0.0f to 1.0f</param>
        /// <param name="highFrequency">The intensity of the high frequency from 0.0f to 1.0f</param>
        /// <param name="durationMs">The duration of the rumble effect in milliseconds.</param>
        void Rumble(float lowFrequency, float highFrequency, uint durationMs);

        /// <summary>
        /// Get a snaphost of the state of the gamepad that is remapped with the informations from the <see cref="InputConfig"/> set via <see cref="SetConfiguration(InputConfig)"/>.
        /// </summary>
        /// <returns>A remapped snaphost of the state of the gamepad.</returns>
        GamepadStateSnapshot GetMappedStateSnapshot();

        /// <summary>
        /// Get a snaphost of the state of the gamepad.
        /// </summary>
        /// <returns>A snaphost of the state of the gamepad.</returns>
        GamepadStateSnapshot GetStateSnapshot();

        /// <summary>
        /// Get a snaphost of the state of a gamepad.
        /// </summary>
        /// <param name="gamepad">The gamepad to do a snapshot of</param>
        /// <returns>A snaphost of the state of the gamepad.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static GamepadStateSnapshot GetStateSnapshot(IGamepad gamepad)
        {
            // NOTE: Update Array size if JoystickInputId is changed.
            Array3<Array2<float>> joysticksState = default;

            for (StickInputId inputId = StickInputId.Left; inputId < StickInputId.Count; inputId++)
            {
                (float state0, float state1) = gamepad.GetStick(inputId);

                Array2<float> state = default;

                state[0] = state0;
                state[1] = state1;

                joysticksState[(int)inputId] = state;
            }

            // NOTE: Update Array size if GamepadInputId is changed.
            Array28<bool> buttonsState = default;

            for (GamepadButtonInputId inputId = GamepadButtonInputId.A; inputId < GamepadButtonInputId.Count; inputId++)
            {
                buttonsState[(int)inputId] = gamepad.IsPressed(inputId);
            }

            return new GamepadStateSnapshot(joysticksState, buttonsState);
        }
    }
}
