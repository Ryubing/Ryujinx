using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gpu.Image
{
    /// <summary>
    /// Resource pool interface.
    /// </summary>
    /// <typeparam name="T">Resource pool type</typeparam>
    interface IPool<T>
    {
        /// <summary>
        /// Start address of the pool in memory.
        /// </summary>
        ulong Address { get; }

        /// <summary>
        /// Linked list node used on the texture pool cache.
        /// </summary>
        LinkedListNode<T> CacheNode { get; set; }

        /// <summary>
        /// Timestamp set on the last use of the pool by the cache.
        /// </summary>
        ulong CacheTimestamp { get; set; }
    }

    /// <summary>
    /// Pool cache.
    /// This can keep multiple pools, and return the current one as needed.
    /// </summary>
    abstract class PoolCache<T> : IDisposable where T : IPool<T>, IDisposable
    {
        private const int MaxCapacity = 2;
        private const ulong MinDeltaForRemoval = 20000;

        private readonly GpuContext _context;
        private readonly LinkedList<T> _pools;
        private ulong _currentTimestamp;

        /// <summary>
        /// Constructs a new instance of the pool.
        /// </summary>
        /// <param name="context">GPU context that the texture pool belongs to</param>
        public PoolCache(GpuContext context)
        {
            _context = context;
            _pools = [];
        }

        /// <summary>
        /// Increments the internal timestamp of the cache that is used to decide when old resources will be deleted.
        /// </summary>
        public void Tick()
        {
            _currentTimestamp++;
        }

        /// <summary>
        /// Finds a cache texture pool, or creates a new one if not found.
        /// </summary>
        /// <param name="channel">GPU channel that the texture pool cache belongs to</param>
        /// <param name="address">Start address of the texture pool</param>
        /// <param name="maximumId">Maximum ID of the texture pool</param>
        /// <param name="bindingsArrayCache">Cache of texture array bindings</param>
        /// <returns>The found or newly created texture pool</returns>
        public T FindOrCreate(GpuChannel channel, ulong address, int maximumId, TextureBindingsArrayCache bindingsArrayCache)
        {
            // Remove old entries from the cache, if possible.
            while (_pools.Count > MaxCapacity && (_currentTimestamp - _pools.First.Value.CacheTimestamp) >= MinDeltaForRemoval)
            {
                T oldestPool = _pools.First.Value;

                _pools.RemoveFirst();
                oldestPool.Dispose();
                oldestPool.CacheNode = null;
                bindingsArrayCache.RemoveAllWithPool(oldestPool);
            }

            T pool;

            // Try to find the pool on the cache.
            for (LinkedListNode<T> node = _pools.First; node != null; node = node.Next)
            {
                pool = node.Value;

                if (pool.Address == address)
                {
                    if (pool.CacheNode != _pools.Last)
                    {
                        _pools.Remove(pool.CacheNode);
                        _pools.AddLast(pool.CacheNode);
                    }

                    pool.CacheTimestamp = _currentTimestamp;

                    return pool;
                }
            }

            // If not found, create a new one.
            pool = CreatePool(_context, channel, address, maximumId);

            pool.CacheNode = _pools.AddLast(pool);
            pool.CacheTimestamp = _currentTimestamp;

            return pool;
        }

        /// <summary>
        /// Creates a new instance of the pool.
        /// </summary>
        /// <param name="context">GPU context that the pool belongs to</param>
        /// <param name="channel">GPU channel that the pool belongs to</param>
        /// <param name="address">Address of the pool in guest memory</param>
        /// <param name="maximumId">Maximum ID of the pool (equal to maximum minus one)</param>
        protected abstract T CreatePool(GpuContext context, GpuChannel channel, ulong address, int maximumId);

        public void Dispose()
        {
            foreach (T pool in _pools)
            {
                pool.Dispose();
                pool.CacheNode = null;
            }

            _pools.Clear();
        }
    }
}
