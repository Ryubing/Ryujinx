using Ryujinx.Memory.Range;

namespace Ryujinx.Graphics.Gpu.Memory
{
    /// <summary>
    /// GPU Vertex Buffer information.
    /// </summary>
    struct VertexBuffer
    {
        public BufferCache BufferCache;
        public MultiRange Range;
        public int Stride;
        public int Divisor;
    }
}
