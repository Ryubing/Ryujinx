using Ryujinx.Common.Memory;
using System.Runtime.CompilerServices;

namespace Ryujinx.Input
{
    /// <summary>
    /// A snapshot of a <see cref="IGamepad"/>.
    /// </summary>
    public struct GamepadStateSnapshot
    {
        // NOTE: Update Array size if JoystickInputId is changed.
        private Array3<Array2<float>> _joysticksState;
        // NOTE: Update Array size if GamepadInputId is changed.
        private Array28<bool> _buttonsState;

        /// <summary>
        /// Create a new instance of <see cref="GamepadStateSnapshot"/>.
        /// </summary>
        /// <param name="joysticksState">The joysticks state</param>
        /// <param name="buttonsState">The buttons state</param>
        public GamepadStateSnapshot(Array3<Array2<float>> joysticksState, Array28<bool> buttonsState)
        {
            _joysticksState = joysticksState;
            _buttonsState = buttonsState;
        }

        /// <summary>
        /// Check if a given input button is pressed.
        /// </summary>
        /// <param name="inputId">The button id</param>
        /// <returns>True if the given button is pressed</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPressed(GamepadButtonInputId inputId) => _buttonsState[(int)inputId];


        /// <summary>
        /// Set the state of a given button.
        /// </summary>
        /// <param name="inputId">The button id</param>
        /// <param name="value">The state to assign for the given button.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetPressed(GamepadButtonInputId inputId, bool value) => _buttonsState[(int)inputId] = value;

        /// <summary>
        /// Get the values of a given input joystick.
        /// </summary>
        /// <param name="inputId">The stick id</param>
        /// <returns>The values of the given input joystick</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (float, float) GetStick(StickInputId inputId)
        {
            Array2<float> result = _joysticksState[(int)inputId];

            return (result[0], result[1]);
        }

        /// <summary>
        /// Set the values of a given input joystick.
        /// </summary>
        /// <param name="inputId">The stick id</param>
        /// <param name="x">The x axis value</param>
        /// <param name="y">The y axis value</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStick(StickInputId inputId, float x, float y)
        {
            _joysticksState[(int)inputId][0] = x;
            _joysticksState[(int)inputId][1] = y;
        }
    }
}
