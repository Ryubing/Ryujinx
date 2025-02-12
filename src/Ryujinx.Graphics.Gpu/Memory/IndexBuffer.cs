using Ryujinx.Graphics.GAL;
using Ryujinx.Memory.Range;

namespace Ryujinx.Graphics.Gpu.Memory
{
    /// <summary>
    /// GPU Index Buffer information.
    /// </summary>
    struct IndexBuffer
    {
        public BufferCache BufferCache;
        public MultiRange Range;
        public IndexType Type;
    }
}
