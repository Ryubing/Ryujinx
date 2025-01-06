namespace Ryujinx.Graphics.Rdna3Vulkan
{
    internal enum BufferAllocationType
    {
        Auto = 0,

        HostMappedNoCache,
        HostMapped,
        DeviceLocal,
        DeviceLocalMapped,
        Sparse,
    }
}
