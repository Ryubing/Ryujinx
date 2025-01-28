using Ryujinx.Graphics.Shader.Decoders;
using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using static Ryujinx.Graphics.Shader.IntermediateRepresentation.OperandHelper;

namespace Ryujinx.Graphics.Shader.Translation
{
    class EmitterContext
    {
        public DecodedProgram Program { get; }
        public TranslatorContext TranslatorContext { get; }
        public ResourceManager ResourceManager { get; }

        public bool VertexAsCompute { get; }

        public bool IsNonMain { get; }

        public Block CurrBlock { get; set; }
        public InstOp CurrOp { get; set; }

        public int OperationsCount => _operations.Count;

        private readonly struct BrxTarget
        {
            public readonly Operand Selector;
            public readonly int ExpectedValue;
            public readonly ulong NextTargetAddress;

            public BrxTarget(Operand selector, int expectedValue, ulong nextTargetAddress)
            {
                Selector = selector;
                ExpectedValue = expectedValue;
                NextTargetAddress = nextTargetAddress;
            }
        }

        private class BlockLabel
        {
            public readonly Operand Label;
            public BrxTarget BrxTarget;

            public BlockLabel(Operand label)
            {
                Label = label;
            }
        }

        private readonly List<Operation> _operations;
        private readonly Dictionary<ulong, BlockLabel> _labels;

        public EmitterContext()
        {
            _operations = [];
            _labels = new Dictionary<ulong, BlockLabel>();
        }

        public EmitterContext(
            TranslatorContext translatorContext,
            ResourceManager resourceManager,
            DecodedProgram program,
            bool vertexAsCompute,
            bool isNonMain) : this()
        {
            TranslatorContext = translatorContext;
            ResourceManager = resourceManager;
            Program = program;
            VertexAsCompute = vertexAsCompute;
            IsNonMain = isNonMain;

            EmitStart();
        }

        private void EmitStart()
        {
            if (TranslatorContext.Options.Flags.HasFlag(TranslationFlags.VertexA))
            {
                return;
            }

            // Vulkan requires the point size to be always written on the shader if the primitive topology is points.
            // OpenGL requires the point size to be always written on the shader if PROGRAM_POINT_SIZE is set.
            if (TranslatorContext.Definitions.Stage == ShaderStage.Vertex)
            {
                this.Store(StorageKind.Output, IoVariable.PointSize, null, ConstF(TranslatorContext.Definitions.PointSize));
            }

            if (VertexAsCompute)
            {
                int vertexInfoCbBinding = ResourceManager.Reservations.VertexInfoConstantBufferBinding;
                int countFieldIndex = TranslatorContext.Stage == ShaderStage.Vertex
                    ? (int)VertexInfoBufferField.VertexCounts
                    : (int)VertexInfoBufferField.GeometryCounts;

                Operand outputVertexOffset = this.Load(StorageKind.Input, IoVariable.GlobalId, Const(0));
                Operand vertexCount = this.Load(StorageKind.ConstantBuffer, vertexInfoCbBinding, Const(countFieldIndex), Const(0));
                Operand isVertexOob = this.ICompareGreaterOrEqualUnsigned(outputVertexOffset, vertexCount);

                Operand lblVertexInBounds = Label();

                this.BranchIfFalse(lblVertexInBounds, isVertexOob);
                this.Return();
                this.MarkLabel(lblVertexInBounds);

                Operand outputInstanceOffset = this.Load(StorageKind.Input, IoVariable.GlobalId, Const(1));
                Operand instanceCount = this.Load(StorageKind.ConstantBuffer, vertexInfoCbBinding, Const((int)VertexInfoBufferField.VertexCounts), Const(1));
                Operand firstVertex = this.Load(StorageKind.ConstantBuffer, vertexInfoCbBinding, Const((int)VertexInfoBufferField.VertexCounts), Const(2));
                Operand firstInstance = this.Load(StorageKind.ConstantBuffer, vertexInfoCbBinding, Const((int)VertexInfoBufferField.VertexCounts), Const(3));
                Operand ibBaseOffset = this.Load(StorageKind.ConstantBuffer, vertexInfoCbBinding, Const((int)VertexInfoBufferField.GeometryCounts), Const(3));
                Operand isInstanceOob = this.ICompareGreaterOrEqualUnsigned(outputInstanceOffset, instanceCount);

                Operand lblInstanceInBounds = Label();

                this.BranchIfFalse(lblInstanceInBounds, isInstanceOob);
                this.Return();
                this.MarkLabel(lblInstanceInBounds);

                if (TranslatorContext.Stage == ShaderStage.Vertex)
                {
                    Operand vertexIndexVr = Local();

                    this.TextureSample(
                        SamplerType.TextureBuffer,
                        TextureFlags.IntCoords,
                        ResourceManager.Reservations.GetIndexBufferTextureSetAndBinding(),
                        1,
                        [vertexIndexVr],
                        [this.IAdd(ibBaseOffset, outputVertexOffset)]);

                    this.Store(StorageKind.LocalMemory, ResourceManager.LocalVertexIndexVertexRateMemoryId, this.IAdd(firstVertex, vertexIndexVr));
                    this.Store(StorageKind.LocalMemory, ResourceManager.LocalVertexIndexInstanceRateMemoryId, this.IAdd(firstInstance, outputInstanceOffset));
                }
                else if (TranslatorContext.Stage == ShaderStage.Geometry)
                {
                    int inputVertices = TranslatorContext.Definitions.InputTopology.ToInputVertices();

                    Operand baseVertex = this.IMultiply(outputVertexOffset, Const(inputVertices));

                    for (int index = 0; index < inputVertices; index++)
                    {
                        Operand vertexIndex = Local();

                        this.TextureSample(
                            SamplerType.TextureBuffer,
                            TextureFlags.IntCoords,
                            ResourceManager.Reservations.GetTopologyRemapBufferTextureSetAndBinding(),
                            1,
                            [vertexIndex],
                            [this.IAdd(baseVertex, Const(index))]);

                        this.Store(StorageKind.LocalMemory, ResourceManager.LocalTopologyRemapMemoryId, Const(index), vertexIndex);
                    }

                    this.Store(StorageKind.LocalMemory, ResourceManager.LocalGeometryOutputVertexCountMemoryId, Const(0));
                    this.Store(StorageKind.LocalMemory, ResourceManager.LocalGeometryOutputIndexCountMemoryId, Const(0));
                }
            }
        }

        public T GetOp<T>() where T : unmanaged
        {
            Debug.Assert(Unsafe.SizeOf<T>() == sizeof(ulong));
            ulong op = CurrOp.RawOpCode;
            return Unsafe.As<ulong, T>(ref op);
        }

        public Operand Add(Instruction inst, Operand dest = null, params Operand[] sources)
        {
            Operation operation = new(inst, dest, sources);

            _operations.Add(operation);

            return dest;
        }

        public Operand Add(Instruction inst, StorageKind storageKind, Operand dest = null, params Operand[] sources)
        {
            Operation operation = new(inst, storageKind, dest, sources);

            _operations.Add(operation);

            return dest;
        }

        public (Operand, Operand) Add(Instruction inst, (Operand, Operand) dest, params Operand[] sources)
        {
            Operand[] dests = [dest.Item1, dest.Item2];

            Operation operation = new(inst, 0, dests, sources);

            Add(operation);

            return dest;
        }

        public void Add(Operation operation)
        {
            _operations.Add(operation);
        }

        public void MarkLabel(Operand label)
        {
            Add(Instruction.MarkLabel, label);
        }

        public Operand GetLabel(ulong address)
        {
            return EnsureBlockLabel(address).Label;
        }

        public void SetBrxTarget(ulong address, Operand selector, int targetValue, ulong nextTargetAddress)
        {
            BlockLabel blockLabel = EnsureBlockLabel(address);
            Debug.Assert(blockLabel.BrxTarget.Selector == null);
            blockLabel.BrxTarget = new BrxTarget(selector, targetValue, nextTargetAddress);
        }

        public void EnterBlock(ulong address)
        {
            BlockLabel blockLabel = EnsureBlockLabel(address);

            MarkLabel(blockLabel.Label);

            BrxTarget brxTarget = blockLabel.BrxTarget;

            if (brxTarget.Selector != null)
            {
                this.BranchIfFalse(GetLabel(brxTarget.NextTargetAddress), this.ICompareEqual(brxTarget.Selector, Const(brxTarget.ExpectedValue)));
            }
        }

        private BlockLabel EnsureBlockLabel(ulong address)
        {
            if (!_labels.TryGetValue(address, out BlockLabel blockLabel))
            {
                blockLabel = new BlockLabel(Label());

                _labels.Add(address, blockLabel);
            }

            return blockLabel;
        }

        public void PrepareForVertexReturn()
        {
            // TODO: Support transform feedback emulation on stages other than vertex.
            // Those stages might produce more primitives, so it needs a way to "compact" the output after it is written.

            if (!TranslatorContext.GpuAccessor.QueryHostSupportsTransformFeedback() &&
                TranslatorContext.GpuAccessor.QueryTransformFeedbackEnabled() &&
                TranslatorContext.Stage == ShaderStage.Vertex)
            {
                Operand vertexCount = this.Load(StorageKind.ConstantBuffer, SupportBuffer.Binding, Const((int)SupportBufferField.TfeVertexCount));

                for (int tfbIndex = 0; tfbIndex < ResourceReservations.TfeBuffersCount; tfbIndex++)
                {
                    ReadOnlySpan<byte> locations = TranslatorContext.GpuAccessor.QueryTransformFeedbackVaryingLocations(tfbIndex);
                    int stride = TranslatorContext.GpuAccessor.QueryTransformFeedbackStride(tfbIndex);

                    Operand baseOffset = this.Load(StorageKind.ConstantBuffer, SupportBuffer.Binding, Const((int)SupportBufferField.TfeOffset), Const(tfbIndex));
                    Operand baseVertex = this.Load(StorageKind.Input, IoVariable.BaseVertex);
                    Operand baseInstance = this.Load(StorageKind.Input, IoVariable.BaseInstance);
                    Operand vertexIndex = this.Load(StorageKind.Input, IoVariable.VertexIndex);
                    Operand instanceIndex = this.Load(StorageKind.Input, IoVariable.InstanceIndex);

                    Operand outputVertexOffset = this.ISubtract(vertexIndex, baseVertex);
                    Operand outputInstanceOffset = this.ISubtract(instanceIndex, baseInstance);

                    Operand outputBaseVertex = this.IMultiply(outputInstanceOffset, vertexCount);

                    Operand vertexOffset = this.IMultiply(this.IAdd(outputBaseVertex, outputVertexOffset), Const(stride / 4));
                    baseOffset = this.IAdd(baseOffset, vertexOffset);

                    for (int j = 0; j < locations.Length; j++)
                    {
                        byte location = locations[j];
                        if (location == 0xff)
                        {
                            continue;
                        }

                        Operand offset = this.IAdd(baseOffset, Const(j));
                        Operand value = Instructions.AttributeMap.GenerateAttributeLoad(this, null, location * 4, isOutput: true, isPerPatch: false);

                        int binding = ResourceManager.Reservations.GetTfeBufferStorageBufferBinding(tfbIndex);

                        this.Store(StorageKind.StorageBuffer, binding, Const(0), offset, value);
                    }
                }
            }

            if (TranslatorContext.Definitions.ViewportTransformDisable)
            {
                Operand x = this.Load(StorageKind.Output, IoVariable.Position, null, Const(0));
                Operand y = this.Load(StorageKind.Output, IoVariable.Position, null, Const(1));
                Operand xScale = this.Load(StorageKind.ConstantBuffer, SupportBuffer.Binding, Const((int)SupportBufferField.ViewportInverse), Const(0));
                Operand yScale = this.Load(StorageKind.ConstantBuffer, SupportBuffer.Binding, Const((int)SupportBufferField.ViewportInverse), Const(1));
                Operand negativeOne = ConstF(-1.0f);

                this.Store(StorageKind.Output, IoVariable.Position, null, Const(0), this.FPFusedMultiplyAdd(x, xScale, negativeOne));
                this.Store(StorageKind.Output, IoVariable.Position, null, Const(1), this.FPFusedMultiplyAdd(y, yScale, negativeOne));
            }

            if (TranslatorContext.Definitions.DepthMode && !TranslatorContext.GpuAccessor.QueryHostSupportsDepthClipControl())
            {
                Operand z = this.Load(StorageKind.Output, IoVariable.Position, null, Const(2));
                Operand w = this.Load(StorageKind.Output, IoVariable.Position, null, Const(3));
                Operand halfW = this.FPMultiply(w, ConstF(0.5f));

                this.Store(StorageKind.Output, IoVariable.Position, null, Const(2), this.FPFusedMultiplyAdd(z, ConstF(0.5f), halfW));
            }
        }

        public void PrepareForVertexReturn(out Operand oldXLocal, out Operand oldYLocal, out Operand oldZLocal)
        {
            if (TranslatorContext.Definitions.ViewportTransformDisable)
            {
                oldXLocal = Local();
                this.Copy(oldXLocal, this.Load(StorageKind.Output, IoVariable.Position, null, Const(0)));
                oldYLocal = Local();
                this.Copy(oldYLocal, this.Load(StorageKind.Output, IoVariable.Position, null, Const(1)));
            }
            else
            {
                oldXLocal = null;
                oldYLocal = null;
            }

            if (TranslatorContext.Definitions.DepthMode && !TranslatorContext.GpuAccessor.QueryHostSupportsDepthClipControl())
            {
                oldZLocal = Local();
                this.Copy(oldZLocal, this.Load(StorageKind.Output, IoVariable.Position, null, Const(2)));
            }
            else
            {
                oldZLocal = null;
            }

            PrepareForVertexReturn();
        }

        public bool PrepareForReturn()
        {
            if (IsNonMain)
            {
                return true;
            }

            if (TranslatorContext.Definitions.LastInVertexPipeline &&
                (TranslatorContext.Definitions.Stage == ShaderStage.Vertex || TranslatorContext.Definitions.Stage == ShaderStage.TessellationEvaluation) &&
                (TranslatorContext.Options.Flags & TranslationFlags.VertexA) == 0)
            {
                PrepareForVertexReturn();
            }
            else if (TranslatorContext.Definitions.Stage == ShaderStage.Geometry)
            {
                void WritePositionOutput(int primIndex)
                {
                    Operand x = this.Load(StorageKind.Input, IoVariable.Position, Const(primIndex), Const(0));
                    Operand y = this.Load(StorageKind.Input, IoVariable.Position, Const(primIndex), Const(1));
                    Operand z = this.Load(StorageKind.Input, IoVariable.Position, Const(primIndex), Const(2));
                    Operand w = this.Load(StorageKind.Input, IoVariable.Position, Const(primIndex), Const(3));

                    this.Store(StorageKind.Output, IoVariable.Position, null, Const(0), x);
                    this.Store(StorageKind.Output, IoVariable.Position, null, Const(1), y);
                    this.Store(StorageKind.Output, IoVariable.Position, null, Const(2), z);
                    this.Store(StorageKind.Output, IoVariable.Position, null, Const(3), w);
                }

                void WriteUserDefinedOutput(int index, int primIndex)
                {
                    Operand x = this.Load(StorageKind.Input, IoVariable.UserDefined, Const(index), Const(primIndex), Const(0));
                    Operand y = this.Load(StorageKind.Input, IoVariable.UserDefined, Const(index), Const(primIndex), Const(1));
                    Operand z = this.Load(StorageKind.Input, IoVariable.UserDefined, Const(index), Const(primIndex), Const(2));
                    Operand w = this.Load(StorageKind.Input, IoVariable.UserDefined, Const(index), Const(primIndex), Const(3));

                    this.Store(StorageKind.Output, IoVariable.UserDefined, null, Const(index), Const(0), x);
                    this.Store(StorageKind.Output, IoVariable.UserDefined, null, Const(index), Const(1), y);
                    this.Store(StorageKind.Output, IoVariable.UserDefined, null, Const(index), Const(2), z);
                    this.Store(StorageKind.Output, IoVariable.UserDefined, null, Const(index), Const(3), w);
                }

                if (TranslatorContext.Definitions.GpPassthrough && !TranslatorContext.GpuAccessor.QueryHostSupportsGeometryShaderPassthrough())
                {
                    int inputStart, inputEnd, inputStep;

                    InputTopology topology = TranslatorContext.Definitions.InputTopology;

                    if (topology == InputTopology.LinesAdjacency)
                    {
                        inputStart = 1;
                        inputEnd = 3;
                        inputStep = 1;
                    }
                    else if (topology == InputTopology.TrianglesAdjacency)
                    {
                        inputStart = 0;
                        inputEnd = 6;
                        inputStep = 2;
                    }
                    else
                    {
                        inputStart = 0;
                        inputEnd = topology.ToInputVerticesNoAdjacency();
                        inputStep = 1;
                    }

                    for (int primIndex = inputStart; primIndex < inputEnd; primIndex += inputStep)
                    {
                        WritePositionOutput(primIndex);

                        int passthroughAttributes = TranslatorContext.AttributeUsage.PassthroughAttributes;
                        while (passthroughAttributes != 0)
                        {
                            int index = BitOperations.TrailingZeroCount(passthroughAttributes);
                            WriteUserDefinedOutput(index, primIndex);
                            passthroughAttributes &= ~(1 << index);
                        }

                        this.EmitVertex();
                    }

                    this.EndPrimitive();
                }
            }
            else if (TranslatorContext.Definitions.Stage == ShaderStage.Fragment)
            {
                GenerateAlphaToCoverageDitherDiscard();

                bool supportsBgra = TranslatorContext.GpuAccessor.QueryHostSupportsBgraFormat();

                if (TranslatorContext.Definitions.OmapDepth)
                {
                    Operand src = Register(TranslatorContext.GetDepthRegister(), RegisterType.Gpr);

                    this.Store(StorageKind.Output, IoVariable.FragmentOutputDepth, null, src);
                }

                AlphaTestOp alphaTestOp = TranslatorContext.Definitions.AlphaTestCompare;

                if (alphaTestOp != AlphaTestOp.Always)
                {
                    if (alphaTestOp == AlphaTestOp.Never)
                    {
                        this.Discard();
                    }
                    else if ((TranslatorContext.Definitions.OmapTargets & 8) != 0)
                    {
                        Instruction comparator = alphaTestOp switch
                        {
                            AlphaTestOp.Equal => Instruction.CompareEqual,
                            AlphaTestOp.Greater => Instruction.CompareGreater,
                            AlphaTestOp.GreaterOrEqual => Instruction.CompareGreaterOrEqual,
                            AlphaTestOp.Less => Instruction.CompareLess,
                            AlphaTestOp.LessOrEqual => Instruction.CompareLessOrEqual,
                            AlphaTestOp.NotEqual => Instruction.CompareNotEqual,
                            _ => 0,
                        };

                        Debug.Assert(comparator != 0, $"Invalid alpha test operation \"{alphaTestOp}\".");

                        Operand alpha = Register(3, RegisterType.Gpr);
                        Operand alphaRef = ConstF(TranslatorContext.Definitions.AlphaTestReference);
                        Operand alphaPass = Add(Instruction.FP32 | comparator, Local(), alpha, alphaRef);
                        Operand alphaPassLabel = Label();

                        this.BranchIfTrue(alphaPassLabel, alphaPass);
                        this.Discard();
                        this.MarkLabel(alphaPassLabel);
                    }
                }

                // We don't need to output anything if alpha test always fails.
                if (alphaTestOp == AlphaTestOp.Never)
                {
                    return false;
                }

                int regIndexBase = 0;

                for (int rtIndex = 0; rtIndex < 8; rtIndex++)
                {
                    for (int component = 0; component < 4; component++)
                    {
                        bool componentEnabled = (TranslatorContext.Definitions.OmapTargets & (1 << (rtIndex * 4 + component))) != 0;
                        if (!componentEnabled)
                        {
                            continue;
                        }

                        Operand src = Register(regIndexBase + component, RegisterType.Gpr);

                        // Perform B <-> R swap if needed, for BGRA formats (not supported on OpenGL).
                        if (!supportsBgra && (component == 0 || component == 2))
                        {
                            Operand isBgra = this.Load(StorageKind.ConstantBuffer, SupportBuffer.Binding, Const((int)SupportBufferField.FragmentIsBgra), Const(rtIndex));

                            Operand lblIsBgra = Label();
                            Operand lblEnd = Label();

                            this.BranchIfTrue(lblIsBgra, isBgra);

                            this.Store(StorageKind.Output, IoVariable.FragmentOutputColor, null, Const(rtIndex), Const(component), src);
                            this.Branch(lblEnd);

                            MarkLabel(lblIsBgra);

                            this.Store(StorageKind.Output, IoVariable.FragmentOutputColor, null, Const(rtIndex), Const(2 - component), src);

                            MarkLabel(lblEnd);
                        }
                        else
                        {
                            this.Store(StorageKind.Output, IoVariable.FragmentOutputColor, null, Const(rtIndex), Const(component), src);
                        }
                    }

                    bool targetEnabled = (TranslatorContext.Definitions.OmapTargets & (0xf << (rtIndex * 4))) != 0;
                    if (targetEnabled)
                    {
                        regIndexBase += 4;
                    }
                }
            }

            if (VertexAsCompute)
            {
                if (TranslatorContext.Stage == ShaderStage.Vertex)
                {
                    int vertexInfoCbBinding = ResourceManager.Reservations.VertexInfoConstantBufferBinding;
                    int vertexOutputSbBinding = ResourceManager.Reservations.VertexOutputStorageBufferBinding;
                    int stride = ResourceManager.Reservations.OutputSizePerInvocation;

                    Operand vertexCount = this.Load(StorageKind.ConstantBuffer, vertexInfoCbBinding, Const((int)VertexInfoBufferField.VertexCounts), Const(0));

                    Operand outputVertexOffset = this.Load(StorageKind.Input, IoVariable.GlobalId, Const(0));
                    Operand outputInstanceOffset = this.Load(StorageKind.Input, IoVariable.GlobalId, Const(1));

                    Operand outputBaseVertex = this.IMultiply(outputInstanceOffset, vertexCount);

                    Operand baseOffset = this.IMultiply(this.IAdd(outputBaseVertex, outputVertexOffset), Const(stride));

                    for (int offset = 0; offset < stride; offset++)
                    {
                        Operand vertexOffset = this.IAdd(baseOffset, Const(offset));
                        Operand value = this.Load(StorageKind.LocalMemory, ResourceManager.LocalVertexDataMemoryId, Const(offset));

                        this.Store(StorageKind.StorageBuffer, vertexOutputSbBinding, Const(0), vertexOffset, value);
                    }
                }
                else if (TranslatorContext.Stage == ShaderStage.Geometry)
                {
                    Operand lblLoopHead = Label();
                    Operand lblExit = Label();

                    this.MarkLabel(lblLoopHead);

                    Operand writtenIndices = this.Load(StorageKind.LocalMemory, ResourceManager.LocalGeometryOutputIndexCountMemoryId);

                    int maxIndicesPerPrimitiveInvocation = TranslatorContext.Definitions.GetGeometryOutputIndexBufferStridePerInstance();
                    int maxIndicesPerPrimitive = maxIndicesPerPrimitiveInvocation * TranslatorContext.Definitions.ThreadsPerInputPrimitive;

                    this.BranchIfTrue(lblExit, this.ICompareGreaterOrEqualUnsigned(writtenIndices, Const(maxIndicesPerPrimitiveInvocation)));

                    int vertexInfoCbBinding = ResourceManager.Reservations.VertexInfoConstantBufferBinding;

                    Operand primitiveIndex = this.Load(StorageKind.Input, IoVariable.GlobalId, Const(0));
                    Operand instanceIndex = this.Load(StorageKind.Input, IoVariable.GlobalId, Const(1));
                    Operand invocationId = this.Load(StorageKind.Input, IoVariable.GlobalId, Const(2));
                    Operand vertexCount = this.Load(StorageKind.ConstantBuffer, vertexInfoCbBinding, Const((int)VertexInfoBufferField.VertexCounts), Const(0));
                    Operand primitiveId = this.IAdd(this.IMultiply(instanceIndex, vertexCount), primitiveIndex);
                    Operand ibOffset = this.IMultiply(primitiveId, Const(maxIndicesPerPrimitive));
                    ibOffset = this.IAdd(ibOffset, this.IMultiply(invocationId, Const(maxIndicesPerPrimitiveInvocation)));
                    ibOffset = this.IAdd(ibOffset, writtenIndices);

                    this.Store(StorageKind.StorageBuffer, ResourceManager.Reservations.GeometryIndexOutputStorageBufferBinding, Const(0), ibOffset, Const(-1));
                    this.Store(StorageKind.LocalMemory, ResourceManager.LocalGeometryOutputIndexCountMemoryId, this.IAdd(writtenIndices, Const(1)));

                    this.Branch(lblLoopHead);

                    this.MarkLabel(lblExit);
                }
            }

            return true;
        }

        private void GenerateAlphaToCoverageDitherDiscard()
        {
            // If the feature is disabled, or alpha is not written, then we're done.
            if (!TranslatorContext.Definitions.AlphaToCoverageDitherEnable || (TranslatorContext.Definitions.OmapTargets & 8) == 0)
            {
                return;
            }

            // 11 11 11 10 10 10 10 00
            // 11 01 01 01 01 00 00 00
            Operand ditherMask = Const(unchecked((int)0xfbb99110u));

            Operand fragCoordX = this.Load(StorageKind.Input, IoVariable.FragmentCoord, null, Const(0));
            Operand fragCoordY = this.Load(StorageKind.Input, IoVariable.FragmentCoord, null, Const(1));

            Operand x = this.BitwiseAnd(this.FP32ConvertToU32(fragCoordX), Const(1));
            Operand y = this.BitwiseAnd(this.FP32ConvertToU32(fragCoordY), Const(1));
            Operand xy = this.BitwiseOr(x, this.ShiftLeft(y, Const(1)));

            Operand alpha = Register(3, RegisterType.Gpr);
            Operand scaledAlpha = this.FPMultiply(this.FPSaturate(alpha), ConstF(8));
            Operand quantizedAlpha = this.IMinimumU32(this.FP32ConvertToU32(scaledAlpha), Const(7));
            Operand shift = this.BitwiseOr(this.ShiftLeft(quantizedAlpha, Const(2)), xy);
            Operand opaque = this.BitwiseAnd(this.ShiftRightU32(ditherMask, shift), Const(1));

            Operand a2cDitherEndLabel = Label();

            this.BranchIfTrue(a2cDitherEndLabel, opaque);
            this.Discard();
            this.MarkLabel(a2cDitherEndLabel);
        }

        public Operation[] GetOperations()
        {
            return _operations.ToArray();
        }
    }
}
