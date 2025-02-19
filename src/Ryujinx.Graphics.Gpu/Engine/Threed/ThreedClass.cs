using Ryujinx.Common.Memory;
using Ryujinx.Graphics.Device;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Gpu.Engine.GPFifo;
using Ryujinx.Graphics.Gpu.Engine.InlineToMemory;
using Ryujinx.Graphics.Gpu.Engine.Threed.Blender;
using Ryujinx.Graphics.Gpu.Engine.Types;
using Ryujinx.Graphics.Gpu.Synchronization;
using Ryujinx.Memory.Range;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Ryujinx.Graphics.Gpu.Engine.Threed
{
    /// <summary>
    /// Represents a 3D engine class.
    /// </summary>
    class ThreedClass : IDeviceState, IDisposable
    {
        private readonly GpuContext _context;
        private readonly GPFifoClass _fifoClass;
        private readonly DeviceStateWithShadow<ThreedClassState> _state;

        private readonly InlineToMemoryClass _i2mClass;
        private readonly AdvancedBlendManager _blendManager;
        private readonly DrawManager _drawManager;
        private readonly SemaphoreUpdater _semaphoreUpdater;
        private readonly ConstantBufferUpdater _cbUpdater;
        private readonly StateUpdater _stateUpdater;

        private SetMmeShadowRamControlMode ShadowMode => _state.State.SetMmeShadowRamControlMode;

        /// <summary>
        /// Creates a new instance of the 3D engine class.
        /// </summary>
        /// <param name="context">GPU context</param>
        /// <param name="channel">GPU channel</param>
        public ThreedClass(GpuContext context, GpuChannel channel, GPFifoClass fifoClass)
        {
            _context = context;
            _fifoClass = fifoClass;
            _state = new DeviceStateWithShadow<ThreedClassState>(new Dictionary<string, RwCallback>
            {
                { nameof(ThreedClassState.LaunchDma), new RwCallback(LaunchDma, null) },
                { nameof(ThreedClassState.LoadInlineData), new RwCallback(LoadInlineData, null) },
                { nameof(ThreedClassState.SyncpointAction), new RwCallback(IncrementSyncpoint, null) },
                { nameof(ThreedClassState.InvalidateSamplerCacheNoWfi), new RwCallback(InvalidateSamplerCacheNoWfi, null) },
                { nameof(ThreedClassState.InvalidateTextureHeaderCacheNoWfi), new RwCallback(InvalidateTextureHeaderCacheNoWfi, null) },
                { nameof(ThreedClassState.TextureBarrier), new RwCallback(TextureBarrier, null) },
                { nameof(ThreedClassState.LoadBlendUcodeStart), new RwCallback(LoadBlendUcodeStart, null) },
                { nameof(ThreedClassState.LoadBlendUcodeInstruction), new RwCallback(LoadBlendUcodeInstruction, null) },
                { nameof(ThreedClassState.TextureBarrierTiled), new RwCallback(TextureBarrierTiled, null) },
                { nameof(ThreedClassState.DrawTextureSrcY), new RwCallback(DrawTexture, null) },
                { nameof(ThreedClassState.DrawVertexArrayBeginEndInstanceFirst), new RwCallback(DrawVertexArrayBeginEndInstanceFirst, null) },
                { nameof(ThreedClassState.DrawVertexArrayBeginEndInstanceSubsequent), new RwCallback(DrawVertexArrayBeginEndInstanceSubsequent, null) },
                { nameof(ThreedClassState.VbElementU8), new RwCallback(VbElementU8, null) },
                { nameof(ThreedClassState.VbElementU16), new RwCallback(VbElementU16, null) },
                { nameof(ThreedClassState.VbElementU32), new RwCallback(VbElementU32, null) },
                { nameof(ThreedClassState.ResetCounter), new RwCallback(ResetCounter, null) },
                { nameof(ThreedClassState.RenderEnableCondition), new RwCallback(null, Zero) },
                { nameof(ThreedClassState.DrawEnd), new RwCallback(DrawEnd, null) },
                { nameof(ThreedClassState.DrawBegin), new RwCallback(DrawBegin, null) },
                { nameof(ThreedClassState.DrawIndexBuffer32BeginEndInstanceFirst), new RwCallback(DrawIndexBuffer32BeginEndInstanceFirst, null) },
                { nameof(ThreedClassState.DrawIndexBuffer16BeginEndInstanceFirst), new RwCallback(DrawIndexBuffer16BeginEndInstanceFirst, null) },
                { nameof(ThreedClassState.DrawIndexBuffer8BeginEndInstanceFirst), new RwCallback(DrawIndexBuffer8BeginEndInstanceFirst, null) },
                { nameof(ThreedClassState.DrawIndexBuffer32BeginEndInstanceSubsequent), new RwCallback(DrawIndexBuffer32BeginEndInstanceSubsequent, null) },
                { nameof(ThreedClassState.DrawIndexBuffer16BeginEndInstanceSubsequent), new RwCallback(DrawIndexBuffer16BeginEndInstanceSubsequent, null) },
                { nameof(ThreedClassState.DrawIndexBuffer8BeginEndInstanceSubsequent), new RwCallback(DrawIndexBuffer8BeginEndInstanceSubsequent, null) },
                { nameof(ThreedClassState.IndexBufferCount), new RwCallback(SetIndexBufferCount, null) },
                { nameof(ThreedClassState.Clear), new RwCallback(Clear, null) },
                { nameof(ThreedClassState.SemaphoreControl), new RwCallback(Report, null) },
                { nameof(ThreedClassState.SetFalcon04), new RwCallback(SetFalcon04, null) },
                { nameof(ThreedClassState.UniformBufferUpdateData), new RwCallback(ConstantBufferUpdate, null) },
                { nameof(ThreedClassState.UniformBufferBindVertex), new RwCallback(ConstantBufferBindVertex, null) },
                { nameof(ThreedClassState.UniformBufferBindTessControl), new RwCallback(ConstantBufferBindTessControl, null) },
                { nameof(ThreedClassState.UniformBufferBindTessEvaluation), new RwCallback(ConstantBufferBindTessEvaluation, null) },
                { nameof(ThreedClassState.UniformBufferBindGeometry), new RwCallback(ConstantBufferBindGeometry, null) },
                { nameof(ThreedClassState.UniformBufferBindFragment), new RwCallback(ConstantBufferBindFragment, null) },
            });

            _i2mClass = new InlineToMemoryClass(context, channel, initializeState: false);

            SpecializationStateUpdater spec = new(context);
            DrawState drawState = new();

            _drawManager = new DrawManager(context, channel, _state, drawState, spec);
            _blendManager = new AdvancedBlendManager(_state);
            _semaphoreUpdater = new SemaphoreUpdater(context, channel, _state);
            _cbUpdater = new ConstantBufferUpdater(channel, _state);
            _stateUpdater = new StateUpdater(context, channel, _state, drawState, _blendManager, spec);

            // This defaults to "always", even without any register write.
            // Reads just return 0, regardless of what was set there.
            _state.State.RenderEnableCondition = Condition.Always;
        }

        /// <summary>
        /// Reads data from the class registers.
        /// </summary>
        /// <param name="offset">Register byte offset</param>
        /// <returns>Data at the specified offset</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Read(int offset) => _state.Read(offset);

        /// <summary>
        /// Writes data to the class registers.
        /// </summary>
        /// <param name="offset">Register byte offset</param>
        /// <param name="data">Data to be written</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(int offset, int data)
        {
            _state.WriteWithRedundancyCheck(offset, data, out bool valueChanged);

            if (valueChanged)
            {
                _stateUpdater.SetDirty(offset);
            }
        }

        /// <summary>
        /// Sets the shadow ram control value of all sub-channels.
        /// </summary>
        /// <param name="control">New shadow ram control value</param>
        public void SetShadowRamControl(int control)
        {
            _state.State.SetMmeShadowRamControl = (uint)control;
        }

        /// <summary>
        /// Updates current host state for all registers modified since the last call to this method.
        /// </summary>
        public void UpdateState()
        {
            _fifoClass.CreatePendingSyncs();
            _cbUpdater.FlushUboDirty();
            _stateUpdater.Update();
        }

        /// <summary>
        /// Updates current host state for all registers modified since the last call to this method.
        /// </summary>
        /// <param name="mask">Mask where each bit set indicates that the respective state group index should be checked</param>
        public void UpdateState(ulong mask)
        {
            _stateUpdater.Update(mask);
        }

        /// <summary>
        /// Updates render targets (color and depth-stencil buffers) based on current render target state.
        /// </summary>
        /// <param name="updateFlags">Flags indicating which render targets should be updated and how</param>
        /// <param name="singleUse">If this is not -1, it indicates that only the given indexed target will be used.</param>
        public void UpdateRenderTargetState(RenderTargetUpdateFlags updateFlags, int singleUse = -1)
        {
            _stateUpdater.UpdateRenderTargetState(updateFlags, singleUse);
        }

        /// <summary>
        /// Updates scissor based on current render target state.
        /// </summary>
        public void UpdateScissorState()
        {
            _stateUpdater.UpdateScissorState();
        }

        /// <summary>
        /// Marks the entire state as dirty, forcing a full host state update before the next draw.
        /// </summary>
        public void ForceStateDirty()
        {
            _drawManager.ForceStateDirty();
            _stateUpdater.SetAllDirty();
        }

        /// <summary>
        /// Marks the specified register offset as dirty, forcing the associated state to update on the next draw.
        /// </summary>
        /// <param name="offset">Register offset</param>
        public void ForceStateDirty(int offset)
        {
            _stateUpdater.SetDirty(offset);
        }

        /// <summary>
        /// Marks the specified register range for a group index as dirty, forcing the associated state to update on the next draw.
        /// </summary>
        /// <param name="groupIndex">Index of the group to dirty</param>
        public void ForceStateDirtyByIndex(int groupIndex)
        {
            _stateUpdater.ForceDirty(groupIndex);
        }

        /// <summary>
        /// Forces the shaders to be rebound on the next draw.
        /// </summary>
        public void ForceShaderUpdate()
        {
            _stateUpdater.ForceShaderUpdate();
        }

        /// <summary>
        /// Create any syncs from WaitForIdle command that are currently pending.
        /// </summary>
        public void CreatePendingSyncs()
        {
            _fifoClass.CreatePendingSyncs();
        }

        /// <summary>
        /// Flushes any queued UBO updates.
        /// </summary>
        public void FlushUboDirty()
        {
            _cbUpdater.FlushUboDirty();
        }

        /// <summary>
        /// Perform any deferred draws.
        /// </summary>
        public void PerformDeferredDraws()
        {
            _drawManager.PerformDeferredDraws(this);
        }

        /// <summary>
        /// Updates the currently bound constant buffer.
        /// </summary>
        /// <param name="data">Data to be written to the buffer</param>
        public void ConstantBufferUpdate(ReadOnlySpan<int> data)
        {
            _cbUpdater.Update(data);
        }

        /// <summary>
        /// Test if two 32 byte structs are equal. 
        /// </summary>
        /// <typeparam name="T">Type of the 32-byte struct</typeparam>
        /// <param name="lhs">First struct</param>
        /// <param name="rhs">Second struct</param>
        /// <returns>True if equal, false otherwise</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool UnsafeEquals32Byte<T>(ref T lhs, ref T rhs) where T : unmanaged
        {
            if (Vector256.IsHardwareAccelerated)
            {
                return Vector256.EqualsAll(
                    Unsafe.As<T, Vector256<uint>>(ref lhs),
                    Unsafe.As<T, Vector256<uint>>(ref rhs)
                );
            }
            else
            {
                ref Vector128<uint> lhsVec = ref Unsafe.As<T, Vector128<uint>>(ref lhs);
                ref Vector128<uint> rhsVec = ref Unsafe.As<T, Vector128<uint>>(ref rhs);

                return Vector128.EqualsAll(lhsVec, rhsVec) &&
                    Vector128.EqualsAll(Unsafe.Add(ref lhsVec, 1), Unsafe.Add(ref rhsVec, 1));
            }
        }

        /// <summary>
        /// Updates blend enable. Respects current shadow mode.
        /// </summary>
        /// <param name="masks">Blend enable</param>
        public void UpdateBlendEnable(ref Array8<Boolean32> enable)
        {
            SetMmeShadowRamControlMode shadow = ShadowMode;
            ref Array8<Boolean32> state = ref _state.State.BlendEnable;

            if (shadow.IsReplay())
            {
                enable = _state.ShadowState.BlendEnable;
            }

            if (!UnsafeEquals32Byte(ref enable, ref state))
            {
                state = enable;

                _stateUpdater.ForceDirty(StateUpdater.BlendStateIndex);
            }

            if (shadow.IsTrack())
            {
                _state.ShadowState.BlendEnable = enable;
            }
        }

        /// <summary>
        /// Updates color masks. Respects current shadow mode.
        /// </summary>
        /// <param name="masks">Color masks</param>
        public void UpdateColorMasks(ref Array8<RtColorMask> masks)
        {
            SetMmeShadowRamControlMode shadow = ShadowMode;
            ref Array8<RtColorMask> state = ref _state.State.RtColorMask;

            if (shadow.IsReplay())
            {
                masks = _state.ShadowState.RtColorMask;
            }

            if (!UnsafeEquals32Byte(ref masks, ref state))
            {
                state = masks;

                _stateUpdater.ForceDirty(StateUpdater.RtColorMaskIndex);
            }

            if (shadow.IsTrack())
            {
                _state.ShadowState.RtColorMask = masks;
            }
        }

        /// <summary>
        /// Updates index buffer state for an indexed draw. Respects current shadow mode.
        /// </summary>
        /// <param name="addrHigh">High part of the address</param>
        /// <param name="addrLow">Low part of the address</param>
        /// <param name="type">Type of the binding</param>
        public void UpdateIndexBuffer(uint addrHigh, uint addrLow, IndexType type)
        {
            SetMmeShadowRamControlMode shadow = ShadowMode;
            ref IndexBufferState state = ref _state.State.IndexBufferState;

            if (shadow.IsReplay())
            {
                ref IndexBufferState shadowState = ref _state.ShadowState.IndexBufferState;
                addrHigh = shadowState.Address.High;
                addrLow = shadowState.Address.Low;
                type = shadowState.Type;
            }

            if (state.Address.High != addrHigh || state.Address.Low != addrLow || state.Type != type)
            {
                state.Address.High = addrHigh;
                state.Address.Low = addrLow;
                state.Type = type;

                _stateUpdater.ForceDirty(StateUpdater.IndexBufferStateIndex);
            }

            if (shadow.IsTrack())
            {
                ref IndexBufferState shadowState = ref _state.ShadowState.IndexBufferState;
                shadowState.Address.High = addrHigh;
                shadowState.Address.Low = addrLow;
                shadowState.Type = type;
            }
        }

        /// <summary>
        /// Updates uniform buffer state for update or bind. Respects current shadow mode.
        /// </summary>
        /// <param name="size">Size of the binding</param>
        /// <param name="addrHigh">High part of the addrsss</param>
        /// <param name="addrLow">Low part of the address</param>
        public void UpdateUniformBufferState(int size, uint addrHigh, uint addrLow)
        {
            SetMmeShadowRamControlMode shadow = ShadowMode;
            ref UniformBufferState state = ref _state.State.UniformBufferState;

            if (shadow.IsReplay())
            {
                ref UniformBufferState shadowState = ref _state.ShadowState.UniformBufferState;
                size = shadowState.Size;
                addrHigh = shadowState.Address.High;
                addrLow = shadowState.Address.Low;
            }

            state.Size = size;
            state.Address.High = addrHigh;
            state.Address.Low = addrLow;

            if (shadow.IsTrack())
            {
                ref UniformBufferState shadowState = ref _state.ShadowState.UniformBufferState;
                shadowState.Size = size;
                shadowState.Address.High = addrHigh;
                shadowState.Address.Low = addrLow;
            }
        }

        /// <summary>
        /// Updates a shader offset. Respects current shadow mode.
        /// </summary>
        /// <param name="index">Index of the shader to update</param>
        /// <param name="offset">Offset to update with</param>
        public void SetShaderOffset(int index, uint offset)
        {
            SetMmeShadowRamControlMode shadow = ShadowMode;
            ref ShaderState shaderState = ref _state.State.ShaderState[index];

            if (shadow.IsReplay())
            {
                offset = _state.ShadowState.ShaderState[index].Offset;
            }

            if (shaderState.Offset != offset)
            {
                shaderState.Offset = offset;

                _stateUpdater.ForceDirty(StateUpdater.ShaderStateIndex);
            }

            if (shadow.IsTrack())
            {
                _state.ShadowState.ShaderState[index].Offset = offset;
            }
        }

        /// <summary>
        /// Updates uniform buffer state for update. Respects current shadow mode.
        /// </summary>
        /// <param name="ubState">Uniform buffer state</param>
        public void UpdateUniformBufferState(UniformBufferState ubState)
        {
            SetMmeShadowRamControlMode shadow = ShadowMode;
            ref UniformBufferState state = ref _state.State.UniformBufferState;

            if (shadow.IsReplay())
            {
                ubState = _state.ShadowState.UniformBufferState;
            }

            state = ubState;

            if (shadow.IsTrack())
            {
                _state.ShadowState.UniformBufferState = ubState;
            }
        }

        /// <summary>
        /// Launches the Inline-to-Memory DMA copy operation.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        private void LaunchDma(int argument)
        {
            _i2mClass.LaunchDma(ref Unsafe.As<ThreedClassState, InlineToMemoryClassState>(ref _state.State), argument);
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
        /// Performs an incrementation on a syncpoint.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        public void IncrementSyncpoint(int argument)
        {
            uint syncpointId = (uint)argument & 0xFFFF;

            _context.AdvanceSequence();
            _context.CreateHostSyncIfNeeded(HostSyncFlags.StrictSyncpoint);
            _context.Renderer.UpdateCounters(); // Poll the query counters, the game may want an updated result.
            _context.Synchronization.IncrementSyncpoint(syncpointId);
        }

        /// <summary>
        /// Invalidates the cache with the sampler descriptors from the sampler pool.
        /// </summary>
        /// <param name="argument">Method call argument (unused)</param>
        private void InvalidateSamplerCacheNoWfi(int argument)
        {
            _context.AdvanceSequence();
        }

        /// <summary>
        /// Invalidates the cache with the texture descriptors from the texture pool.
        /// </summary>
        /// <param name="argument">Method call argument (unused)</param>
        private void InvalidateTextureHeaderCacheNoWfi(int argument)
        {
            _context.AdvanceSequence();
        }

        /// <summary>
        /// Issues a texture barrier.
        /// This waits until previous texture writes from the GPU to finish, before
        /// performing new operations with said textures.
        /// </summary>
        /// <param name="argument">Method call argument (unused)</param>
        private void TextureBarrier(int argument)
        {
            _context.Renderer.Pipeline.TextureBarrier();
        }

        /// <summary>
        /// Sets the start offset of the blend microcode in memory.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        private void LoadBlendUcodeStart(int argument)
        {
            _blendManager.LoadBlendUcodeStart(argument);
        }

        /// <summary>
        /// Pushes one word of blend microcode.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        private void LoadBlendUcodeInstruction(int argument)
        {
            _blendManager.LoadBlendUcodeInstruction(argument);
        }

        /// <summary>
        /// Issues a texture barrier.
        /// This waits until previous texture writes from the GPU to finish, before
        /// performing new operations with said textures.
        /// This performs a per-tile wait, it is only valid if both the previous write
        /// and current access has the same access patterns.
        /// This may be faster than the regular barrier on tile-based rasterizers.
        /// </summary>
        /// <param name="argument">Method call argument (unused)</param>
        private void TextureBarrierTiled(int argument)
        {
            _context.Renderer.Pipeline.TextureBarrierTiled();
        }

        /// <summary>
        /// Draws a texture, without needing to specify shader programs.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        private void DrawTexture(int argument)
        {
            _drawManager.DrawTexture(this, argument);
        }

        /// <summary>
        /// Performs a non-indexed draw with the specified topology, index and count.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        private void DrawVertexArrayBeginEndInstanceFirst(int argument)
        {
            _drawManager.DrawVertexArrayBeginEndInstanceFirst(this, argument);
        }

        /// <summary>
        /// Performs a non-indexed draw with the specified topology, index and count,
        /// while incrementing the current instance.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        private void DrawVertexArrayBeginEndInstanceSubsequent(int argument)
        {
            _drawManager.DrawVertexArrayBeginEndInstanceSubsequent(this, argument);
        }

        /// <summary>
        /// Pushes four 8-bit index buffer elements.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        private void VbElementU8(int argument)
        {
            _drawManager.VbElementU8(argument);
        }

        /// <summary>
        /// Pushes two 16-bit index buffer elements.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        private void VbElementU16(int argument)
        {
            _drawManager.VbElementU16(argument);
        }

        /// <summary>
        /// Pushes one 32-bit index buffer element.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        private void VbElementU32(int argument)
        {
            _drawManager.VbElementU32(argument);
        }

        /// <summary>
        /// Resets the value of an internal GPU counter back to zero.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        private void ResetCounter(int argument)
        {
            _semaphoreUpdater.ResetCounter(argument);
        }

        /// <summary>
        /// Finishes the draw call.
        /// This draws geometry on the bound buffers based on the current GPU state.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        private void DrawEnd(int argument)
        {
            _drawManager.DrawEnd(this, argument);
        }

        /// <summary>
        /// Starts draw.
        /// This sets primitive type and instanced draw parameters.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        private void DrawBegin(int argument)
        {
            _drawManager.DrawBegin(this, argument);
        }

        /// <summary>
        /// Sets the index buffer count.
        /// This also sets internal state that indicates that the next draw is an indexed draw.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        private void SetIndexBufferCount(int argument)
        {
            _drawManager.SetIndexBufferCount(argument);
        }

        /// <summary>
        /// Performs a indexed draw with 8-bit index buffer elements.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        private void DrawIndexBuffer8BeginEndInstanceFirst(int argument)
        {
            _drawManager.DrawIndexBuffer8BeginEndInstanceFirst(this, argument);
        }

        /// <summary>
        /// Performs a indexed draw with 16-bit index buffer elements.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        private void DrawIndexBuffer16BeginEndInstanceFirst(int argument)
        {
            _drawManager.DrawIndexBuffer16BeginEndInstanceFirst(this, argument);
        }

        /// <summary>
        /// Performs a indexed draw with 32-bit index buffer elements.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        private void DrawIndexBuffer32BeginEndInstanceFirst(int argument)
        {
            _drawManager.DrawIndexBuffer32BeginEndInstanceFirst(this, argument);
        }

        /// <summary>
        /// Performs a indexed draw with 8-bit index buffer elements,
        /// while also pre-incrementing the current instance value.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        private void DrawIndexBuffer8BeginEndInstanceSubsequent(int argument)
        {
            _drawManager.DrawIndexBuffer8BeginEndInstanceSubsequent(this, argument);
        }

        /// <summary>
        /// Performs a indexed draw with 16-bit index buffer elements,
        /// while also pre-incrementing the current instance value.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        private void DrawIndexBuffer16BeginEndInstanceSubsequent(int argument)
        {
            _drawManager.DrawIndexBuffer16BeginEndInstanceSubsequent(this, argument);
        }

        /// <summary>
        /// Performs a indexed draw with 32-bit index buffer elements,
        /// while also pre-incrementing the current instance value.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        private void DrawIndexBuffer32BeginEndInstanceSubsequent(int argument)
        {
            _drawManager.DrawIndexBuffer32BeginEndInstanceSubsequent(this, argument);
        }

        /// <summary>
        /// Clears the current color and depth-stencil buffers.
        /// Which buffers should be cleared is also specified on the argument.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        private void Clear(int argument)
        {
            _drawManager.Clear(this, argument);
        }

        /// <summary>
        /// Writes a GPU counter to guest memory.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        private void Report(int argument)
        {
            _semaphoreUpdater.Report(argument);
        }

        /// <summary>
        /// Performs high-level emulation of Falcon microcode function number "4".
        /// </summary>
        /// <param name="argument">Method call argument</param>
        private void SetFalcon04(int argument)
        {
            _state.State.SetMmeShadowScratch[0] = 1;
        }

        /// <summary>
        /// Updates the uniform buffer data with inline data.
        /// </summary>
        /// <param name="argument">New uniform buffer data word</param>
        private void ConstantBufferUpdate(int argument)
        {
            _cbUpdater.Update(argument);
        }

        /// <summary>
        /// Binds a uniform buffer for the vertex shader stage.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        private void ConstantBufferBindVertex(int argument)
        {
            _cbUpdater.BindVertex(argument);
        }

        /// <summary>
        /// Binds a uniform buffer for the tessellation control shader stage.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        private void ConstantBufferBindTessControl(int argument)
        {
            _cbUpdater.BindTessControl(argument);
        }

        /// <summary>
        /// Binds a uniform buffer for the tessellation evaluation shader stage.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        private void ConstantBufferBindTessEvaluation(int argument)
        {
            _cbUpdater.BindTessEvaluation(argument);
        }

        /// <summary>
        /// Binds a uniform buffer for the geometry shader stage.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        private void ConstantBufferBindGeometry(int argument)
        {
            _cbUpdater.BindGeometry(argument);
        }

        /// <summary>
        /// Binds a uniform buffer for the fragment shader stage.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        private void ConstantBufferBindFragment(int argument)
        {
            _cbUpdater.BindFragment(argument);
        }

        /// <summary>
        /// Generic register read function that just returns 0.
        /// </summary>
        /// <returns>Zero</returns>
        private static int Zero()
        {
            return 0;
        }

        /// <summary>
        /// Performs a indexed or non-indexed draw.
        /// </summary>
        /// <param name="topology">Primitive topology</param>
        /// <param name="count">Index count for indexed draws, vertex count for non-indexed draws</param>
        /// <param name="instanceCount">Instance count</param>
        /// <param name="firstIndex">First index on the index buffer for indexed draws, ignored for non-indexed draws</param>
        /// <param name="firstVertex">First vertex on the vertex buffer</param>
        /// <param name="firstInstance">First instance</param>
        /// <param name="indexed">True if the draw is indexed, false otherwise</param>
        public void Draw(
            PrimitiveTopology topology,
            int count,
            int instanceCount,
            int firstIndex,
            int firstVertex,
            int firstInstance,
            bool indexed)
        {
            _drawManager.Draw(this, topology, count, instanceCount, firstIndex, firstVertex, firstInstance, indexed);
        }

        /// <summary>
        /// Performs a indirect draw, with parameters from a GPU buffer.
        /// </summary>
        /// <param name="topology">Primitive topology</param>
        /// <param name="indirectBufferRange">Memory range of the buffer with the draw parameters, such as count, first index, etc</param>
        /// <param name="parameterBufferRange">Memory range of the buffer with the draw count</param>
        /// <param name="maxDrawCount">Maximum number of draws that can be made</param>
        /// <param name="stride">Distance in bytes between each entry on the data pointed to by <paramref name="indirectBufferRange"/></param>
        /// <param name="indexCount">Maximum number of indices that the draw can consume</param>
        /// <param name="drawType">Type of the indirect draw, which can be indexed or non-indexed, with or without a draw count</param>
        public void DrawIndirect(
            PrimitiveTopology topology,
            MultiRange indirectBufferRange,
            MultiRange parameterBufferRange,
            int maxDrawCount,
            int stride,
            int indexCount,
            IndirectDrawType drawType)
        {
            _drawManager.DrawIndirect(this, topology, indirectBufferRange, parameterBufferRange, maxDrawCount, stride, indexCount, drawType);
        }

        /// <summary>
        /// Clears the current color and depth-stencil buffers.
        /// Which buffers should be cleared can also specified with the arguments.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        /// <param name="layerCount">For array and 3D textures, indicates how many layers should be cleared</param>
        public void Clear(int argument, int layerCount)
        {
            _drawManager.Clear(this, argument, layerCount);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _drawManager.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
