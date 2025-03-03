using SharpMetal.Metal;
using System;
using System.Runtime.Versioning;

namespace Ryujinx.Graphics.Metal
{
    [SupportedOSPlatform("macos")]
    readonly struct DisposableSampler : IDisposable
    {
        public MTLSamplerState Value { get; }

        public DisposableSampler(MTLSamplerState sampler)
        {
            Value = sampler;
        }

        public void Dispose()
        {
            Value.Dispose();
        }
    }
}
