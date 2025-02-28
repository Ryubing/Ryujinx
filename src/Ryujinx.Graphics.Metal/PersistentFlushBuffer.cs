using Ryujinx.Graphics.GAL;
using SharpMetal.Metal;
using System;
using System.Runtime.Versioning;

namespace Ryujinx.Graphics.Metal
{
    [SupportedOSPlatform("macos")]
    internal class PersistentFlushBuffer : IDisposable
    {
        private readonly MetalRenderer _renderer;

        private BufferHolder _flushStorage;

        public PersistentFlushBuffer(MetalRenderer renderer)
        {
            _renderer = renderer;
        }

        private BufferHolder ResizeIfNeeded(int size)
        {
            BufferHolder flushStorage = _flushStorage;

            if (flushStorage == null || size > _flushStorage.Size)
            {
                flushStorage?.Dispose();

                flushStorage = _renderer.BufferManager.Create(size);
                _flushStorage = flushStorage;
            }

            return flushStorage;
        }

        public Span<byte> GetBufferData(CommandBufferPool cbp, BufferHolder buffer, int offset, int size)
        {
            BufferHolder flushStorage = ResizeIfNeeded(size);
            Auto<DisposableBuffer> srcBuffer;

            using (CommandBufferScoped cbs = cbp.Rent())
            {
                srcBuffer = buffer.GetBuffer();
                Auto<DisposableBuffer> dstBuffer = flushStorage.GetBuffer();

                if (srcBuffer.TryIncrementReferenceCount())
                {
                    BufferHolder.Copy(cbs, srcBuffer, dstBuffer, offset, 0, size, registerSrcUsage: false);
                }
                else
                {
                    // Source buffer is no longer alive, don't copy anything to flush storage.
                    srcBuffer = null;
                }
            }

            flushStorage.WaitForFences();
            srcBuffer?.DecrementReferenceCount();
            return flushStorage.GetDataStorage(0, size);
        }

        public Span<byte> GetTextureData(CommandBufferPool cbp, Texture view, int size)
        {
            TextureCreateInfo info = view.Info;

            BufferHolder flushStorage = ResizeIfNeeded(size);

            using (CommandBufferScoped cbs = cbp.Rent())
            {
                MTLBuffer buffer = flushStorage.GetBuffer().Get(cbs).Value;
                MTLTexture image = view.GetHandle();

                view.CopyFromOrToBuffer(cbs, buffer, image, size, true, 0, 0, info.GetLayers(), info.Levels, singleSlice: false);
            }

            flushStorage.WaitForFences();
            return flushStorage.GetDataStorage(0, size);
        }

        public Span<byte> GetTextureData(CommandBufferPool cbp, Texture view, int size, int layer, int level)
        {
            BufferHolder flushStorage = ResizeIfNeeded(size);

            using (CommandBufferScoped cbs = cbp.Rent())
            {
                MTLBuffer buffer = flushStorage.GetBuffer().Get(cbs).Value;
                MTLTexture image = view.GetHandle();

                view.CopyFromOrToBuffer(cbs, buffer, image, size, true, layer, level, 1, 1, singleSlice: true);
            }

            flushStorage.WaitForFences();
            return flushStorage.GetDataStorage(0, size);
        }

        public void Dispose()
        {
            _flushStorage.Dispose();
        }
    }
}
