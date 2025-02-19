using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Shader;
using System;

namespace Ryujinx.Graphics.Gpu.Memory
{
    /// <summary>
    /// Buffer data updater.
    /// </summary>
    class BufferUpdater : IDisposable
    {
        private BufferHandle _handle;

        /// <summary>
        /// Handle of the buffer.
        /// </summary>
        public BufferHandle Handle => _handle;

        private readonly IRenderer _renderer;
        private int _startOffset = -1;
        private int _endOffset = -1;

        /// <summary>
        /// Creates a new instance of the buffer updater.
        /// </summary>
        /// <param name="renderer">Renderer that the buffer will be used with</param>
        public BufferUpdater(IRenderer renderer)
        {
            _renderer = renderer;
        }

        /// <summary>
        /// Mark a region of the buffer as modified and needing to be sent to the GPU.
        /// </summary>
        /// <param name="startOffset">Start offset of the region in bytes</param>
        /// <param name="byteSize">Size of the region in bytes</param>
        protected void MarkDirty(int startOffset, int byteSize)
        {
            int endOffset = startOffset + byteSize;

            if (_startOffset == -1)
            {
                _startOffset = startOffset;
                _endOffset = endOffset;
            }
            else
            {
                if (startOffset < _startOffset)
                {
                    _startOffset = startOffset;
                }

                if (endOffset > _endOffset)
                {
                    _endOffset = endOffset;
                }
            }
        }

        /// <summary>
        /// Submits all pending buffer updates to the GPU.
        /// </summary>
        /// <param name="data">All data that should be sent to the GPU. Only the modified regions will be updated</param>
        /// <param name="binding">Optional binding to bind the buffer if a new buffer was created</param>
        protected void Commit(ReadOnlySpan<byte> data, int binding = -1)
        {
            if (_startOffset != -1)
            {
                if (_handle == BufferHandle.Null)
                {
                    _handle = _renderer.CreateBuffer(data.Length, BufferAccess.Stream);
                    _renderer.Pipeline.ClearBuffer(_handle, 0, data.Length, 0);

                    if (binding >= 0)
                    {
                        BufferRange range = new(_handle, 0, data.Length);
                        _renderer.Pipeline.SetUniformBuffers([new BufferAssignment(0, range)]);
                    }
                };

                _renderer.SetBufferData(_handle, _startOffset, data[_startOffset.._endOffset]);

                _startOffset = -1;
                _endOffset = -1;
            }
        }

        /// <summary>
        /// Gets a reference to a given element of a vector.
        /// </summary>
        /// <param name="vector">Vector to get the element reference from</param>
        /// <param name="elementIndex">Element index</param>
        /// <returns>Reference to the specified element</returns>
        protected static ref T GetElementRef<T>(ref Vector4<T> vector, int elementIndex)
        {
            switch (elementIndex)
            {
                case 0:
                    return ref vector.X;
                case 1:
                    return ref vector.Y;
                case 2:
                    return ref vector.Z;
                case 3:
                    return ref vector.W;
                default:
                    throw new ArgumentOutOfRangeException(nameof(elementIndex));
            }
        }

        /// <summary>
        /// Destroys the buffer.
        /// </summary>
        public void Dispose()
        {
            if (_handle != BufferHandle.Null)
            {
                _renderer.DeleteBuffer(_handle);
                _handle = BufferHandle.Null;
            }
        }
    }
}
