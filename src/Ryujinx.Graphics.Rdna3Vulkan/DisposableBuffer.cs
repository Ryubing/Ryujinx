using Silk.NET.Vulkan;
using System;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Ryujinx.Graphics.Rdna3Vulkan
{
    readonly struct DisposableBuffer : IDisposable
    {
        private readonly Vk _api;
        private readonly Device _device;

        public Buffer Value { get; }

        public DisposableBuffer(Vk api, Device device, Buffer buffer)
        {
            _api = api;
            _device = device;
            Value = buffer;
        }

        public void Dispose()
        {
            _api.DestroyBuffer(_device, Value, Span<AllocationCallbacks>.Empty);
        }
    }
}
