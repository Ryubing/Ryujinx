using System;
using System.Linq;

namespace Ryujinx.Graphics.Vulkan
{
    internal readonly struct RenderPassCacheKey : IRefEquatable<RenderPassCacheKey>
    {
        private readonly TextureView _depthStencil;
        private readonly TextureView[] _colors;

        public RenderPassCacheKey(TextureView depthStencil, TextureView[] colors)
        {
            _depthStencil = depthStencil;
            _colors = colors;
        }

        public override int GetHashCode()
        {
            HashCode hc = new();

            hc.Add(_depthStencil);

            if (_colors != null)
            {
                foreach (TextureView color in _colors)
                {
                    hc.Add(color);
                }
            }

            return hc.ToHashCode();
        }

        public bool Equals(ref RenderPassCacheKey other)
        {
            bool colorsNull = _colors == null;
            bool otherNull = other._colors == null;
            return other._depthStencil == _depthStencil &&
                colorsNull == otherNull &&
                (colorsNull || other._colors.SequenceEqual(_colors));
        }
    }
}
