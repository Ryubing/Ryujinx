using Ryujinx.Graphics.Gpu.Memory;

namespace Ryujinx.Graphics.Gpu.Image
{
    /// <summary>
    /// Sampler pool cache.
    /// This can keep multiple sampler pools, and return the current one as needed.
    /// It is useful for applications that uses multiple sampler pools.
    /// </summary>
    class SamplerPoolCache : PoolCache<SamplerPool>
    {
        /// <summary>
        /// Constructs a new instance of the texture pool.
        /// </summary>
        /// <param name="context">GPU context that the texture pool belongs to</param>
        public SamplerPoolCache(GpuContext context) : base(context)
        {
        }

        /// <summary>
        /// Creates a new instance of the sampler pool.
        /// </summary>
        /// <param name="context">GPU context that the sampler pool belongs to</param>
        /// <param name="channel">GPU channel that the texture pool belongs to</param>
        /// <param name="address">Address of the sampler pool in guest memory</param>
        /// <param name="maximumId">Maximum sampler ID of the sampler pool (equal to maximum samplers minus one)</param>
        protected override SamplerPool CreatePool(GpuContext context, GpuChannel channel, PhysicalMemory physicalMemory, ulong address, int maximumId)
        {
            return new SamplerPool(context, physicalMemory, address, maximumId);
        }
    }
}
