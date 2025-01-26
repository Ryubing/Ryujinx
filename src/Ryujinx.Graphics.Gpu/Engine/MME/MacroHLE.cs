using Ryujinx.Common.Logging;
using Ryujinx.Common.Memory;
using Ryujinx.Graphics.Device;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Gpu.Engine.GPFifo;
using Ryujinx.Graphics.Gpu.Engine.Threed;
using Ryujinx.Graphics.Gpu.Engine.Types;
using Ryujinx.Graphics.Gpu.Memory;
using Ryujinx.Memory.Range;
using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gpu.Engine.MME
{
    /// <summary>
    /// Macro High-level emulation.
    /// </summary>
    class MacroHLE : IMacroEE
    {
        private const int ColorLayerCountOffset = 0x818;
        private const int ColorStructSize = 0x40;
        private const int ZetaLayerCountOffset = 0x1230;
        private const int UniformBufferBindVertexOffset = 0x2410;
        private const int FirstVertexOffset = 0x1434;

        private const int IndirectIndexedDataEntrySize = 0x14;

        private const int LogicOpOffset = 0x19c4;
        private const int ShaderIdScratchOffset = 0x3470;
        private const int ShaderAddressScratchOffset = 0x3488;
        private const int UpdateConstantBufferAddressesBase = 0x34a8;
        private const int UpdateConstantBufferSizesBase = 0x34bc;
        private const int UpdateConstantBufferAddressCbu = 0x3460;

        private readonly GPFifoProcessor _processor;
        private readonly MacroHLEFunctionName _functionName;

        /// <summary>
        /// Arguments FIFO.
        /// </summary>
        public Queue<FifoWord> Fifo { get; }

        /// <summary>
        /// Creates a new instance of the HLE macro handler.
        /// </summary>
        /// <param name="processor">GPU GP FIFO command processor</param>
        /// <param name="functionName">Name of the HLE macro function to be called</param>
        public MacroHLE(GPFifoProcessor processor, MacroHLEFunctionName functionName)
        {
            _processor = processor;
            _functionName = functionName;

            Fifo = new Queue<FifoWord>();
        }

        /// <summary>
        /// Executes a macro program until it exits.
        /// </summary>
        /// <param name="code">Code of the program to execute</param>
        /// <param name="state">GPU state at the time of the call</param>
        /// <param name="arg0">Optional argument passed to the program, 0 if not used</param>
        public void Execute(ReadOnlySpan<int> code, IDeviceState state, int arg0)
        {
            switch (_functionName)
            {
                case MacroHLEFunctionName.BindShaderProgram:
                    BindShaderProgram(state, arg0);
                    break;
                case MacroHLEFunctionName.ClearColor:
                    ClearColor(state, arg0);
                    break;
                case MacroHLEFunctionName.ClearDepthStencil:
                    ClearDepthStencil(state, arg0);
                    break;
                case MacroHLEFunctionName.DrawArraysInstanced:
                    DrawArraysInstanced(state, arg0);
                    break;
                case MacroHLEFunctionName.DrawElements:
                    DrawElements(state, arg0);
                    break;
                case MacroHLEFunctionName.DrawElementsInstanced:
                    DrawElementsInstanced(state, arg0);
                    break;
                case MacroHLEFunctionName.DrawElementsIndirect:
                    DrawElementsIndirect(state, arg0);
                    break;
                case MacroHLEFunctionName.MultiDrawElementsIndirectCount:
                    MultiDrawElementsIndirectCount(state, arg0);
                    break;
                case MacroHLEFunctionName.UpdateBlendState:
                    UpdateBlendState(state, arg0);
                    break;
                case MacroHLEFunctionName.UpdateColorMasks:
                    UpdateColorMasks(state, arg0);
                    break;
                case MacroHLEFunctionName.UpdateUniformBufferState:
                    UpdateUniformBufferState(state, arg0);
                    break;
                case MacroHLEFunctionName.UpdateUniformBufferStateCbu:
                    UpdateUniformBufferStateCbu(state, arg0);
                    break;
                case MacroHLEFunctionName.UpdateUniformBufferStateCbuV2:
                    UpdateUniformBufferStateCbuV2(state, arg0);
                    break;
                default:
                    throw new NotImplementedException(_functionName.ToString());
            }

            // It should be empty at this point, but clear it just to be safe.
            Fifo.Clear();
        }

        /// <summary>
        /// Binds a shader program with the index in arg0.
        /// </summary>
        /// <param name="state">GPU state at the time of the call</param>
        /// <param name="arg0">First argument of the call</param>
        private void BindShaderProgram(IDeviceState state, int arg0)
        {
            int scratchOffset = ShaderIdScratchOffset + arg0 * 4;

            int lastId = state.Read(scratchOffset);
            int id = FetchParam().Word;
            int offset = FetchParam().Word;

            if (lastId == id)
            {
                FetchParam();
                FetchParam();

                return;
            }

            _processor.ThreedClass.SetShaderOffset(arg0, (uint)offset);

            // Removes overflow on the method address into the increment portion.
            // Present in the original macro.
            int addrMask = unchecked((int)0xfffc0fff) << 2;

            state.Write(scratchOffset & addrMask, id);
            state.Write((ShaderAddressScratchOffset + arg0 * 4) & addrMask, offset);

            int stage = FetchParam().Word;
            uint cbAddress = (uint)FetchParam().Word;

            _processor.ThreedClass.UpdateUniformBufferState(65536, cbAddress >> 24, cbAddress << 8);

            int stageOffset = (stage & 0x7f) << 3;

            state.Write((UniformBufferBindVertexOffset + stageOffset * 4) & addrMask, 17);
        }

        /// <summary>
        /// Updates uniform buffer state for update or bind.
        /// </summary>
        /// <param name="state">GPU state at the time of the call</param>
        /// <param name="arg0">First argument of the call</param>
        private void UpdateUniformBufferState(IDeviceState state, int arg0)
        {
            uint address = (uint)state.Read(UpdateConstantBufferAddressesBase + arg0 * 4);
            int size = state.Read(UpdateConstantBufferSizesBase + arg0 * 4);

            _processor.ThreedClass.UpdateUniformBufferState(size, address >> 24, address << 8);
        }

        /// <summary>
        /// Updates uniform buffer state for update.
        /// </summary>
        /// <param name="state">GPU state at the time of the call</param>
        /// <param name="arg0">First argument of the call</param>
        private void UpdateUniformBufferStateCbu(IDeviceState state, int arg0)
        {
            uint address = (uint)state.Read(UpdateConstantBufferAddressCbu);

            UniformBufferState ubState = new()
            {
                Address = new()
                {
                    High = address >> 24,
                    Low = address << 8
                },
                Size = 24320,
                Offset = arg0 << 2
            };

            _processor.ThreedClass.UpdateUniformBufferState(ubState);
        }

        /// <summary>
        /// Updates uniform buffer state for update.
        /// </summary>
        /// <param name="state">GPU state at the time of the call</param>
        /// <param name="arg0">First argument of the call</param>
        private void UpdateUniformBufferStateCbuV2(IDeviceState state, int arg0)
        {
            uint address = (uint)state.Read(UpdateConstantBufferAddressCbu);

            UniformBufferState ubState = new()
            {
                Address = new()
                {
                    High = address >> 24,
                    Low = address << 8
                },
                Size = 28672,
                Offset = arg0 << 2
            };

            _processor.ThreedClass.UpdateUniformBufferState(ubState);
        }

        /// <summary>
        /// Updates blend enable using the given argument.
        /// </summary>
        /// <param name="state">GPU state at the time of the call</param>
        /// <param name="arg0">First argument of the call</param>
        private void UpdateBlendState(IDeviceState state, int arg0)
        {
            state.Write(LogicOpOffset, 0);

            Array8<Boolean32> enable = new();

            for (int i = 0; i < 8; i++)
            {
                enable[i] = new Boolean32((uint)(arg0 >> (i + 8)) & 1);
            }

            _processor.ThreedClass.UpdateBlendEnable(ref enable);
        }

        /// <summary>
        /// Updates color masks using the given argument and three pushed arguments.
        /// </summary>
        /// <param name="state">GPU state at the time of the call</param>
        /// <param name="arg0">First argument of the call</param>
        private void UpdateColorMasks(IDeviceState state, int arg0)
        {
            Array8<RtColorMask> masks = new();

            int index = 0;

            for (int i = 0; i < 4; i++)
            {
                masks[index++] = new RtColorMask((uint)arg0 & 0x1fff);
                masks[index++] = new RtColorMask(((uint)arg0 >> 16) & 0x1fff);

                if (i != 3)
                {
                    arg0 = FetchParam().Word;
                }
            }

            _processor.ThreedClass.UpdateColorMasks(ref masks);
        }

        /// <summary>
        /// Clears one bound color target.
        /// </summary>
        /// <param name="state">GPU state at the time of the call</param>
        /// <param name="arg0">First argument of the call</param>
        private void ClearColor(IDeviceState state, int arg0)
        {
            int index = (arg0 >> 6) & 0xf;
            int layerCount = state.Read(ColorLayerCountOffset + index * ColorStructSize);

            _processor.ThreedClass.Clear(arg0, layerCount);
        }

        /// <summary>
        /// Clears the current depth-stencil target.
        /// </summary>
        /// <param name="state">GPU state at the time of the call</param>
        /// <param name="arg0">First argument of the call</param>
        private void ClearDepthStencil(IDeviceState state, int arg0)
        {
            int layerCount = state.Read(ZetaLayerCountOffset);

            _processor.ThreedClass.Clear(arg0, layerCount);
        }

        /// <summary>
        /// Performs a draw.
        /// </summary>
        /// <param name="state">GPU state at the time of the call</param>
        /// <param name="arg0">First argument of the call</param>
        private void DrawArraysInstanced(IDeviceState state, int arg0)
        {
            PrimitiveTopology topology = (PrimitiveTopology)arg0;

            FifoWord count = FetchParam();
            FifoWord instanceCount = FetchParam();
            FifoWord firstVertex = FetchParam();
            FifoWord firstInstance = FetchParam();

            if (ShouldSkipDraw(state, instanceCount.Word))
            {
                return;
            }

            _processor.ThreedClass.Draw(
                topology,
                count.Word,
                instanceCount.Word,
                0,
                firstVertex.Word,
                firstInstance.Word,
                indexed: false);
        }

        /// <summary>
        /// Performs a indexed draw.
        /// </summary>
        /// <param name="state">GPU state at the time of the call</param>
        /// <param name="arg0">First argument of the call</param>
        private void DrawElements(IDeviceState state, int arg0)
        {
            PrimitiveTopology topology = (PrimitiveTopology)arg0;

            FifoWord indexAddressHigh = FetchParam();
            FifoWord indexAddressLow = FetchParam();
            FifoWord indexType = FetchParam();
            int firstIndex = 0;
            FifoWord indexCount = FetchParam();

            _processor.ThreedClass.UpdateIndexBuffer(
                (uint)indexAddressHigh.Word,
                (uint)indexAddressLow.Word,
                (IndexType)indexType.Word);

            _processor.ThreedClass.Draw(
                topology,
                indexCount.Word,
                1,
                firstIndex,
                state.Read(FirstVertexOffset),
                0,
                indexed: true);
        }

        /// <summary>
        /// Performs a indexed draw.
        /// </summary>
        /// <param name="state">GPU state at the time of the call</param>
        /// <param name="arg0">First argument of the call</param>
        private void DrawElementsInstanced(IDeviceState state, int arg0)
        {
            PrimitiveTopology topology = (PrimitiveTopology)arg0;

            FifoWord count = FetchParam();
            FifoWord instanceCount = FetchParam();
            FifoWord firstIndex = FetchParam();
            FifoWord firstVertex = FetchParam();
            FifoWord firstInstance = FetchParam();

            if (ShouldSkipDraw(state, instanceCount.Word))
            {
                return;
            }

            _processor.ThreedClass.Draw(
                topology,
                count.Word,
                instanceCount.Word,
                firstIndex.Word,
                firstVertex.Word,
                firstInstance.Word,
                indexed: true);
        }

        /// <summary>
        /// Performs a indirect indexed draw, with parameters from a GPU buffer.
        /// </summary>
        /// <param name="state">GPU state at the time of the call</param>
        /// <param name="arg0">First argument of the call</param>
        private void DrawElementsIndirect(IDeviceState state, int arg0)
        {
            PrimitiveTopology topology = (PrimitiveTopology)arg0;

            FifoWord count = FetchParam();
            FifoWord instanceCount = FetchParam();
            FifoWord firstIndex = FetchParam();
            FifoWord firstVertex = FetchParam();
            FifoWord firstInstance = FetchParam();

            ulong indirectBufferGpuVa = count.GpuVa;

            BufferCache bufferCache = _processor.MemoryManager.Physical.BufferCache;

            bool useBuffer = bufferCache.CheckModified(_processor.MemoryManager, indirectBufferGpuVa, IndirectIndexedDataEntrySize, out ulong indirectBufferAddress);

            if (useBuffer)
            {
                int indexCount = firstIndex.Word + count.Word;

                _processor.ThreedClass.DrawIndirect(
                    topology,
                    new MultiRange(indirectBufferAddress, IndirectIndexedDataEntrySize),
                    default,
                    1,
                    IndirectIndexedDataEntrySize,
                    indexCount,
                    IndirectDrawType.DrawIndexedIndirect);
            }
            else
            {
                if (ShouldSkipDraw(state, instanceCount.Word))
                {
                    return;
                }

                _processor.ThreedClass.Draw(
                    topology,
                    count.Word,
                    instanceCount.Word,
                    firstIndex.Word,
                    firstVertex.Word,
                    firstInstance.Word,
                    indexed: true);
            }
        }

        /// <summary>
        /// Performs a indirect indexed multi-draw, with parameters from a GPU buffer.
        /// </summary>
        /// <param name="state">GPU state at the time of the call</param>
        /// <param name="arg0">First argument of the call</param>
        private void MultiDrawElementsIndirectCount(IDeviceState state, int arg0)
        {
            int arg1 = FetchParam().Word;
            int arg2 = FetchParam().Word;
            int arg3 = FetchParam().Word;

            int startDraw = arg0;
            int endDraw = arg1;
            PrimitiveTopology topology = (PrimitiveTopology)arg2;
            int paddingWords = arg3;
            int stride = paddingWords * 4 + 0x14;

            ulong parameterBufferGpuVa = FetchParam().GpuVa;

            int maxDrawCount = endDraw - startDraw;

            if (startDraw != 0)
            {
                int drawCount = _processor.MemoryManager.Read<int>(parameterBufferGpuVa, tracked: true);

                // Calculate maximum draw count based on the previous draw count and current draw count.
                if ((uint)drawCount <= (uint)startDraw)
                {
                    // The start draw is past our total draw count, so all draws were already performed.
                    maxDrawCount = 0;
                }
                else
                {
                    // Perform just the missing number of draws.
                    maxDrawCount = (int)Math.Min((uint)maxDrawCount, (uint)(drawCount - startDraw));
                }
            }

            if (maxDrawCount == 0)
            {
                Fifo.Clear();
                return;
            }

            ulong indirectBufferGpuVa = 0;
            int indexCount = 0;

            for (int i = 0; i < maxDrawCount; i++)
            {
                FifoWord count = FetchParam();
#pragma warning disable IDE0059 // Remove unnecessary value assignment
                FifoWord instanceCount = FetchParam();
                FifoWord firstIndex = FetchParam();
                FifoWord firstVertex = FetchParam();
                FifoWord firstInstance = FetchParam();
#pragma warning restore IDE0059

                if (i == 0)
                {
                    indirectBufferGpuVa = count.GpuVa;
                }

                indexCount = Math.Max(indexCount, count.Word + firstIndex.Word);

                if (i != maxDrawCount - 1)
                {
                    for (int j = 0; j < paddingWords; j++)
                    {
                        FetchParam();
                    }
                }
            }

            BufferCache bufferCache = _processor.MemoryManager.Physical.BufferCache;

            ulong indirectBufferSize = (ulong)maxDrawCount * (ulong)stride;

            MultiRange indirectBufferRange = bufferCache.TranslateAndCreateMultiBuffers(_processor.MemoryManager, indirectBufferGpuVa, indirectBufferSize, BufferStage.Indirect);
            MultiRange parameterBufferRange = bufferCache.TranslateAndCreateMultiBuffers(_processor.MemoryManager, parameterBufferGpuVa, 4, BufferStage.Indirect);

            _processor.ThreedClass.DrawIndirect(
                topology,
                indirectBufferRange,
                parameterBufferRange,
                maxDrawCount,
                stride,
                indexCount,
                Threed.IndirectDrawType.DrawIndexedIndirectCount);
        }

        /// <summary>
        /// Checks if the draw should be skipped, because the masked instance count is zero.
        /// </summary>
        /// <param name="state">Current GPU state</param>
        /// <param name="instanceCount">Draw instance count</param>
        /// <returns>True if the draw should be skipped, false otherwise</returns>
        private static bool ShouldSkipDraw(IDeviceState state, int instanceCount)
        {
            return (Read(state, 0xd1b) & instanceCount) == 0;
        }

        /// <summary>
        /// Fetches a arguments from the arguments FIFO.
        /// </summary>
        /// <returns>The call argument, or a 0 value with null address if the FIFO is empty</returns>
        private FifoWord FetchParam()
        {
            if (!Fifo.TryDequeue(out FifoWord value))
            {
                Logger.Warning?.Print(LogClass.Gpu, "Macro attempted to fetch an inexistent argument.");

                return new FifoWord(0UL, 0);
            }

            return value;
        }

        /// <summary>
        /// Reads data from a GPU register.
        /// </summary>
        /// <param name="state">Current GPU state</param>
        /// <param name="reg">Register offset to read</param>
        /// <returns>GPU register value</returns>
        private static int Read(IDeviceState state, int reg)
        {
            return state.Read(reg * 4);
        }
    }
}
