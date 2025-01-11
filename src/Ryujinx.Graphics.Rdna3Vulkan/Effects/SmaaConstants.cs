using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Rdna3Vulkan.Effects
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct SmaaConstants
    {
        public int QualityLow;
        public int QualityMedium;
        public int QualityHigh;
        public int QualityUltra;
        public float Width;
        public float Height;
    }
}
