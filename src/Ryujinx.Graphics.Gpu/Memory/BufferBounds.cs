using Ryujinx.Graphics.Shader;
using Ryujinx.Memory.Range;
using System;

namespace Ryujinx.Graphics.Gpu.Memory
{
    /// <summary>
    /// Memory range used for buffers.
    /// </summary>
    readonly struct BufferBounds : IEquatable<BufferBounds>
    {
        /// <summary>
        /// Physical memory backing the buffer.
        /// </summary>
        public PhysicalMemory Physical { get; }

        /// <summary>
        /// Buffer cache that owns the buffer.
        /// </summary>
        public BufferCache BufferCache => Physical.BufferCache;

        /// <summary>
        /// Physical memory ranges where the buffer is mapped.
        /// </summary>
        public MultiRange Range { get; }

        /// <summary>
        /// Buffer usage flags.
        /// </summary>
        public BufferUsageFlags Flags { get; }

        /// <summary>
        /// Indicates that the backing memory for the buffer does not exist.
        /// </summary>
        public bool IsUnmapped => Range.IsUnmapped;

        /// <summary>
        /// Creates a new buffer region.
        /// </summary>
        /// <param name="range">Physical memory ranges where the buffer is mapped</param>
        /// <param name="flags">Buffer usage flags</param>
        public BufferBounds(PhysicalMemory physical, MultiRange range, BufferUsageFlags flags = BufferUsageFlags.None)
        {
            Physical = physical;
            Range = range;
            Flags = flags;
        }

        public override bool Equals(object obj)
        {
            return obj is BufferBounds bounds && Equals(bounds);
        }

        public bool Equals(BufferBounds bounds)
        {
            return Range == bounds.Range && Flags == bounds.Flags;
        }

        public bool Equals(ref BufferBounds bounds)
        {
            return Range == bounds.Range && Flags == bounds.Flags;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Range, Flags);
        }
    }
}
