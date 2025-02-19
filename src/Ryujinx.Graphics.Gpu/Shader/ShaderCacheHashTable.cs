using Ryujinx.Graphics.Gpu.Memory;
using Ryujinx.Graphics.Gpu.Shader.HashTable;
using Ryujinx.Graphics.Shader;
using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gpu.Shader
{
    /// <summary>
    /// Holds already cached code for a guest shader.
    /// </summary>
    struct CachedGraphicsGuestCode
    {
        public byte[] VertexACode;
        public byte[] VertexBCode;
        public byte[] TessControlCode;
        public byte[] TessEvaluationCode;
        public byte[] GeometryCode;
        public byte[] FragmentCode;

        /// <summary>
        /// Gets the guest code of a shader stage by its index.
        /// </summary>
        /// <param name="stageIndex">Index of the shader stage</param>
        /// <returns>Guest code, or null if not present</returns>
        public readonly byte[] GetByIndex(int stageIndex)
        {
            return stageIndex switch
            {
                1 => TessControlCode,
                2 => TessEvaluationCode,
                3 => GeometryCode,
                4 => FragmentCode,
                _ => VertexBCode,
            };
        }
    }

    /// <summary>
    /// Graphics shader cache hash table.
    /// </summary>
    class ShaderCacheHashTable
    {
        /// <summary>
        /// Shader ID cache.
        /// </summary>
        private struct IdCache
        {
            private PartitionedHashTable<int> _cache;
            private int _id;

            /// <summary>
            /// Initializes the state.
            /// </summary>
            public void Initialize()
            {
                _cache = new PartitionedHashTable<int>();
                _id = 0;
            }

            /// <summary>
            /// Adds guest code to the cache.
            /// </summary>
            /// <remarks>
            /// If the code was already cached, it will just return the existing ID.
            /// </remarks>
            /// <param name="code">Code to add</param>
            /// <returns>Unique ID for the guest code</returns>
            public int Add(byte[] code)
            {
                int id = ++_id;
                int cachedId = _cache.GetOrAdd(code, id);
                if (cachedId != id)
                {
                    --_id;
                }

                return cachedId;
            }

            /// <summary>
            /// Tries to find cached guest code.
            /// </summary>
            /// <param name="dataAccessor">Code accessor used to read guest code to find a match on the hash table</param>
            /// <param name="id">ID of the guest code, if found</param>
            /// <param name="data">Cached guest code, if found</param>
            /// <returns>True if found, false otherwise</returns>
            public readonly bool TryFind(IDataAccessor dataAccessor, out int id, out byte[] data)
            {
                return _cache.TryFindItem(dataAccessor, out id, out data);
            }
        }

        /// <summary>
        /// Guest code IDs of the guest shaders that when combined forms a single host program.
        /// </summary>
        private struct IdTable : IEquatable<IdTable>
        {
            public int VertexAId;
            public int VertexBId;
            public int TessControlId;
            public int TessEvaluationId;
            public int GeometryId;
            public int FragmentId;

            public readonly override bool Equals(object obj)
            {
                return obj is IdTable other && Equals(other);
            }

            public readonly bool Equals(IdTable other)
            {
                return other.VertexAId == VertexAId &&
                       other.VertexBId == VertexBId &&
                       other.TessControlId == TessControlId &&
                       other.TessEvaluationId == TessEvaluationId &&
                       other.GeometryId == GeometryId &&
                       other.FragmentId == FragmentId;
            }

            public readonly override int GetHashCode()
            {
                return HashCode.Combine(VertexAId, VertexBId, TessControlId, TessEvaluationId, GeometryId, FragmentId);
            }
        }

        private IdCache _vertexACache;
        private IdCache _vertexBCache;
        private IdCache _tessControlCache;
        private IdCache _tessEvaluationCache;
        private IdCache _geometryCache;
        private IdCache _fragmentCache;

        private readonly Dictionary<IdTable, ShaderSpecializationList> _shaderPrograms;

        /// <summary>
        /// Creates a new graphics shader cache hash table.
        /// </summary>
        public ShaderCacheHashTable()
        {
            _vertexACache.Initialize();
            _vertexBCache.Initialize();
            _tessControlCache.Initialize();
            _tessEvaluationCache.Initialize();
            _geometryCache.Initialize();
            _fragmentCache.Initialize();

            _shaderPrograms = new Dictionary<IdTable, ShaderSpecializationList>();
        }

        /// <summary>
        /// Adds a program to the cache.
        /// </summary>
        /// <param name="program">Program to be added</param>
        public void Add(CachedShaderProgram program)
        {
            IdTable idTable = new();

            foreach (CachedShaderStage shader in program.Shaders)
            {
                if (shader == null)
                {
                    continue;
                }

                if (shader.Info != null)
                {
                    switch (shader.Info.Stage)
                    {
                        case ShaderStage.Vertex:
                            idTable.VertexBId = _vertexBCache.Add(shader.Code);
                            break;
                        case ShaderStage.TessellationControl:
                            idTable.TessControlId = _tessControlCache.Add(shader.Code);
                            break;
                        case ShaderStage.TessellationEvaluation:
                            idTable.TessEvaluationId = _tessEvaluationCache.Add(shader.Code);
                            break;
                        case ShaderStage.Geometry:
                            idTable.GeometryId = _geometryCache.Add(shader.Code);
                            break;
                        case ShaderStage.Fragment:
                            idTable.FragmentId = _fragmentCache.Add(shader.Code);
                            break;
                    }
                }
                else
                {
                    idTable.VertexAId = _vertexACache.Add(shader.Code);
                }
            }

            if (!_shaderPrograms.TryGetValue(idTable, out ShaderSpecializationList specList))
            {
                specList = [];
                _shaderPrograms.Add(idTable, specList);
            }

            specList.Add(program);
        }

        /// <summary>
        /// Tries to find a cached program.
        /// </summary>
        /// <remarks>
        /// Even if false is returned, <paramref name="guestCode"/> might still contain cached guest code.
        /// This can be used to avoid additional allocations for guest code that was already cached.
        /// </remarks>
        /// <param name="channel">GPU channel</param>
        /// <param name="poolState">Texture pool state</param>
        /// <param name="graphicsState">Graphics state</param>
        /// <param name="addresses">Guest addresses of the shaders to find</param>
        /// <param name="program">Cached host program for the given state, if found</param>
        /// <param name="guestCode">Cached guest code, if any found</param>
        /// <returns>True if a cached host program was found, false otherwise</returns>
        public bool TryFind(
            GpuChannel channel,
            ref GpuChannelPoolState poolState,
            ref GpuChannelGraphicsState graphicsState,
            ShaderAddresses addresses,
            out CachedShaderProgram program,
            out CachedGraphicsGuestCode guestCode)
        {
            MemoryManager memoryManager = channel.MemoryManager;
            IdTable idTable = new();
            guestCode = new CachedGraphicsGuestCode();

            program = null;

            bool found = TryGetId(_vertexACache, memoryManager, addresses.VertexA, out idTable.VertexAId, out guestCode.VertexACode);
            found &= TryGetId(_vertexBCache, memoryManager, addresses.VertexB, out idTable.VertexBId, out guestCode.VertexBCode);
            found &= TryGetId(_tessControlCache, memoryManager, addresses.TessControl, out idTable.TessControlId, out guestCode.TessControlCode);
            found &= TryGetId(_tessEvaluationCache, memoryManager, addresses.TessEvaluation, out idTable.TessEvaluationId, out guestCode.TessEvaluationCode);
            found &= TryGetId(_geometryCache, memoryManager, addresses.Geometry, out idTable.GeometryId, out guestCode.GeometryCode);
            found &= TryGetId(_fragmentCache, memoryManager, addresses.Fragment, out idTable.FragmentId, out guestCode.FragmentCode);

            if (found && _shaderPrograms.TryGetValue(idTable, out ShaderSpecializationList specList))
            {
                return specList.TryFindForGraphics(channel, ref poolState, ref graphicsState, out program);
            }

            return false;
        }

        /// <summary>
        /// Tries to get the ID of a single cached shader stage.
        /// </summary>
        /// <param name="idCache">ID cache of the stage</param>
        /// <param name="memoryManager">GPU memory manager</param>
        /// <param name="baseAddress">Base address of the shader</param>
        /// <param name="id">ID, if found</param>
        /// <param name="data">Cached guest code, if found</param>
        /// <returns>True if a cached shader is found, false otherwise</returns>
        private static bool TryGetId(IdCache idCache, MemoryManager memoryManager, ulong baseAddress, out int id, out byte[] data)
        {
            if (baseAddress == 0)
            {
                id = 0;
                data = null;
                return true;
            }

            ShaderCodeAccessor codeAccessor = new(memoryManager, baseAddress);
            return idCache.TryFind(codeAccessor, out id, out data);
        }

        /// <summary>
        /// Gets all programs that have been added to the table.
        /// </summary>
        /// <returns>Programs added to the table</returns>
        public IEnumerable<CachedShaderProgram> GetPrograms()
        {
            foreach (ShaderSpecializationList specList in _shaderPrograms.Values)
            {
                foreach (CachedShaderProgram program in specList)
                {
                    yield return program;
                }
            }
        }
    }
}
