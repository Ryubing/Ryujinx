using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.StructuredIr;
using System.Collections.Generic;
using System.Numerics;

namespace Ryujinx.Graphics.Shader.Translation
{
    public class ResourceReservations
    {
        public const int TfeBuffersCount = 4;

        public const int MaxVertexBufferTextures = 32;

        private const int TextureSetIndex = 2; // TODO: Get from GPU accessor.

        public int VertexInfoConstantBufferBinding { get; }
        public int VertexOutputStorageBufferBinding { get; }
        public int GeometryVertexOutputStorageBufferBinding { get; }
        public int GeometryIndexOutputStorageBufferBinding { get; }
        public int IndexBufferTextureBinding { get; }
        public int TopologyRemapBufferTextureBinding { get; }

        public int ReservedConstantBuffers { get; }
        public int ReservedStorageBuffers { get; }
        public int ReservedTextures { get; }
        public int ReservedImages { get; }
        public int InputSizePerInvocation { get; }
        public int OutputSizePerInvocation { get; }
        public int OutputSizeInBytesPerInvocation => OutputSizePerInvocation * sizeof(uint);

        private readonly int _tfeBufferSbBaseBinding;
        private readonly int _vertexBufferTextureBaseBinding;

        private readonly Dictionary<IoDefinition, int> _offsets;
        internal IReadOnlyDictionary<IoDefinition, int> Offsets => _offsets;

        internal ResourceReservations(bool isTransformFeedbackEmulated, bool vertexAsCompute)
        {
            // All stages reserves the first constant buffer binding for the support buffer.
            ReservedConstantBuffers = 1;
            ReservedStorageBuffers = 0;
            ReservedTextures = 0;
            ReservedImages = 0;

            if (isTransformFeedbackEmulated)
            {
                // Transform feedback emulation currently always uses 4 storage buffers.
                _tfeBufferSbBaseBinding = ReservedStorageBuffers;
                ReservedStorageBuffers = TfeBuffersCount;
            }

            if (vertexAsCompute)
            {
                // One constant buffer reserved for vertex related state.
                VertexInfoConstantBufferBinding = ReservedConstantBuffers++;

                // One storage buffer for the output vertex data.
                VertexOutputStorageBufferBinding = ReservedStorageBuffers++;

                // One storage buffer for the output geometry vertex data.
                GeometryVertexOutputStorageBufferBinding = ReservedStorageBuffers++;

                // One storage buffer for the output geometry index data.
                GeometryIndexOutputStorageBufferBinding = ReservedStorageBuffers++;

                // Enough textures reserved for all vertex attributes, plus the index buffer.
                IndexBufferTextureBinding = ReservedTextures;
                TopologyRemapBufferTextureBinding = ReservedTextures + 1;
                _vertexBufferTextureBaseBinding = ReservedTextures + 2;
                ReservedTextures += 2 + MaxVertexBufferTextures;
            }
        }

        internal ResourceReservations(
            IGpuAccessor gpuAccessor,
            bool isTransformFeedbackEmulated,
            bool vertexAsCompute,
            IoUsage? vacInput,
            IoUsage vacOutput) : this(isTransformFeedbackEmulated, vertexAsCompute)
        {
            if (vertexAsCompute)
            {
                _offsets = [];

                if (vacInput.HasValue)
                {
                    InputSizePerInvocation = FillIoOffsetMap(gpuAccessor, StorageKind.Input, vacInput.Value);
                }

                OutputSizePerInvocation = FillIoOffsetMap(gpuAccessor, StorageKind.Output, vacOutput);
            }
        }

        private int FillIoOffsetMap(IGpuAccessor gpuAccessor, StorageKind storageKind, IoUsage vacUsage)
        {
            int offset = 0;

            for (int c = 0; c < 4; c++)
            {
                _offsets.Add(new IoDefinition(storageKind, IoVariable.Position, 0, c), offset++);
            }

            _offsets.Add(new IoDefinition(storageKind, IoVariable.PointSize), offset++);

            int clipDistancesWrittenMap = vacUsage.ClipDistancesWritten;

            while (clipDistancesWrittenMap != 0)
            {
                int index = BitOperations.TrailingZeroCount(clipDistancesWrittenMap);

                _offsets.Add(new IoDefinition(storageKind, IoVariable.ClipDistance, 0, index), offset++);

                clipDistancesWrittenMap &= ~(1 << index);
            }

            if (vacUsage.UsesRtLayer)
            {
                _offsets.Add(new IoDefinition(storageKind, IoVariable.Layer), offset++);
            }

            if (vacUsage.UsesViewportIndex && gpuAccessor.QueryHostSupportsViewportIndexVertexTessellation())
            {
                _offsets.Add(new IoDefinition(storageKind, IoVariable.VertexIndex), offset++);
            }

            if (vacUsage.UsesViewportMask && gpuAccessor.QueryHostSupportsViewportMask())
            {
                _offsets.Add(new IoDefinition(storageKind, IoVariable.ViewportMask), offset++);
            }

            int usedDefinedMap = vacUsage.UserDefinedMap;

            while (usedDefinedMap != 0)
            {
                int location = BitOperations.TrailingZeroCount(usedDefinedMap);

                for (int c = 0; c < 4; c++)
                {
                    _offsets.Add(new IoDefinition(storageKind, IoVariable.UserDefined, location, c), offset++);
                }

                usedDefinedMap &= ~(1 << location);
            }

            return offset;
        }

        internal static bool IsVectorOrArrayVariable(IoVariable variable)
        {
            return variable switch
            {
                IoVariable.ClipDistance or
                IoVariable.Position => true,
                _ => false,
            };
        }

        public int GetTfeBufferStorageBufferBinding(int bufferIndex)
        {
            return _tfeBufferSbBaseBinding + bufferIndex;
        }

        public int GetVertexBufferTextureBinding(int vaLocation)
        {
            return _vertexBufferTextureBaseBinding + vaLocation;
        }

        public SetBindingPair GetVertexBufferTextureSetAndBinding(int vaLocation)
        {
            return new SetBindingPair(TextureSetIndex, GetVertexBufferTextureBinding(vaLocation));
        }

        public SetBindingPair GetIndexBufferTextureSetAndBinding()
        {
            return new SetBindingPair(TextureSetIndex, IndexBufferTextureBinding);
        }

        public SetBindingPair GetTopologyRemapBufferTextureSetAndBinding()
        {
            return new SetBindingPair(TextureSetIndex, TopologyRemapBufferTextureBinding);
        }

        internal bool TryGetOffset(StorageKind storageKind, int location, int component, out int offset)
        {
            return _offsets.TryGetValue(new IoDefinition(storageKind, IoVariable.UserDefined, location, component), out offset);
        }

        internal bool TryGetOffset(StorageKind storageKind, IoVariable ioVariable, int location, int component, out int offset)
        {
            return _offsets.TryGetValue(new IoDefinition(storageKind, ioVariable, location, component), out offset);
        }

        internal bool TryGetOffset(StorageKind storageKind, IoVariable ioVariable, int component, out int offset)
        {
            return _offsets.TryGetValue(new IoDefinition(storageKind, ioVariable, 0, component), out offset);
        }

        internal bool TryGetOffset(StorageKind storageKind, IoVariable ioVariable, out int offset)
        {
            return _offsets.TryGetValue(new IoDefinition(storageKind, ioVariable, 0, 0), out offset);
        }
    }
}
