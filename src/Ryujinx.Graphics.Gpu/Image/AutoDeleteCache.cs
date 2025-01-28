using Ryujinx.Common.Logging;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gpu.Image
{
    /// <summary>
    /// An entry on the short duration texture cache.
    /// </summary>
    class ShortTextureCacheEntry
    {
        public bool IsAutoDelete;
        public readonly TextureDescriptor Descriptor;
        public readonly int InvalidatedSequence;
        public readonly Texture Texture;

        /// <summary>
        /// Create a new entry on the short duration texture cache.
        /// </summary>
        /// <param name="descriptor">Last descriptor that referenced the texture</param>
        /// <param name="texture">The texture</param>
        public ShortTextureCacheEntry(TextureDescriptor descriptor, Texture texture)
        {
            Descriptor = descriptor;
            InvalidatedSequence = texture.InvalidatedSequence;
            Texture = texture;
        }

        /// <summary>
        /// Create a new entry on the short duration texture cache from the auto delete cache.
        /// </summary>
        /// <param name="texture">The texture</param>
        public ShortTextureCacheEntry(Texture texture)
        {
            IsAutoDelete = true;
            InvalidatedSequence = texture.InvalidatedSequence;
            Texture = texture;
        }
    }

    /// <summary>
    /// A texture cache that automatically removes older textures that are not used for some time.
    /// The cache works with a rotated list with a fixed size. When new textures are added, the
    /// old ones at the bottom of the list are deleted.
    /// </summary>
    class AutoDeleteCache : IEnumerable<Texture>
    {
        private const int MinCountForDeletion = 32;
        private const int MaxCapacity = 2048;
        private const ulong GiB = 1024 * 1024 * 1024;
        private ulong MaxTextureSizeCapacity = 4UL * GiB;
        private const ulong MinTextureSizeCapacity = 512 * 1024 * 1024;
        private const ulong DefaultTextureSizeCapacity = 1 * GiB;
        private const ulong TextureSizeCapacity6GiB = 4 * GiB;
        private const ulong TextureSizeCapacity8GiB = 6 * GiB;
        private const ulong TextureSizeCapacity12GiB = 12 * GiB;


        private const float MemoryScaleFactor = 0.50f;
        private ulong _maxCacheMemoryUsage = DefaultTextureSizeCapacity;

        private readonly LinkedList<Texture> _textures;
        private ulong _totalSize;

        private HashSet<ShortTextureCacheEntry> _shortCacheBuilder;
        private HashSet<ShortTextureCacheEntry> _shortCache;

        private readonly Dictionary<TextureDescriptor, ShortTextureCacheEntry> _shortCacheLookup;

        /// <summary>
        /// Initializes the cache, setting the maximum texture capacity for the specified GPU context.
        /// </summary>
        /// <remarks>
        /// If the backend GPU has 0 memory capacity, the cache size defaults to `DefaultTextureSizeCapacity`.
        /// 
        /// Reads the current Device total CPU Memory, to determine the maximum amount of Vram available. Capped to 50% of Current GPU Memory.
        /// </remarks>
        /// <param name="context">The GPU context that the cache belongs to</param>
        /// <param name="cpuMemorySize">The amount of physical CPU Memory Avaiable on the device.</param>
        public void Initialize(GpuContext context, ulong cpuMemorySize)
        {
            ulong cpuMemorySizeGiB = cpuMemorySize / GiB;

            if (cpuMemorySizeGiB < 6 || context.Capabilities.MaximumGpuMemory == 0)
            {
                _maxCacheMemoryUsage = DefaultTextureSizeCapacity;
                return;
            }
            else if (cpuMemorySizeGiB == 6)
            {
                MaxTextureSizeCapacity = TextureSizeCapacity6GiB;
            }
            else if (cpuMemorySizeGiB == 8)
            {
                MaxTextureSizeCapacity = TextureSizeCapacity8GiB;
            }
            else
            {
                MaxTextureSizeCapacity = TextureSizeCapacity12GiB;
            }

            ulong cacheMemory = (ulong)(context.Capabilities.MaximumGpuMemory * MemoryScaleFactor);

            _maxCacheMemoryUsage = Math.Clamp(cacheMemory, MinTextureSizeCapacity, MaxTextureSizeCapacity);

            Logger.Info?.Print(LogClass.Gpu, $"AutoDelete Cache Allocated VRAM : {_maxCacheMemoryUsage / GiB} GiB");
        }

        /// <summary>
        /// Creates a new instance of the automatic deletion cache.
        /// </summary>
        public AutoDeleteCache()
        {
            _textures = [];

            _shortCacheBuilder = [];
            _shortCache = [];

            _shortCacheLookup = new Dictionary<TextureDescriptor, ShortTextureCacheEntry>();
        }

        /// <summary>
        /// Adds a new texture to the cache, even if the texture added is already on the cache.
        /// </summary>
        /// <remarks>
        /// Using this method is only recommended if you know that the texture is not yet on the cache,
        /// otherwise it would store the same texture more than once.
        /// </remarks>
        /// <param name="texture">The texture to be added to the cache</param>
        public void Add(Texture texture)
        {
            _totalSize += texture.Size;

            texture.IncrementReferenceCount();
            texture.CacheNode = _textures.AddLast(texture);

            if (_textures.Count > MaxCapacity ||
                (_totalSize > _maxCacheMemoryUsage && _textures.Count >= MinCountForDeletion))
            {
                RemoveLeastUsedTexture();
            }
        }

        /// <summary>
        /// Adds a new texture to the cache, or just moves it to the top of the list if the
        /// texture is already on the cache.
        /// </summary>
        /// <remarks>
        /// Moving the texture to the top of the list prevents it from being deleted,
        /// as the textures on the bottom of the list are deleted when new ones are added.
        /// </remarks>
        /// <param name="texture">The texture to be added, or moved to the top</param>
        public void Lift(Texture texture)
        {
            if (texture.CacheNode != null)
            {
                if (texture.CacheNode != _textures.Last)
                {
                    _textures.Remove(texture.CacheNode);
                    _textures.AddLast(texture.CacheNode);
                }

                if (_totalSize > _maxCacheMemoryUsage && _textures.Count >= MinCountForDeletion)
                {
                    RemoveLeastUsedTexture();
                }
            }
            else
            {
                Add(texture);
            }
        }

        /// <summary>
        /// Removes the least used texture from the cache.
        /// </summary>
        private void RemoveLeastUsedTexture()
        {
            Texture oldestTexture = _textures.First.Value;

            _totalSize -= oldestTexture.Size;

            if (!oldestTexture.CheckModified(false))
            {
                // The texture must be flushed if it falls out of the auto delete cache.
                // Flushes out of the auto delete cache do not trigger write tracking,
                // as it is expected that other overlapping textures exist that have more up-to-date contents.

                oldestTexture.Group.SynchronizeDependents(oldestTexture);
                oldestTexture.FlushModified(false);
            }

            _textures.RemoveFirst();

            oldestTexture.DecrementReferenceCount();
            oldestTexture.CacheNode = null;
        }

        /// <summary>
        /// Removes a texture from the cache.
        /// </summary>
        /// <param name="texture">The texture to be removed from the cache</param>
        /// <param name="flush">True to remove the texture if it was on the cache</param>
        /// <returns>True if the texture was found and removed, false otherwise</returns>
        public bool Remove(Texture texture, bool flush)
        {
            if (texture.CacheNode == null)
            {
                return false;
            }

            // Remove our reference to this texture.
            if (flush)
            {
                texture.FlushModified(false);
            }

            _textures.Remove(texture.CacheNode);

            _totalSize -= texture.Size;

            texture.CacheNode = null;

            return texture.DecrementReferenceCount();
        }

        /// <summary>
        /// Attempt to find a texture on the short duration cache.
        /// </summary>
        /// <param name="descriptor">The texture descriptor</param>
        /// <returns>The texture if found, null otherwise</returns>
        public Texture FindShortCache(in TextureDescriptor descriptor)
        {
            if (_shortCacheLookup.Count > 0 && _shortCacheLookup.TryGetValue(descriptor, out ShortTextureCacheEntry entry))
            {
                if (entry.InvalidatedSequence == entry.Texture.InvalidatedSequence)
                {
                    return entry.Texture;
                }
                else
                {
                    _shortCacheLookup.Remove(descriptor);
                }
            }

            return null;
        }

        /// <summary>
        /// Removes a texture from the short duration cache.
        /// </summary>
        /// <param name="texture">Texture to remove from the short cache</param>
        public void RemoveShortCache(Texture texture)
        {
            bool removed = _shortCache.Remove(texture.ShortCacheEntry);
            removed |= _shortCacheBuilder.Remove(texture.ShortCacheEntry);

            if (removed)
            {
                texture.DecrementReferenceCount();

                if (!texture.ShortCacheEntry.IsAutoDelete)
                {
                    _shortCacheLookup.Remove(texture.ShortCacheEntry.Descriptor);
                }

                texture.ShortCacheEntry = null;
            }
        }

        /// <summary>
        /// Adds a texture to the short duration cache.
        /// It starts in the builder set, and it is moved into the deletion set on next process.
        /// </summary>
        /// <param name="texture">Texture to add to the short cache</param>
        /// <param name="descriptor">Last used texture descriptor</param>
        public void AddShortCache(Texture texture, ref TextureDescriptor descriptor)
        {
            ShortTextureCacheEntry entry = new(descriptor, texture);

            _shortCacheBuilder.Add(entry);
            _shortCacheLookup.Add(entry.Descriptor, entry);

            texture.ShortCacheEntry = entry;

            texture.IncrementReferenceCount();
        }

        /// <summary>
        /// Adds a texture to the short duration cache without a descriptor. This typically keeps it alive for two ticks.
        /// On expiry, it will be removed from the AutoDeleteCache.
        /// </summary>
        /// <param name="texture">Texture to add to the short cache</param>
        public void AddShortCache(Texture texture)
        {
            if (texture.ShortCacheEntry != null)
            {
                ShortTextureCacheEntry entry = new(texture);

                _shortCacheBuilder.Add(entry);

                texture.ShortCacheEntry = entry;

                texture.IncrementReferenceCount();
            }
        }

        /// <summary>
        /// Delete textures from the short duration cache.
        /// Moves the builder set to be deleted on next process.
        /// </summary>
        public void ProcessShortCache()
        {
            HashSet<ShortTextureCacheEntry> toRemove = _shortCache;

            foreach (ShortTextureCacheEntry entry in toRemove)
            {
                entry.Texture.DecrementReferenceCount();

                if (entry.IsAutoDelete)
                {
                    Remove(entry.Texture, false);
                }
                else
                {
                    _shortCacheLookup.Remove(entry.Descriptor);
                }

                entry.Texture.ShortCacheEntry = null;
            }

            toRemove.Clear();
            _shortCache = _shortCacheBuilder;
            _shortCacheBuilder = toRemove;
        }

        public IEnumerator<Texture> GetEnumerator()
        {
            return _textures.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _textures.GetEnumerator();
        }
    }
}
