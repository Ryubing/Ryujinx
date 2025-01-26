using Ryujinx.Graphics.Gpu.Memory;
using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Gpu.Engine.Threed
{
    /// <summary>
    /// Constant buffer updater.
    /// </summary>
    class ConstantBufferUpdater
    {
        private const int UniformDataCacheSize = 512;

        private readonly GpuChannel _channel;
        private readonly DeviceStateWithShadow<ThreedClassState> _state;

        // State associated with direct uniform buffer updates.
        // This state is used to attempt to batch together consecutive updates.
        private ulong _ubBeginCpuAddress = 0;
        private ulong _ubFollowUpAddress = 0;
        private ulong _ubByteCount = 0;
        private int _ubIndex = 0;
        private readonly int[] _ubData = new int[UniformDataCacheSize];

        /// <summary>
        /// Creates a new instance of the constant buffer updater.
        /// </summary>
        /// <param name="channel">GPU channel</param>
        /// <param name="state">Channel state</param>
        public ConstantBufferUpdater(GpuChannel channel, DeviceStateWithShadow<ThreedClassState> state)
        {
            _channel = channel;
            _state = state;
        }

        /// <summary>
        /// Binds a uniform buffer for the vertex shader stage.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        public void BindVertex(int argument)
        {
            Bind(argument, ShaderType.Vertex);
        }

        /// <summary>
        /// Binds a uniform buffer for the tessellation control shader stage.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        public void BindTessControl(int argument)
        {
            Bind(argument, ShaderType.TessellationControl);
        }

        /// <summary>
        /// Binds a uniform buffer for the tessellation evaluation shader stage.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        public void BindTessEvaluation(int argument)
        {
            Bind(argument, ShaderType.TessellationEvaluation);
        }

        /// <summary>
        /// Binds a uniform buffer for the geometry shader stage.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        public void BindGeometry(int argument)
        {
            Bind(argument, ShaderType.Geometry);
        }

        /// <summary>
        /// Binds a uniform buffer for the fragment shader stage.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        public void BindFragment(int argument)
        {
            Bind(argument, ShaderType.Fragment);
        }

        /// <summary>
        /// Binds a uniform buffer for the specified shader stage.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        /// <param name="type">Shader stage that will access the uniform buffer</param>
        private void Bind(int argument, ShaderType type)
        {
            bool enable = (argument & 1) != 0;

            int index = (argument >> 4) & 0x1f;

            FlushUboDirty();

            if (enable)
            {
                UniformBufferState uniformBuffer = _state.State.UniformBufferState;

                ulong address = uniformBuffer.Address.Pack();

                _channel.BufferManager.SetGraphicsUniformBuffer((int)type, index, address, (uint)uniformBuffer.Size);
            }
            else
            {
                _channel.BufferManager.SetGraphicsUniformBuffer((int)type, index, 0, 0);
            }
        }

        /// <summary>
        /// Flushes any queued UBO updates.
        /// </summary>
        public void FlushUboDirty()
        {
            if (_ubFollowUpAddress != 0)
            {
                MemoryManager memoryManager = _channel.MemoryManager;

                Span<byte> data = MemoryMarshal.Cast<int, byte>(_ubData.AsSpan(0, (int)(_ubByteCount / 4)));

                if (memoryManager.Physical.WriteWithRedundancyCheck(_ubBeginCpuAddress, data))
                {
                    memoryManager.Physical.BufferCache.ForceDirty(memoryManager, _ubFollowUpAddress - _ubByteCount, _ubByteCount);
                }

                _ubFollowUpAddress = 0;
                _ubIndex = 0;
            }
        }

        /// <summary>
        /// Updates the uniform buffer data with inline data.
        /// </summary>
        /// <param name="argument">New uniform buffer data word</param>
        public void Update(int argument)
        {
            UniformBufferState uniformBuffer = _state.State.UniformBufferState;

            ulong address = uniformBuffer.Address.Pack() + (uint)uniformBuffer.Offset;

            if (_ubFollowUpAddress != address || _ubIndex == _ubData.Length)
            {
                FlushUboDirty();

                _ubByteCount = 0;
                _ubBeginCpuAddress = _channel.MemoryManager.Translate(address);
            }

            _ubData[_ubIndex++] = argument;

            _ubFollowUpAddress = address + 4;
            _ubByteCount += 4;

            _state.State.UniformBufferState.Offset += 4;
        }

        /// <summary>
        /// Updates the uniform buffer data with inline data.
        /// </summary>
        /// <param name="data">Data to be written to the uniform buffer</param>
        public void Update(ReadOnlySpan<int> data)
        {
            UniformBufferState uniformBuffer = _state.State.UniformBufferState;

            ulong address = uniformBuffer.Address.Pack() + (uint)uniformBuffer.Offset;

            ulong size = (ulong)data.Length * 4;

            if (_ubFollowUpAddress != address || _ubIndex + data.Length > _ubData.Length)
            {
                FlushUboDirty();

                _ubByteCount = 0;
                _ubBeginCpuAddress = _channel.MemoryManager.Translate(address);
            }

            data.CopyTo(_ubData.AsSpan(_ubIndex));
            _ubIndex += data.Length;

            _ubFollowUpAddress = address + size;
            _ubByteCount += size;

            _state.State.UniformBufferState.Offset += data.Length * 4;
        }
    }
}
