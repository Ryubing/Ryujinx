using Silk.NET.Vulkan;
using System;

namespace Ryujinx.Graphics.Rdna3Vulkan
{
    readonly struct DisposableSampler : IDisposable
    {
        private readonly Vk _api;
        private readonly Device _device;

        public Sampler Value { get; }

        public DisposableSampler(Vk api, Device device, Sampler sampler)
        {
            _api = api;
            _device = device;
            Value = sampler;
        }

        public void Dispose()
        {
            _api.DestroySampler(_device, Value, Span<AllocationCallbacks>.Empty);
        }
    }
}
