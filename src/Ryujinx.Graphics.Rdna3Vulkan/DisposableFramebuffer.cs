using Silk.NET.Vulkan;
using System;

namespace Ryujinx.Graphics.Rdna3Vulkan
{
    readonly struct DisposableFramebuffer : IDisposable
    {
        private readonly Vk _api;
        private readonly Device _device;

        public Framebuffer Value { get; }

        public DisposableFramebuffer(Vk api, Device device, Framebuffer framebuffer)
        {
            _api = api;
            _device = device;
            Value = framebuffer;
        }

        public void Dispose()
        {
            _api.DestroyFramebuffer(_device, Value, Span<AllocationCallbacks>.Empty);
        }
    }
}
