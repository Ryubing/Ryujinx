using Ryujinx.Common.Memory;
using Ryujinx.Graphics.Shader.StructuredIr;
using Ryujinx.Graphics.Shader.Translation;
using System.Runtime.CompilerServices;

namespace Ryujinx.Graphics.Shader
{
    enum VertexInfoBufferField
    {
        // Must match the order of the fields on the struct.
        VertexCounts,
        GeometryCounts,
        VertexStrides,
        VertexOffsets,
    }

    public struct VertexInfoBuffer
    {
        public static readonly int RequiredSize;

        public static readonly int VertexCountsOffset;
        public static readonly int GeometryCountsOffset;
        public static readonly int VertexStridesOffset;
        public static readonly int VertexOffsetsOffset;

        private static int OffsetOf<T>(ref VertexInfoBuffer storage, ref T target)
        {
            return (int)Unsafe.ByteOffset(ref Unsafe.As<VertexInfoBuffer, T>(ref storage), ref target);
        }

        static VertexInfoBuffer()
        {
            RequiredSize = Unsafe.SizeOf<VertexInfoBuffer>();

            VertexInfoBuffer instance = new();

            VertexCountsOffset = OffsetOf(ref instance, ref instance.VertexCounts);
            GeometryCountsOffset = OffsetOf(ref instance, ref instance.GeometryCounts);
            VertexStridesOffset = OffsetOf(ref instance, ref instance.VertexStrides);
            VertexOffsetsOffset = OffsetOf(ref instance, ref instance.VertexOffsets);
        }

        internal static StructureType GetStructureType()
        {
            return new StructureType([
                new StructureField(AggregateType.Vector4 | AggregateType.U32, "vertex_counts"),
                new StructureField(AggregateType.Vector4 | AggregateType.U32, "geometry_counts"),
                new StructureField(AggregateType.Array | AggregateType.Vector4 | AggregateType.U32, "vertex_strides", ResourceReservations.MaxVertexBufferTextures),
                new StructureField(AggregateType.Array | AggregateType.Vector4 | AggregateType.U32, "vertex_offsets", ResourceReservations.MaxVertexBufferTextures)
            ]);
        }

        public Vector4<int> VertexCounts;
        public Vector4<int> GeometryCounts;
        public Array32<Vector4<int>> VertexStrides;
        public Array32<Vector4<int>> VertexOffsets;
    }
}
