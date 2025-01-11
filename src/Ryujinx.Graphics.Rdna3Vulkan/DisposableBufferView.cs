using Silk.NET.Vulkan;
using System;

namespace Ryujinx.Graphics.Rdna3Vulkan
{
    readonly struct DisposableBufferView : IDisposable
    {
        private readonly Vk _api;
        private readonly Device _device;

        public BufferView Value { get; }

        public DisposableBufferView(Vk api, Device device, BufferView bufferView)
        {
            _api = api;
            _device = device;
            Value = bufferView;
        }

        public void Dispose()
        {
            _api.DestroyBufferView(_device, Value, Span<AllocationCallbacks>.Empty);
        }
    }
}
