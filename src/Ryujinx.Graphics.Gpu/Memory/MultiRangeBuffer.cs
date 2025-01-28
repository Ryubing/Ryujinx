using Ryujinx.Graphics.GAL;
using Ryujinx.Memory.Range;
using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gpu.Memory
{
    /// <summary>
    /// Buffer, used to store vertex and index data, uniform and storage buffers, and others.
    /// </summary>
    class MultiRangeBuffer : IMultiRangeItem, IDisposable
    {
        private readonly GpuContext _context;

        /// <summary>
        /// Host buffer handle.
        /// </summary>
        public BufferHandle Handle { get; }

        /// <summary>
        /// Range of memory where the data is located.
        /// </summary>
        public MultiRange Range { get; }

        /// <summary>
        /// Ever increasing counter value indicating when the buffer was modified relative to other buffers.
        /// </summary>
        public int ModificationSequenceNumber { get; private set; }

        /// <summary>
        /// Physical buffer dependency entry.
        /// </summary>
        private readonly struct PhysicalDependency
        {
            /// <summary>
            /// Physical buffer.
            /// </summary>
            public readonly Buffer PhysicalBuffer;

            /// <summary>
            /// Offset of the range on the physical buffer.
            /// </summary>
            public readonly ulong PhysicalOffset;

            /// <summary>
            /// Offset of the range on the virtual buffer.
            /// </summary>
            public readonly ulong VirtualOffset;

            /// <summary>
            /// Size of the range.
            /// </summary>
            public readonly ulong Size;

            /// <summary>
            /// Creates a new physical dependency.
            /// </summary>
            /// <param name="physicalBuffer">Physical buffer</param>
            /// <param name="physicalOffset">Offset of the range on the physical buffer</param>
            /// <param name="virtualOffset">Offset of the range on the virtual buffer</param>
            /// <param name="size">Size of the range</param>
            public PhysicalDependency(Buffer physicalBuffer, ulong physicalOffset, ulong virtualOffset, ulong size)
            {
                PhysicalBuffer = physicalBuffer;
                PhysicalOffset = physicalOffset;
                VirtualOffset = virtualOffset;
                Size = size;
            }
        }

        private List<PhysicalDependency> _dependencies;
        private BufferModifiedRangeList _modifiedRanges = null;

        /// <summary>
        /// Creates a new instance of the buffer.
        /// </summary>
        /// <param name="context">GPU context that the buffer belongs to</param>
        /// <param name="range">Range of memory where the data is mapped</param>
        public MultiRangeBuffer(GpuContext context, MultiRange range)
        {
            _context = context;
            Range = range;
            Handle = context.Renderer.CreateBuffer((int)range.GetSize());
        }

        /// <summary>
        /// Creates a new instance of the buffer.
        /// </summary>
        /// <param name="context">GPU context that the buffer belongs to</param>
        /// <param name="range">Range of memory where the data is mapped</param>
        /// <param name="storages">Backing memory for the buffer</param>
        public MultiRangeBuffer(GpuContext context, MultiRange range, ReadOnlySpan<BufferRange> storages)
        {
            _context = context;
            Range = range;
            Handle = context.Renderer.CreateBufferSparse(storages);
        }

        /// <summary>
        /// Gets a sub-range from the buffer.
        /// </summary>
        /// <remarks>
        /// This can be used to bind and use sub-ranges of the buffer on the host API.
        /// </remarks>
        /// <param name="range">Range of memory where the data is mapped</param>
        /// <returns>The buffer sub-range</returns>
        public BufferRange GetRange(MultiRange range)
        {
            int offset = Range.FindOffset(range);

            return new BufferRange(Handle, offset, (int)range.GetSize());
        }

        /// <summary>
        /// Removes all physical buffer dependencies.
        /// </summary>
        public void ClearPhysicalDependencies()
        {
            _dependencies?.Clear();
        }

        /// <summary>
        /// Adds a physical buffer dependency.
        /// </summary>
        /// <param name="buffer">Physical buffer to be added</param>
        /// <param name="rangeAddress">Address inside the physical buffer where the virtual buffer range is located</param>
        /// <param name="dstOffset">Offset inside the virtual buffer where the physical range is located</param>
        /// <param name="rangeSize">Size of the range in bytes</param>
        public void AddPhysicalDependency(Buffer buffer, ulong rangeAddress, ulong dstOffset, ulong rangeSize)
        {
            (_dependencies ??= []).Add(new(buffer, rangeAddress - buffer.Address, dstOffset, rangeSize));
            buffer.AddVirtualDependency(this);
        }

        /// <summary>
        /// Tries to get the physical range corresponding to the given physical buffer.
        /// </summary>
        /// <param name="buffer">Physical buffer</param>
        /// <param name="minimumVirtOffset">Minimum virtual offset that a range match can have</param>
        /// <param name="physicalOffset">Physical offset of the match</param>
        /// <param name="virtualOffset">Virtual offset of the match, always greater than or equal <paramref name="minimumVirtOffset"/></param>
        /// <param name="size">Size of the range match</param>
        /// <returns>True if a match was found for the given parameters, false otherwise</returns>
        public bool TryGetPhysicalOffset(Buffer buffer, ulong minimumVirtOffset, out ulong physicalOffset, out ulong virtualOffset, out ulong size)
        {
            physicalOffset = 0;
            virtualOffset = 0;
            size = 0;

            if (_dependencies != null)
            {
                foreach (PhysicalDependency dependency in _dependencies)
                {
                    if (dependency.PhysicalBuffer == buffer && dependency.VirtualOffset >= minimumVirtOffset)
                    {
                        physicalOffset = dependency.PhysicalOffset;
                        virtualOffset = dependency.VirtualOffset;
                        size = dependency.Size;

                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Adds a modified virtual memory range.
        /// </summary>
        /// <remarks>
        /// This is only required when the host does not support sparse buffers, otherwise only physical buffers need to track modification.
        /// </remarks>
        /// <param name="range">Modified range</param>
        /// <param name="modifiedSequenceNumber">ModificationSequenceNumber</param>
        public void AddModifiedRegion(MultiRange range, int modifiedSequenceNumber)
        {
            _modifiedRanges ??= new(_context, null, null);

            for (int i = 0; i < range.Count; i++)
            {
                MemoryRange subRange = range.GetSubRange(i);

                _modifiedRanges.SignalModified(subRange.Address, subRange.Size);
            }

            ModificationSequenceNumber = modifiedSequenceNumber;
        }

        /// <summary>
        /// Calls the specified <paramref name="rangeAction"/> for all modified ranges that overlaps with <paramref name="buffer"/>.
        /// </summary>
        /// <param name="buffer">Buffer to have its range checked</param>
        /// <param name="rangeAction">Action to perform for modified ranges</param>
        public void ConsumeModifiedRegion(Buffer buffer, Action<ulong, ulong> rangeAction)
        {
            ConsumeModifiedRegion(buffer.Address, buffer.Size, rangeAction);
        }

        /// <summary>
        /// Calls the specified <paramref name="rangeAction"/> for all modified ranges that overlaps with <paramref name="address"/> and <paramref name="size"/>.
        /// </summary>
        /// <param name="address">Address of the region to consume</param>
        /// <param name="size">Size of the region to consume</param>
        /// <param name="rangeAction">Action to perform for modified ranges</param>
        public void ConsumeModifiedRegion(ulong address, ulong size, Action<ulong, ulong> rangeAction)
        {
            if (_modifiedRanges != null)
            {
                _modifiedRanges.GetRanges(address, size, rangeAction);
                _modifiedRanges.Clear(address, size);
            }
        }

        /// <summary>
        /// Gets data from the specified region of the buffer, and places it on <paramref name="output"/>.
        /// </summary>
        /// <param name="output">Span to put the data into</param>
        /// <param name="offset">Offset of the buffer to get the data from</param>
        /// <param name="size">Size of the data in bytes</param>
        public void GetData(Span<byte> output, int offset, int size)
        {
            using PinnedSpan<byte> data = _context.Renderer.GetBufferData(Handle, offset, size);
            data.Get().CopyTo(output);
        }

        /// <summary>
        /// Disposes the host buffer.
        /// </summary>
        public void Dispose()
        {
            if (_dependencies != null)
            {
                foreach (PhysicalDependency dependency in _dependencies)
                {
                    dependency.PhysicalBuffer.RemoveVirtualDependency(this);
                }

                _dependencies = null;
            }

            _context.Renderer.DeleteBuffer(Handle);
        }
    }
}
