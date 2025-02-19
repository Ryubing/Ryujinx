using Ryujinx.Graphics.Gpu.Shader.HashTable;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gpu.Shader
{
    /// <summary>
    /// Compute shader cache hash table.
    /// </summary>
    class ComputeShaderCacheHashTable
    {
        private readonly PartitionedHashTable<ShaderSpecializationList> _cache;
        private readonly List<CachedShaderProgram> _shaderPrograms;

        /// <summary>
        /// Creates a new compute shader cache hash table.
        /// </summary>
        public ComputeShaderCacheHashTable()
        {
            _cache = new PartitionedHashTable<ShaderSpecializationList>();
            _shaderPrograms = [];
        }

        /// <summary>
        /// Adds a program to the cache.
        /// </summary>
        /// <param name="program">Program to be added</param>
        public void Add(CachedShaderProgram program)
        {
            ShaderSpecializationList specList = _cache.GetOrAdd(program.Shaders[0].Code, []);
            specList.Add(program);
            _shaderPrograms.Add(program);
        }

        /// <summary>
        /// Tries to find a cached program.
        /// </summary>
        /// <param name="channel">GPU channel</param>
        /// <param name="poolState">Texture pool state</param>
        /// <param name="computeState">Compute state</param>
        /// <param name="gpuVa">GPU virtual address of the compute shader</param>
        /// <param name="program">Cached host program for the given state, if found</param>
        /// <param name="cachedGuestCode">Cached guest code, if any found</param>
        /// <returns>True if a cached host program was found, false otherwise</returns>
        public bool TryFind(
            GpuChannel channel,
            GpuChannelPoolState poolState,
            GpuChannelComputeState computeState,
            ulong gpuVa,
            out CachedShaderProgram program,
            out byte[] cachedGuestCode)
        {
            program = null;
            ShaderCodeAccessor codeAccessor = new(channel.MemoryManager, gpuVa);
            bool hasSpecList = _cache.TryFindItem(codeAccessor, out ShaderSpecializationList specList, out cachedGuestCode);

            return hasSpecList && specList.TryFindForCompute(channel, poolState, computeState, out program);
        }

        /// <summary>
        /// Gets all programs that have been added to the table.
        /// </summary>
        /// <returns>Programs added to the table</returns>
        public IEnumerable<CachedShaderProgram> GetPrograms()
        {
            foreach (CachedShaderProgram program in _shaderPrograms)
            {
                yield return program;
            }
        }
    }
}
