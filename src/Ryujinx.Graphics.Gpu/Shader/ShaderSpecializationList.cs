using System.Collections;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gpu.Shader
{
    /// <summary>
    /// List of cached shader programs that differs only by specialization state.
    /// </summary>
    class ShaderSpecializationList : IEnumerable<CachedShaderProgram>
    {
        private readonly List<CachedShaderProgram> _entries = [];

        /// <summary>
        /// Adds a program to the list.
        /// </summary>
        /// <param name="program">Program to be added</param>
        public void Add(CachedShaderProgram program)
        {
            _entries.Add(program);
        }

        /// <summary>
        /// Tries to find an existing 3D program on the cache.
        /// </summary>
        /// <param name="channel">GPU channel</param>
        /// <param name="poolState">Texture pool state</param>
        /// <param name="graphicsState">Graphics state</param>
        /// <param name="program">Cached program, if found</param>
        /// <returns>True if a compatible program is found, false otherwise</returns>
        public bool TryFindForGraphics(
            GpuChannel channel,
            ref GpuChannelPoolState poolState,
            ref GpuChannelGraphicsState graphicsState,
            out CachedShaderProgram program)
        {
            foreach (CachedShaderProgram entry in _entries)
            {
                bool vertexAsCompute = entry.VertexAsCompute != null;
                bool usesDrawParameters = entry.Shaders[1]?.Info.UsesDrawParameters ?? false;

                if (entry.SpecializationState.MatchesGraphics(
                    channel,
                    ref poolState,
                    ref graphicsState,
                    vertexAsCompute,
                    usesDrawParameters,
                    checkTextures: true))
                {
                    program = entry;
                    return true;
                }
            }

            program = default;
            return false;
        }

        /// <summary>
        /// Tries to find an existing compute program on the cache.
        /// </summary>
        /// <param name="channel">GPU channel</param>
        /// <param name="poolState">Texture pool state</param>
        /// <param name="computeState">Compute state</param>
        /// <param name="program">Cached program, if found</param>
        /// <returns>True if a compatible program is found, false otherwise</returns>
        public bool TryFindForCompute(GpuChannel channel, GpuChannelPoolState poolState, GpuChannelComputeState computeState, out CachedShaderProgram program)
        {
            foreach (CachedShaderProgram entry in _entries)
            {
                if (entry.SpecializationState.MatchesCompute(channel, ref poolState, computeState, true))
                {
                    program = entry;
                    return true;
                }
            }

            program = default;
            return false;
        }

        public IEnumerator<CachedShaderProgram> GetEnumerator()
        {
            return _entries.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
