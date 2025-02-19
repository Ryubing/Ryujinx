using Ryujinx.Graphics.GAL;
using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.OpenGL.Image
{
    class IntermediatePool : IDisposable
    {
        private readonly OpenGLRenderer _renderer;
        private readonly List<TextureView> _entries;

        public IntermediatePool(OpenGLRenderer renderer)
        {
            _renderer = renderer;
            _entries = [];
        }

        public TextureView GetOrCreateWithAtLeast(
            Target target,
            int blockWidth,
            int blockHeight,
            int bytesPerPixel,
            Format format,
            int width,
            int height,
            int depth,
            int levels,
            int samples)
        {
            TextureView entry;

            for (int i = 0; i < _entries.Count; i++)
            {
                entry = _entries[i];

                if (entry.Target == target && entry.Format == format && entry.Info.Samples == samples)
                {
                    if (entry.Width < width ||
                        entry.Height < height ||
                        entry.Info.Depth < depth ||
                        entry.Info.Levels < levels)
                    {
                        width = Math.Max(width, entry.Width);
                        height = Math.Max(height, entry.Height);
                        depth = Math.Max(depth, entry.Info.Depth);
                        levels = Math.Max(levels, entry.Info.Levels);

                        entry.Dispose();
                        entry = CreateNew(target, blockWidth, blockHeight, bytesPerPixel, format, width, height, depth, levels, samples);
                        _entries[i] = entry;
                    }

                    return entry;
                }
            }

            entry = CreateNew(target, blockWidth, blockHeight, bytesPerPixel, format, width, height, depth, levels, samples);
            _entries.Add(entry);

            return entry;
        }

        private TextureView CreateNew(
            Target target,
            int blockWidth,
            int blockHeight,
            int bytesPerPixel,
            Format format,
            int width,
            int height,
            int depth,
            int levels,
            int samples)
        {
            return (TextureView)_renderer.CreateTexture(new TextureCreateInfo(
                width,
                height,
                depth,
                levels,
                samples,
                blockWidth,
                blockHeight,
                bytesPerPixel,
                format,
                DepthStencilMode.Depth,
                target,
                SwizzleComponent.Red,
                SwizzleComponent.Green,
                SwizzleComponent.Blue,
                SwizzleComponent.Alpha));
        }

        public void Dispose()
        {
            foreach (TextureView entry in _entries)
            {
                entry.Dispose();
            }

            _entries.Clear();
        }
    }
}
