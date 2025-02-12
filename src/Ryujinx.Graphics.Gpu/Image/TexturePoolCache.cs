using Ryujinx.Graphics.Gpu.Memory;

namespace Ryujinx.Graphics.Gpu.Image
{
    /// <summary>
    /// Texture pool cache.
    /// This can keep multiple texture pools, and return the current one as needed.
    /// It is useful for applications that uses multiple texture pools.
    /// </summary>
    class TexturePoolCache : PoolCache<TexturePool>
    {
        /// <summary>
        /// Constructs a new instance of the texture pool.
        /// </summary>
        /// <param name="context">GPU context that the texture pool belongs to</param>
        public TexturePoolCache(GpuContext context) : base(context)
        {
        }

        /// <summary>
        /// Creates a new instance of the texture pool.
        /// </summary>
        /// <param name="context">GPU context that the texture pool belongs to</param>
        /// <param name="channel">GPU channel that the texture pool belongs to</param>
        /// <param name="physicalMemory">Backing memory of the pool</param>
        /// <param name="address">Address of the texture pool in guest memory</param>
        /// <param name="maximumId">Maximum texture ID of the texture pool (equal to maximum textures minus one)</param>
        protected override TexturePool CreatePool(
            GpuContext context,
            GpuChannel channel,
            PhysicalMemory physicalMemory,
            ulong address,
            int maximumId)
        {
            return new TexturePool(context, channel, physicalMemory, address, maximumId);
        }
    }
}
