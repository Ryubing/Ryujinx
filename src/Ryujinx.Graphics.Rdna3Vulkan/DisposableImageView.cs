using Silk.NET.Vulkan;
using System;

namespace Ryujinx.Graphics.Rdna3Vulkan
{
    readonly struct DisposableImageView : IDisposable
    {
        private readonly Vk _api;
        private readonly Device _device;

        public ImageView Value { get; }

        public DisposableImageView(Vk api, Device device, ImageView imageView)
        {
            _api = api;
            _device = device;
            Value = imageView;
        }

        public void Dispose()
        {
            _api.DestroyImageView(_device, Value, Span<AllocationCallbacks>.Empty);
        }
    }
}
