using Ryujinx.Graphics.Device;
using Ryujinx.Graphics.Gpu.Engine.InlineToMemory;
using Ryujinx.Graphics.Gpu.Engine.Threed;
using Ryujinx.Graphics.Gpu.Engine.Types;
using Ryujinx.Graphics.Gpu.Memory;
using Ryujinx.Graphics.Gpu.Shader;
using Ryujinx.Graphics.Shader;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Ryujinx.Graphics.Gpu.Engine.Compute
{
    /// <summary>
    /// Represents a compute engine class.
    /// </summary>
    class ComputeClass : IDeviceState
    {
        private readonly GpuContext _context;
        private readonly GpuChannel _channel;
        private readonly ThreedClass _3dEngine;
        private readonly DeviceState<ComputeClassState> _state;

        private readonly InlineToMemoryClass _i2mClass;

        /// <summary>
        /// Creates a new instance of the compute engine class.
        /// </summary>
        /// <param name="context">GPU context</param>
        /// <param name="channel">GPU channel</param>
        /// <param name="threedEngine">3D engine</param>
        public ComputeClass(GpuContext context, GpuChannel channel, ThreedClass threedEngine)
        {
            _context = context;
            _channel = channel;
            _3dEngine = threedEngine;
            _state = new DeviceState<ComputeClassState>(new Dictionary<string, RwCallback>
            {
                { nameof(ComputeClassState.LaunchDma), new RwCallback(LaunchDma, null) },
                { nameof(ComputeClassState.LoadInlineData), new RwCallback(LoadInlineData, null) },
                { nameof(ComputeClassState.SendSignalingPcasB), new RwCallback(SendSignalingPcasB, null) },
            });

            _i2mClass = new InlineToMemoryClass(context, channel, initializeState: false);
        }

        /// <summary>
        /// Reads data from the class registers.
        /// </summary>
        /// <param name="offset">Register byte offset</param>
        /// <returns>Data at the specified offset</returns>
        public int Read(int offset) => _state.Read(offset);

        /// <summary>
        /// Writes data to the class registers.
        /// </summary>
        /// <param name="offset">Register byte offset</param>
        /// <param name="data">Data to be written</param>
        public void Write(int offset, int data) => _state.Write(offset, data);

        /// <summary>
        /// Launches the Inline-to-Memory DMA copy operation.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        private void LaunchDma(int argument)
        {
            _i2mClass.LaunchDma(ref Unsafe.As<ComputeClassState, InlineToMemoryClassState>(ref _state.State), argument);
        }

        /// <summary>
        /// Pushes a block of data to the Inline-to-Memory engine.
        /// </summary>
        /// <param name="data">Data to push</param>
        public void LoadInlineData(ReadOnlySpan<int> data)
        {
            _i2mClass.LoadInlineData(data);
        }

        /// <summary>
        /// Pushes a word of data to the Inline-to-Memory engine.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        private void LoadInlineData(int argument)
        {
            _i2mClass.LoadInlineData(argument);
        }

        /// <summary>
        /// Performs the compute dispatch operation.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        private void SendSignalingPcasB(int argument)
        {
            MemoryManager memoryManager = _channel.MemoryManager;

            // Since we're going to change the state, make sure any pending instanced draws are done.
            _3dEngine.PerformDeferredDraws();

            // Make sure all pending uniform buffer data is written to memory.
            _3dEngine.FlushUboDirty();

            uint qmdAddress = _state.State.SendPcasA;

            ComputeQmd qmd = _channel.MemoryManager.Read<ComputeQmd>((ulong)qmdAddress << 8);

            ulong shaderGpuVa = ((ulong)_state.State.SetProgramRegionAAddressUpper << 32) | _state.State.SetProgramRegionB;

            shaderGpuVa += (uint)qmd.ProgramOffset;

            int localMemorySize = qmd.ShaderLocalMemoryLowSize + qmd.ShaderLocalMemoryHighSize;

            int sharedMemorySize = Math.Min(qmd.SharedMemorySize, _context.Capabilities.MaximumComputeSharedMemorySize);

            for (int index = 0; index < Constants.TotalCpUniformBuffers; index++)
            {
                if (!qmd.ConstantBufferValid(index))
                {
                    continue;
                }

                ulong gpuVa = (uint)qmd.ConstantBufferAddrLower(index) | (ulong)qmd.ConstantBufferAddrUpper(index) << 32;
                ulong size = (ulong)qmd.ConstantBufferSize(index);

                _channel.BufferManager.SetComputeUniformBuffer(index, gpuVa, size);
            }

            ulong samplerPoolGpuVa = ((ulong)_state.State.SetTexSamplerPoolAOffsetUpper << 32) | _state.State.SetTexSamplerPoolB;
            ulong texturePoolGpuVa = ((ulong)_state.State.SetTexHeaderPoolAOffsetUpper << 32) | _state.State.SetTexHeaderPoolB;

            int samplerPoolMaximumId = _state.State.SetTexSamplerPoolCMaximumIndex;

            GpuChannelPoolState poolState = new(
                texturePoolGpuVa,
                _state.State.SetTexHeaderPoolCMaximumIndex,
                _state.State.SetBindlessTextureConstantBufferSlotSelect);

            GpuChannelComputeState computeState = new(
                qmd.CtaThreadDimension0,
                qmd.CtaThreadDimension1,
                qmd.CtaThreadDimension2,
                localMemorySize,
                sharedMemorySize,
                _channel.BufferManager.HasUnalignedStorageBuffers);

            CachedShaderProgram cs = memoryManager.Physical.ShaderCache.GetComputeShader(_channel, samplerPoolMaximumId, poolState, computeState, shaderGpuVa);

            _context.Renderer.Pipeline.SetProgram(cs.HostProgram);

            _channel.TextureManager.SetComputeSamplerPool(samplerPoolGpuVa, _state.State.SetTexSamplerPoolCMaximumIndex, qmd.SamplerIndex);
            _channel.TextureManager.SetComputeTexturePool(texturePoolGpuVa, _state.State.SetTexHeaderPoolCMaximumIndex);
            _channel.TextureManager.SetComputeTextureBufferIndex(_state.State.SetBindlessTextureConstantBufferSlotSelect);

            ShaderProgramInfo info = cs.Shaders[0].Info;

            for (int index = 0; index < info.SBuffers.Count; index++)
            {
                BufferDescriptor sb = info.SBuffers[index];

                ulong sbDescAddress = _channel.BufferManager.GetComputeUniformBufferAddress(sb.SbCbSlot);
                sbDescAddress += (ulong)sb.SbCbOffset * 4;

                SbDescriptor sbDescriptor = _channel.MemoryManager.Physical.Read<SbDescriptor>(sbDescAddress);

                uint size;
                if (sb.SbCbSlot == Constants.DriverReservedUniformBuffer)
                {
                    // Only trust the SbDescriptor size if it comes from slot 0.
                    size = (uint)sbDescriptor.Size;
                }
                else
                {
                    // TODO: Use full mapped size and somehow speed up buffer sync.
                    size = (uint)_channel.MemoryManager.GetMappedSize(sbDescriptor.PackAddress(), Constants.MaxUnknownStorageSize);
                }

                _channel.BufferManager.SetComputeStorageBuffer(sb.Slot, sbDescriptor.PackAddress(), size, sb.Flags);
            }

            if (_channel.BufferManager.HasUnalignedStorageBuffers != computeState.HasUnalignedStorageBuffer)
            {
                // Refetch the shader, as assumptions about storage buffer alignment have changed.
                computeState = new GpuChannelComputeState(
                    qmd.CtaThreadDimension0,
                    qmd.CtaThreadDimension1,
                    qmd.CtaThreadDimension2,
                    localMemorySize,
                    sharedMemorySize,
                    _channel.BufferManager.HasUnalignedStorageBuffers);

                cs = memoryManager.Physical.ShaderCache.GetComputeShader(_channel, samplerPoolMaximumId, poolState, computeState, shaderGpuVa);

                _context.Renderer.Pipeline.SetProgram(cs.HostProgram);
            }

            _channel.BufferManager.SetComputeBufferBindings(cs.Bindings);

            _channel.TextureManager.SetComputeBindings(cs.Bindings);

            // Should never return false for mismatching spec state, since the shader was fetched above.
            _channel.TextureManager.CommitComputeBindings(cs.SpecializationState);

            _channel.BufferManager.CommitComputeBindings();

            _context.Renderer.Pipeline.DispatchCompute(qmd.CtaRasterWidth, qmd.CtaRasterHeight, qmd.CtaRasterDepth);

            _3dEngine.ForceShaderUpdate();
        }
    }
}
