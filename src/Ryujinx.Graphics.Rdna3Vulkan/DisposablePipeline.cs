using Silk.NET.Vulkan;
using System;

namespace Ryujinx.Graphics.Rdna3Vulkan
{
    readonly struct DisposablePipeline : IDisposable
    {
        private readonly Vk _api;
        private readonly Device _device;

        public Pipeline Value { get; }

        public DisposablePipeline(Vk api, Device device, Pipeline pipeline)
        {
            _api = api;
            _device = device;
            Value = pipeline;
        }

        public void Dispose()
        {
            _api.DestroyPipeline(_device, Value, Span<AllocationCallbacks>.Empty);
        }
    }
}
