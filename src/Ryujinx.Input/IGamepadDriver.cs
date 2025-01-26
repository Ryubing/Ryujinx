using System;
using System.Collections.Generic;

namespace Ryujinx.Input
{
    /// <summary>
    /// Represent an emulated gamepad driver used to provide input in the emulator.
    /// </summary>
    public interface IGamepadDriver : IDisposable
    {
        /// <summary>
        /// The name of the driver
        /// </summary>
        string DriverName { get; }

        /// <summary>
        /// The unique ids of the gamepads connected.
        /// </summary>
        ReadOnlySpan<string> GamepadsIds { get; }

        /// <summary>
        /// Event triggered when a gamepad is connected.
        /// </summary>
        event Action<string> OnGamepadConnected;

        /// <summary>
        /// Event triggered when a gamepad is disconnected.
        /// </summary>
        event Action<string> OnGamepadDisconnected;

        /// <summary>
        /// Open a gampad by its unique id.
        /// </summary>
        /// <param name="id">The unique id of the gamepad</param>
        /// <returns>An instance of <see cref="IGamepad"/> associated to the gamepad id given or null if not found</returns>
        IGamepad GetGamepad(string id);
        
        /// <summary>
        /// Returns an <see cref="IEnumerable{T}"/> of the connected gamepads.
        /// </summary>
        IEnumerable<IGamepad> GetGamepads();

        /// <summary>
        /// Clear the internal state of the driver.
        /// </summary>
        /// <remarks>Does nothing by default.</remarks>
        void Clear() { }
    }
}
