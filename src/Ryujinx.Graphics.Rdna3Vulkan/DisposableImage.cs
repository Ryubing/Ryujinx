using Silk.NET.Vulkan;
using System;

namespace Ryujinx.Graphics.Rdna3Vulkan
{
    readonly struct DisposableImage : IDisposable
    {
        private readonly Vk _api;
        private readonly Device _device;

        public Image Value { get; }

        public DisposableImage(Vk api, Device device, Image image)
        {
            _api = api;
            _device = device;
            Value = image;
        }

        public void Dispose()
        {
            _api.DestroyImage(_device, Value, Span<AllocationCallbacks>.Empty);
        }
    }
}
