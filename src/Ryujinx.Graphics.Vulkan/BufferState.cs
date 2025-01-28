using System;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Ryujinx.Graphics.Vulkan
{
    struct BufferState : IDisposable
    {
        public static BufferState Null => new(null, 0, 0);

        private readonly int _offset;
        private readonly int _size;

        private Auto<DisposableBuffer> _buffer;

        public BufferState(Auto<DisposableBuffer> buffer, int offset, int size)
        {
            _buffer = buffer;
            _offset = offset;
            _size = size;
            buffer?.IncrementReferenceCount();
        }

        public readonly void BindTransformFeedbackBuffer(VulkanRenderer gd, CommandBufferScoped cbs, uint binding)
        {
            if (_buffer != null)
            {
                Buffer buffer = _buffer.Get(cbs, _offset, _size, true).Value;

                ulong offset = (ulong)_offset;
                ulong size = (ulong)_size;

                gd.TransformFeedbackApi.CmdBindTransformFeedbackBuffers(cbs.CommandBuffer, binding, 1, in buffer, in offset, in size);
            }
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

        public readonly bool Overlaps(Auto<DisposableBuffer> buffer, int offset, int size)
        {
            return buffer == _buffer && offset < _offset + _size && offset + size > _offset;
        }

        public readonly void Dispose()
        {
            _buffer?.DecrementReferenceCount();
        }
    }
}
