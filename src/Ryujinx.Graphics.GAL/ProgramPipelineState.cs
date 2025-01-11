using Ryujinx.Common.Memory;
using System;

namespace Ryujinx.Graphics.GAL
{
    /// <summary>
    /// Descriptor for a pipeline buffer binding.
    /// </summary>
    public readonly struct BufferPipelineDescriptor
    {
        public bool Enable { get; }
        public int Stride { get; }
        public int Divisor { get; }

        public BufferPipelineDescriptor(bool enable, int stride, int divisor)
        {
            Enable = enable;
            Stride = stride;
            Divisor = divisor;
        }
    }

    /// <summary>
    /// State required for a program to compile shaders.
    /// </summary>
    public struct ProgramPipelineState
    {
        // Some state is considered always dynamic and should not be included:
        // - Viewports/Scissors
        // - Bias values (not enable)

        public int SamplesCount;
        public Array8<bool> AttachmentEnable;
        public Array8<Format> AttachmentFormats;
        public bool DepthStencilEnable;
        public Format DepthStencilFormat;

        public bool LogicOpEnable;
        public LogicalOp LogicOp;
        public Array8<BlendDescriptor> BlendDescriptors;
        public Array8<uint> ColorWriteMask;

        public int VertexAttribCount;
        public Array32<VertexAttribDescriptor> VertexAttribs;

        public int VertexBufferCount;
        public Array32<BufferPipelineDescriptor> VertexBuffers;

        // TODO: Min/max depth bounds.
        public DepthTestDescriptor DepthTest;
        public StencilTestDescriptor StencilTest;
        public FrontFace FrontFace;
        public Face CullMode;

        public PolygonModeMask BiasEnable;

        public bool AlphaToCoverageEnable;
        public bool AlphaToOneEnable;

        // TODO: Polygon mode.
        public bool DepthClampEnable;
        public bool RasterizerDiscard;
        public PrimitiveTopology Topology;
        public bool PrimitiveRestartEnable;
        public uint PatchControlPoints;

        public float DepthBiasUnits;
        public float DepthBiasFactor;

        public DepthMode DepthMode;

        public void SetVertexAttribs(ReadOnlySpan<VertexAttribDescriptor> vertexAttribs)
        {
            VertexAttribCount = vertexAttribs.Length;
            vertexAttribs.CopyTo(VertexAttribs.AsSpan());
        }

        public void SetLogicOpState(bool enable, LogicalOp op)
        {
            LogicOp = op;
            LogicOpEnable = enable;
        }
    }
}
