using Ryujinx.Graphics.Device;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Ryujinx.Graphics.Gpu.Engine
{
    /// <summary>
    /// State interface with a shadow memory control register.
    /// </summary>
    interface IShadowState
    {
        /// <summary>
        /// MME shadow ram control mode.
        /// </summary>
        SetMmeShadowRamControlMode SetMmeShadowRamControlMode { get; }
    }

    /// <summary>
    /// Represents a device's state, with a additional shadow state.
    /// </summary>
    /// <typeparam name="TState">Type of the state</typeparam>
    class DeviceStateWithShadow<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TState> : IDeviceState where TState : unmanaged, IShadowState
    {
        private readonly DeviceState<TState> _state;
        private readonly DeviceState<TState> _shadowState;

        /// <summary>
        /// Current device state.
        /// </summary>
        public ref TState State => ref _state.State;

        /// <summary>
        /// Current shadow state.
        /// </summary>
        public ref TState ShadowState => ref _shadowState.State;

        /// <summary>
        /// Creates a new instance of the device state, with shadow state.
        /// </summary>
        /// <param name="callbacks">Optional that will be called if a register specified by name is read or written</param>
        /// <param name="debugLogCallback">Optional callback to be used for debug log messages</param>
        public DeviceStateWithShadow(IReadOnlyDictionary<string, RwCallback> callbacks = null, Action<string> debugLogCallback = null)
        {
            _state = new DeviceState<TState>(callbacks, debugLogCallback);
            _shadowState = new DeviceState<TState>();
        }

        /// <summary>
        /// Reads a value from a register.
        /// </summary>
        /// <param name="offset">Register offset in bytes</param>
        /// <returns>Value stored on the register</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Read(int offset)
        {
            return _state.Read(offset);
        }

        /// <summary>
        /// Writes a value to a register.
        /// </summary>
        /// <param name="offset">Register offset in bytes</param>
        /// <param name="value">Value to be written</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(int offset, int value)
        {
            WriteWithRedundancyCheck(offset, value, out _);
        }

        /// <summary>
        /// Writes a value to a register, returning a value indicating if <paramref name="value"/>
        /// is different from the current value on the register.
        /// </summary>
        /// <param name="offset">Register offset in bytes</param>
        /// <param name="value">Value to be written</param>
        /// <param name="changed">True if the value was changed, false otherwise</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteWithRedundancyCheck(int offset, int value, out bool changed)
        {
            SetMmeShadowRamControlMode shadowRamControl = _state.State.SetMmeShadowRamControlMode;
            if (shadowRamControl == SetMmeShadowRamControlMode.MethodPassthrough || offset < 0x200)
            {
                _state.WriteWithRedundancyCheck(offset, value, out changed);
            }
            else if (shadowRamControl == SetMmeShadowRamControlMode.MethodTrack ||
                     shadowRamControl == SetMmeShadowRamControlMode.MethodTrackWithFilter)
            {
                _shadowState.Write(offset, value);
                _state.WriteWithRedundancyCheck(offset, value, out changed);
            }
            else /* if (shadowRamControl == SetMmeShadowRamControlMode.MethodReplay) */
            {
                Debug.Assert(shadowRamControl == SetMmeShadowRamControlMode.MethodReplay);
                _state.WriteWithRedundancyCheck(offset, _shadowState.Read(offset), out changed);
            }
        }
    }
}
