using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Ryujinx.Graphics.Shader.Translation
{
    class ShaderDefinitions
    {
        private readonly GpuGraphicsState _graphicsState;

        public ShaderStage Stage { get; }

        public int ComputeLocalSizeX { get; }
        public int ComputeLocalSizeY { get; }
        public int ComputeLocalSizeZ { get; }

        public bool TessCw => _graphicsState.TessCw;
        public TessPatchType TessPatchType => _graphicsState.TessPatchType;
        public TessSpacing TessSpacing => _graphicsState.TessSpacing;

        public bool AlphaToCoverageDitherEnable => _graphicsState.AlphaToCoverageEnable && _graphicsState.AlphaToCoverageDitherEnable;
        public bool ViewportTransformDisable => _graphicsState.ViewportTransformDisable;

        public bool DepthMode => _graphicsState.DepthMode;

        public float PointSize => _graphicsState.PointSize;

        public AlphaTestOp AlphaTestCompare => _graphicsState.AlphaTestCompare;
        public float AlphaTestReference => _graphicsState.AlphaTestReference;

        public bool GpPassthrough { get; }
        public bool LastInVertexPipeline { get; set; }

        public int ThreadsPerInputPrimitive { get; private set; }

        public InputTopology InputTopology => _graphicsState.Topology;
        public OutputTopology OutputTopology { get; }

        public int MaxOutputVertices { get; }

        public bool DualSourceBlend => _graphicsState.DualSourceBlendEnable;
        public bool EarlyZForce => _graphicsState.EarlyZForce;

        public bool YNegateEnabled => _graphicsState.YNegateEnabled;
        public bool OriginUpperLeft => _graphicsState.OriginUpperLeft;

        public bool HalvePrimitiveId => _graphicsState.HalvePrimitiveId;

        public ImapPixelType[] ImapTypes { get; }
        public bool IaIndexing { get; private set; }
        public bool OaIndexing { get; private set; }

        public int OmapTargets { get; }
        public bool OmapSampleMask { get; }
        public bool OmapDepth { get; }

        public bool SupportsScaledVertexFormats { get; }

        public bool TransformFeedbackEnabled { get; }

        private readonly TransformFeedbackOutput[] _transformFeedbackOutputs;

        readonly struct TransformFeedbackVariable : IEquatable<TransformFeedbackVariable>
        {
            public IoVariable IoVariable { get; }
            public int Location { get; }
            public int Component { get; }

            public TransformFeedbackVariable(IoVariable ioVariable, int location = 0, int component = 0)
            {
                IoVariable = ioVariable;
                Location = location;
                Component = component;
            }

            public override bool Equals(object other)
            {
                return other is TransformFeedbackVariable tfbVar && Equals(tfbVar);
            }

            public bool Equals(TransformFeedbackVariable other)
            {
                return IoVariable == other.IoVariable &&
                    Location == other.Location &&
                    Component == other.Component;
            }

            public override int GetHashCode()
            {
                return (int)IoVariable | (Location << 8) | (Component << 16);
            }

            public override string ToString()
            {
                return $"{IoVariable}.{Location}.{Component}";
            }
        }

        private readonly Dictionary<TransformFeedbackVariable, TransformFeedbackOutput> _transformFeedbackDefinitions;

        public ShaderDefinitions(ShaderStage stage, ulong transformFeedbackVecMap, TransformFeedbackOutput[] transformFeedbackOutputs)
        {
            Stage = stage;
            TransformFeedbackEnabled = transformFeedbackOutputs != null;
            _transformFeedbackOutputs = transformFeedbackOutputs;
            _transformFeedbackDefinitions = new();

            PopulateTransformFeedbackDefinitions(transformFeedbackVecMap, transformFeedbackOutputs);
        }

        public ShaderDefinitions(
            ShaderStage stage,
            int computeLocalSizeX,
            int computeLocalSizeY,
            int computeLocalSizeZ)
        {
            Stage = stage;
            ComputeLocalSizeX = computeLocalSizeX;
            ComputeLocalSizeY = computeLocalSizeY;
            ComputeLocalSizeZ = computeLocalSizeZ;
        }

        public ShaderDefinitions(
            ShaderStage stage,
            GpuGraphicsState graphicsState,
            bool gpPassthrough,
            int threadsPerInputPrimitive,
            OutputTopology outputTopology,
            int maxOutputVertices)
        {
            Stage = stage;
            _graphicsState = graphicsState;
            GpPassthrough = gpPassthrough;
            ThreadsPerInputPrimitive = threadsPerInputPrimitive;
            OutputTopology = outputTopology;
            MaxOutputVertices = maxOutputVertices;
        }

        public ShaderDefinitions(
            ShaderStage stage,
            GpuGraphicsState graphicsState,
            bool gpPassthrough,
            int threadsPerInputPrimitive,
            OutputTopology outputTopology,
            int maxOutputVertices,
            ImapPixelType[] imapTypes,
            int omapTargets,
            bool omapSampleMask,
            bool omapDepth,
            bool supportsScaledVertexFormats,
            ulong transformFeedbackVecMap,
            TransformFeedbackOutput[] transformFeedbackOutputs)
        {
            Stage = stage;
            _graphicsState = graphicsState;
            GpPassthrough = gpPassthrough;
            ThreadsPerInputPrimitive = threadsPerInputPrimitive;
            OutputTopology = outputTopology;
            MaxOutputVertices = gpPassthrough ? graphicsState.Topology.ToInputVerticesNoAdjacency() : maxOutputVertices;
            ImapTypes = imapTypes;
            OmapTargets = omapTargets;
            OmapSampleMask = omapSampleMask;
            OmapDepth = omapDepth;
            LastInVertexPipeline = stage < ShaderStage.Fragment;
            SupportsScaledVertexFormats = supportsScaledVertexFormats;
            TransformFeedbackEnabled = transformFeedbackOutputs != null;
            _transformFeedbackOutputs = transformFeedbackOutputs;
            _transformFeedbackDefinitions = new();

            PopulateTransformFeedbackDefinitions(transformFeedbackVecMap, transformFeedbackOutputs);
        }

        private void PopulateTransformFeedbackDefinitions(ulong transformFeedbackVecMap, TransformFeedbackOutput[] transformFeedbackOutputs)
        {
            while (transformFeedbackVecMap != 0)
            {
                int vecIndex = BitOperations.TrailingZeroCount(transformFeedbackVecMap);

                for (int subIndex = 0; subIndex < 4; subIndex++)
                {
                    int wordOffset = vecIndex * 4 + subIndex;
                    int byteOffset = wordOffset * 4;

                    if (transformFeedbackOutputs[wordOffset].Valid)
                    {
                        IoVariable ioVariable = Instructions.AttributeMap.GetIoVariable(this, byteOffset, out int location);
                        int component = 0;

                        if (HasPerLocationInputOrOutputComponent(ioVariable, location, subIndex, isOutput: true))
                        {
                            component = subIndex;
                        }

                        TransformFeedbackVariable transformFeedbackVariable = new(ioVariable, location, component);
                        _transformFeedbackDefinitions.TryAdd(transformFeedbackVariable, transformFeedbackOutputs[wordOffset]);
                    }
                }

                transformFeedbackVecMap &= ~(1UL << vecIndex);
            }
        }

        public void EnableInputIndexing()
        {
            IaIndexing = true;
        }

        public void EnableOutputIndexing()
        {
            OaIndexing = true;
        }

        public bool TryGetTransformFeedbackOutput(IoVariable ioVariable, int location, int component, out TransformFeedbackOutput transformFeedbackOutput)
        {
            if (!HasTransformFeedbackOutputs())
            {
                transformFeedbackOutput = default;
                return false;
            }

            TransformFeedbackVariable transformFeedbackVariable = new(ioVariable, location, component);
            return _transformFeedbackDefinitions.TryGetValue(transformFeedbackVariable, out transformFeedbackOutput);
        }

        private bool HasTransformFeedbackOutputs()
        {
            return TransformFeedbackEnabled && (LastInVertexPipeline || Stage == ShaderStage.Fragment);
        }

        public bool HasTransformFeedbackOutputs(bool isOutput)
        {
            return TransformFeedbackEnabled && ((isOutput && LastInVertexPipeline) || (!isOutput && Stage == ShaderStage.Fragment));
        }

        public bool HasPerLocationInputOrOutput(IoVariable ioVariable, bool isOutput)
        {
            if (ioVariable == IoVariable.UserDefined)
            {
                return (!isOutput && !IaIndexing) || (isOutput && !OaIndexing);
            }

            return ioVariable == IoVariable.FragmentOutputColor;
        }

        public bool HasPerLocationInputOrOutputComponent(IoVariable ioVariable, int location, int component, bool isOutput)
        {
            if (ioVariable != IoVariable.UserDefined || !HasTransformFeedbackOutputs(isOutput))
            {
                return false;
            }

            return GetTransformFeedbackOutputComponents(location, component) == 1;
        }

        public TransformFeedbackOutput GetTransformFeedbackOutput(int wordOffset)
        {
            return _transformFeedbackOutputs[wordOffset];
        }

        public TransformFeedbackOutput GetTransformFeedbackOutput(int location, int component)
        {
            return GetTransformFeedbackOutput((AttributeConsts.UserAttributeBase / 4) + location * 4 + component);
        }

        public int GetTransformFeedbackOutputComponents(int location, int component)
        {
            int baseIndex = (AttributeConsts.UserAttributeBase / 4) + location * 4;
            int index = baseIndex + component;
            int count = 1;

            for (; count < 4; count++)
            {
                ref TransformFeedbackOutput prev = ref _transformFeedbackOutputs[baseIndex + count - 1];
                ref TransformFeedbackOutput curr = ref _transformFeedbackOutputs[baseIndex + count];

                int prevOffset = prev.Offset;
                int currOffset = curr.Offset;

                if (!prev.Valid || !curr.Valid || prevOffset + 4 != currOffset)
                {
                    break;
                }
            }

            if (baseIndex + count <= index)
            {
                return 1;
            }

            return count;
        }

        public AggregateType GetFragmentOutputColorType(int location)
        {
            return AggregateType.Vector4 | _graphicsState.FragmentOutputTypes[location].ToAggregateType();
        }

        public AggregateType GetUserDefinedType(int location, bool isOutput)
        {
            if ((!isOutput && IaIndexing) || (isOutput && OaIndexing))
            {
                return AggregateType.Array | AggregateType.Vector4 | AggregateType.FP32;
            }

            AggregateType type = AggregateType.Vector4;

            if (Stage == ShaderStage.Vertex && !isOutput)
            {
                type |= _graphicsState.AttributeTypes[location].ToAggregateType(SupportsScaledVertexFormats);
            }
            else
            {
                type |= AggregateType.FP32;
            }

            return type;
        }

        public AttributeType GetAttributeType(int location)
        {
            return _graphicsState.AttributeTypes[location];
        }

        public bool IsAttributeSint(int location)
        {
            return (_graphicsState.AttributeTypes[location] & ~AttributeType.AnyPacked) == AttributeType.Sint;
        }

        public bool IsAttributePacked(int location)
        {
            return _graphicsState.AttributeTypes[location].HasFlag(AttributeType.Packed);
        }

        public bool IsAttributePackedRgb10A2Signed(int location)
        {
            return _graphicsState.AttributeTypes[location].HasFlag(AttributeType.PackedRgb10A2Signed);
        }

        public int GetGeometryOutputIndexBufferStridePerInstance()
        {
            return MaxOutputVertices + OutputTopology switch
            {
                OutputTopology.LineStrip => MaxOutputVertices / 2,
                OutputTopology.TriangleStrip => MaxOutputVertices / 3,
                _ => MaxOutputVertices,
            };
        }

        public int GetGeometryOutputIndexBufferStride()
        {
            return GetGeometryOutputIndexBufferStridePerInstance() * ThreadsPerInputPrimitive;
        }
    }
}
