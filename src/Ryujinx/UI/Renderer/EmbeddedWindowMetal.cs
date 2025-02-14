using Ryujinx.Common.Helper;
using SharpMetal.QuartzCore;
using System;

namespace Ryujinx.Ava.UI.Renderer
{
    public class EmbeddedWindowMetal : EmbeddedWindow
    {
        public CAMetalLayer CreateSurface()
        {
            if (OperatingSystem.IsMacOS() && RunningPlatform.IsArm)
            {
                return new CAMetalLayer(MetalLayer);
            }
            
            throw new NotSupportedException($"Cannot create a {nameof(CAMetalLayer)} without being on ARM Mac.");
        }
    }
}
