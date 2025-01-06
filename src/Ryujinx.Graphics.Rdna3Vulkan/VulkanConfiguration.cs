namespace Ryujinx.Graphics.Rdna3Vulkan
{
    static class VulkanConfiguration
    {
        public const bool UseFastBufferUpdates = true;
        public const bool UseUnsafeBlit = true;
        public const bool UsePushDescriptors = true;

        public const bool ForceD24S8Unsupported = false;
        public const bool ForceRGB16IntFloatUnsupported = false;
    }
}
