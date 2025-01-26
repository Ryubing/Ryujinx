using Ryujinx.Graphics.GAL;
using IndexType = Silk.NET.Vulkan.IndexType;

namespace Ryujinx.Graphics.Vulkan
{
    internal struct IndexBufferState
    {
        private const int IndexBufferMaxMirrorable = 0x20000;

        public static IndexBufferState Null => new(BufferHandle.Null, 0, 0);

        private readonly int _offset;
        private readonly int _size;
        private readonly IndexType _type;

        private readonly BufferHandle _handle;
        private Auto<DisposableBuffer> _buffer;

        public IndexBufferState(BufferHandle handle, int offset, int size, IndexType type)
        {
            _handle = handle;
            _offset = offset;
            _size = size;
            _type = type;
            _buffer = null;
        }

        public IndexBufferState(BufferHandle handle, int offset, int size)
        {
            _handle = handle;
            _offset = offset;
            _size = size;
            _type = IndexType.Uint16;
            _buffer = null;
        }

        public void BindIndexBuffer(VulkanRenderer gd, CommandBufferScoped cbs)
        {
            Auto<DisposableBuffer> autoBuffer;
            int offset, size;
            IndexType type = _type;
            bool mirrorable = false;

            if (_type == IndexType.Uint8Ext && !gd.Capabilities.SupportsIndexTypeUint8)
            {
                // Index type is not supported. Convert to I16.
                autoBuffer = gd.BufferManager.GetBufferI8ToI16(cbs, _handle, _offset, _size);

                type = IndexType.Uint16;
                offset = 0;
                size = _size * 2;
            }
            else
            {
                autoBuffer = gd.BufferManager.GetBuffer(cbs.CommandBuffer, _handle, false, out int bufferSize);

                if (_offset >= bufferSize)
                {
                    autoBuffer = null;
                }

                mirrorable = _size < IndexBufferMaxMirrorable;

                offset = _offset;
                size = _size;
            }

            _buffer = autoBuffer;

            if (autoBuffer != null)
            {
                DisposableBuffer buffer = mirrorable ? autoBuffer.GetMirrorable(cbs, ref offset, size, out _) : autoBuffer.Get(cbs, offset, size);

                gd.Api.CmdBindIndexBuffer(cbs.CommandBuffer, buffer.Value, (ulong)offset, type);
            }
        }

        public void BindConvertedIndexBuffer(
            VulkanRenderer gd,
            CommandBufferScoped cbs,
            int firstIndex,
            int indexCount,
            int convertedCount,
            IndexBufferPattern pattern)
        {
            Auto<DisposableBuffer> autoBuffer;

            // Convert the index buffer using the given pattern.
            int indexSize = GetIndexSize();

            int firstIndexOffset = firstIndex * indexSize;

            autoBuffer = gd.BufferManager.GetBufferTopologyConversion(cbs, _handle, _offset + firstIndexOffset, indexCount * indexSize, pattern, indexSize);

            int size = convertedCount * 4;

            _buffer = autoBuffer;

            if (autoBuffer != null)
            {
                gd.Api.CmdBindIndexBuffer(cbs.CommandBuffer, autoBuffer.Get(cbs, 0, size).Value, 0, IndexType.Uint32);
            }
        }

        public Auto<DisposableBuffer> BindConvertedIndexBufferIndirect(
            VulkanRenderer gd,
            CommandBufferScoped cbs,
            BufferRange indirectBuffer,
            BufferRange drawCountBuffer,
            IndexBufferPattern pattern,
            bool hasDrawCount,
            int maxDrawCount,
            int indirectDataStride)
        {
            // Convert the index buffer using the given pattern.
            int indexSize = GetIndexSize();

            (Auto<DisposableBuffer> indexBufferAuto, Auto<DisposableBuffer> indirectBufferAuto) = gd.BufferManager.GetBufferTopologyConversionIndirect(
                gd,
                cbs,
                new BufferRange(_handle, _offset, _size),
                indirectBuffer,
                drawCountBuffer,
                pattern,
                indexSize,
                hasDrawCount,
                maxDrawCount,
                indirectDataStride);

            int convertedCount = pattern.GetConvertedCount(_size / indexSize);
            int size = convertedCount * 4;

            _buffer = indexBufferAuto;

            if (indexBufferAuto != null)
            {
                gd.Api.CmdBindIndexBuffer(cbs.CommandBuffer, indexBufferAuto.Get(cbs, 0, size).Value, 0, IndexType.Uint32);
            }

            return indirectBufferAuto;
        }

        private readonly int GetIndexSize()
        {
            return _type switch
            {
                IndexType.Uint32 => 4,
                IndexType.Uint16 => 2,
                _ => 1,
            };
        }

        public readonly bool BoundEquals(Auto<DisposableBuffer> buffer)
        {
            return _buffer == buffer;
        }

        public void Swap(Auto<DisposableBuffer> from, Auto<DisposableBuffer> to)
        {
            if (_buffer == from)
            {
                _buffer = to;
            }
        }

        public readonly bool Overlaps(Auto<DisposableBuffer> buffer, int offset, int size)
        {
            return buffer == _buffer && offset < _offset + _size && offset + size > _offset;
        }
    }
}
