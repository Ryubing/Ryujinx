using Ryujinx.Graphics.GAL;
using Silk.NET.Vulkan;

namespace Ryujinx.Graphics.Vulkan
{
    internal struct VertexBufferState
    {
        private const int VertexBufferMaxMirrorable = 0x20000;

        public static VertexBufferState Null => new(null, 0, 0, 0);

        private readonly int _offset;
        private readonly int _size;
        private readonly int _stride;

        private readonly BufferHandle _handle;
        private Auto<DisposableBuffer> _buffer;

        internal readonly int DescriptorIndex;
        internal int AttributeScalarAlignment;

        public VertexBufferState(Auto<DisposableBuffer> buffer, int descriptorIndex, int offset, int size, int stride = 0)
        {
            _buffer = buffer;
            _handle = BufferHandle.Null;

            _offset = offset;
            _size = size;
            _stride = stride;

            DescriptorIndex = descriptorIndex;
            AttributeScalarAlignment = 1;

            buffer?.IncrementReferenceCount();
        }

        public VertexBufferState(BufferHandle handle, int descriptorIndex, int offset, int size, int stride = 0)
        {
            // This buffer state may be rewritten at bind time, so it must be retrieved on bind.

            _buffer = null;
            _handle = handle;

            _offset = offset;
            _size = size;
            _stride = stride;

            DescriptorIndex = descriptorIndex;
            AttributeScalarAlignment = 1;
        }

        public void BindVertexBuffer(VulkanRenderer gd, CommandBufferScoped cbs, uint binding, ref PipelineState state, VertexBufferUpdater updater)
        {
            Auto<DisposableBuffer> autoBuffer = _buffer;

            if (_handle != BufferHandle.Null)
            {
                // May need to restride the vertex buffer.
                //
                // Fix divide by zero when recovering from missed draw (Oct. 16 2024)
                // (fixes crash in 'Baldo: The Guardian Owls' opening cutscene)
                if (gd.NeedsVertexBufferAlignment(AttributeScalarAlignment, out int alignment) && alignment != 0 && (_stride % alignment) != 0)
                {
                    autoBuffer = gd.BufferManager.GetAlignedVertexBuffer(cbs, _handle, _offset, _size, _stride, alignment);

                    if (autoBuffer != null)
                    {
                        int stride = (_stride + (alignment - 1)) & -alignment;
                        int newSize = (_size / _stride) * stride;

                        Buffer buffer = autoBuffer.Get(cbs, 0, newSize).Value;

                        updater.BindVertexBuffer(cbs, binding, buffer, 0, (ulong)newSize, (ulong)stride);

                        _buffer = autoBuffer;

                        state.Internal.VertexBindingDescriptions[DescriptorIndex].Stride = (uint)stride;
                    }

                    return;
                }

                autoBuffer = gd.BufferManager.GetBuffer(cbs.CommandBuffer, _handle, false, out int size);

                // The original stride must be reapplied in case it was rewritten.
                state.Internal.VertexBindingDescriptions[DescriptorIndex].Stride = (uint)_stride;

                if (_offset >= size)
                {
                    autoBuffer = null;
                }
            }

            if (autoBuffer != null)
            {
                int offset = _offset;
                bool mirrorable = _size <= VertexBufferMaxMirrorable;
                Buffer buffer = mirrorable ? autoBuffer.GetMirrorable(cbs, ref offset, _size, out _).Value : autoBuffer.Get(cbs, offset, _size).Value;

                updater.BindVertexBuffer(cbs, binding, buffer, (ulong)offset, (ulong)_size, (ulong)_stride);
            }
        }

        public readonly bool BoundEquals(Auto<DisposableBuffer> buffer)
        {
            return _buffer == buffer;
        }

        public readonly bool Overlaps(Auto<DisposableBuffer> buffer, int offset, int size)
        {
            return buffer == _buffer && offset < _offset + _size && offset + size > _offset;
        }

        public readonly bool Matches(Auto<DisposableBuffer> buffer, int descriptorIndex, int offset, int size, int stride = 0)
        {
            return _buffer == buffer && DescriptorIndex == descriptorIndex && _offset == offset && _size == size && _stride == stride;
        }

        public void Swap(Auto<DisposableBuffer> from, Auto<DisposableBuffer> to)
        {
            if (_buffer == from)
            {
                _buffer.DecrementReferenceCount();
                to.IncrementReferenceCount();

                _buffer = to;
            }
        }

        public readonly void Dispose()
        {
            // Only dispose if this buffer is not refetched on each bind.

            if (_handle == BufferHandle.Null)
            {
                _buffer?.DecrementReferenceCount();
            }
        }
    }
}
