using Ryujinx.Common.Memory;
using Ryujinx.Graphics.Shader.StructuredIr;
using Ryujinx.Graphics.Shader.Translation;
using System.Runtime.CompilerServices;

namespace Ryujinx.Graphics.Shader
{
    public struct Vector4<T>
    {
        public T X;
        public T Y;
        public T Z;
        public T W;
    }

    enum SupportBufferField
    {
        // Must match the order of the fields on the struct.
        FragmentAlphaTest,
        FragmentIsBgra,
        ViewportInverse,
        ViewportSize,
        FragmentRenderScaleCount,
        RenderScale,
        TfeOffset,
        TfeVertexCount,
    }

    public struct SupportBuffer
    {
        public const int Binding = 0;

        public static readonly int FieldSize;
        public static readonly int RequiredSize;

        public static readonly int FragmentAlphaTestOffset;
        public static readonly int FragmentIsBgraOffset;
        public static readonly int ViewportInverseOffset;
        public static readonly int ViewportSizeOffset;
        public static readonly int FragmentRenderScaleCountOffset;
        public static readonly int GraphicsRenderScaleOffset;
        public static readonly int ComputeRenderScaleOffset;
        public static readonly int TfeOffsetOffset;
        public static readonly int TfeVertexCountOffset;

        public const int FragmentIsBgraCount = 8;
        // One for the render target, 64 for the textures, and 8 for the images.
        public const int RenderScaleMaxCount = 1 + 64 + 8;

        private static int OffsetOf<T>(ref SupportBuffer storage, ref T target)
        {
            return (int)Unsafe.ByteOffset(ref Unsafe.As<SupportBuffer, T>(ref storage), ref target);
        }

        static SupportBuffer()
        {
            FieldSize = Unsafe.SizeOf<Vector4<float>>();
            RequiredSize = Unsafe.SizeOf<SupportBuffer>();

            SupportBuffer instance = new();

            FragmentAlphaTestOffset = OffsetOf(ref instance, ref instance.FragmentAlphaTest);
            FragmentIsBgraOffset = OffsetOf(ref instance, ref instance.FragmentIsBgra);
            ViewportInverseOffset = OffsetOf(ref instance, ref instance.ViewportInverse);
            ViewportSizeOffset = OffsetOf(ref instance, ref instance.ViewportSize);
            FragmentRenderScaleCountOffset = OffsetOf(ref instance, ref instance.FragmentRenderScaleCount);
            GraphicsRenderScaleOffset = OffsetOf(ref instance, ref instance.RenderScale);
            ComputeRenderScaleOffset = GraphicsRenderScaleOffset + FieldSize;
            TfeOffsetOffset = OffsetOf(ref instance, ref instance.TfeOffset);
            TfeVertexCountOffset = OffsetOf(ref instance, ref instance.TfeVertexCount);
        }

        internal static StructureType GetStructureType()
        {
            return new StructureType([
                new StructureField(AggregateType.U32, "alpha_test"),
                new StructureField(AggregateType.Array | AggregateType.U32, "is_bgra", FragmentIsBgraCount),
                new StructureField(AggregateType.Vector4 | AggregateType.FP32, "viewport_inverse"),
                new StructureField(AggregateType.Vector4 | AggregateType.FP32, "viewport_size"),
                new StructureField(AggregateType.S32, "frag_scale_count"),
                new StructureField(AggregateType.Array | AggregateType.FP32, "render_scale", RenderScaleMaxCount),
                new StructureField(AggregateType.Vector4 | AggregateType.S32, "tfe_offset"),
                new StructureField(AggregateType.S32, "tfe_vertex_count")
            ]);
        }

        public Vector4<int> FragmentAlphaTest;
        public Array8<Vector4<int>> FragmentIsBgra;
        public Vector4<float> ViewportInverse;
        public Vector4<float> ViewportSize;
        public Vector4<int> FragmentRenderScaleCount;

        // Render scale max count: 1 + 64 + 8. First scale is fragment output scale, others are textures/image inputs.
        public Array73<Vector4<float>> RenderScale;

        public Vector4<int> TfeOffset;
        public Vector4<int> TfeVertexCount;
    }
}
