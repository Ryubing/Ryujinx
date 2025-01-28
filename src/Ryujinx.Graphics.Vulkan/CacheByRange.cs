using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Vulkan
{
    interface ICacheKey : IDisposable
    {
        bool KeyEqual(ICacheKey other);
    }

    struct I8ToI16CacheKey : ICacheKey
    {
        // Used to notify the pipeline that bindings have invalidated on dispose.
        private readonly VulkanRenderer _gd;
        private Auto<DisposableBuffer> _buffer;

        public I8ToI16CacheKey(VulkanRenderer gd)
        {
            _gd = gd;
            _buffer = null;
        }

        public readonly bool KeyEqual(ICacheKey other)
        {
            return other is I8ToI16CacheKey;
        }

        public void SetBuffer(Auto<DisposableBuffer> buffer)
        {
            _buffer = buffer;
        }

        public readonly void Dispose()
        {
            _gd.PipelineInternal.DirtyIndexBuffer(_buffer);
        }
    }

    struct AlignedVertexBufferCacheKey : ICacheKey
    {
        private readonly int _stride;
        private readonly int _alignment;

        // Used to notify the pipeline that bindings have invalidated on dispose.
        private readonly VulkanRenderer _gd;
        private Auto<DisposableBuffer> _buffer;

        public AlignedVertexBufferCacheKey(VulkanRenderer gd, int stride, int alignment)
        {
            _gd = gd;
            _stride = stride;
            _alignment = alignment;
            _buffer = null;
        }

        public readonly bool KeyEqual(ICacheKey other)
        {
            return other is AlignedVertexBufferCacheKey entry &&
                entry._stride == _stride &&
                entry._alignment == _alignment;
        }

        public void SetBuffer(Auto<DisposableBuffer> buffer)
        {
            _buffer = buffer;
        }

        public readonly void Dispose()
        {
            _gd.PipelineInternal.DirtyVertexBuffer(_buffer);
        }
    }

    struct TopologyConversionCacheKey : ICacheKey
    {
        private readonly IndexBufferPattern _pattern;
        private readonly int _indexSize;

        // Used to notify the pipeline that bindings have invalidated on dispose.
        private readonly VulkanRenderer _gd;
        private Auto<DisposableBuffer> _buffer;

        public TopologyConversionCacheKey(VulkanRenderer gd, IndexBufferPattern pattern, int indexSize)
        {
            _gd = gd;
            _pattern = pattern;
            _indexSize = indexSize;
            _buffer = null;
        }

        public readonly bool KeyEqual(ICacheKey other)
        {
            return other is TopologyConversionCacheKey entry &&
                entry._pattern == _pattern &&
                entry._indexSize == _indexSize;
        }

        public void SetBuffer(Auto<DisposableBuffer> buffer)
        {
            _buffer = buffer;
        }

        public readonly void Dispose()
        {
            _gd.PipelineInternal.DirtyIndexBuffer(_buffer);
        }
    }

    readonly struct TopologyConversionIndirectCacheKey : ICacheKey
    {
        private readonly TopologyConversionCacheKey _baseKey;
        private readonly BufferHolder _indirectDataBuffer;
        private readonly int _indirectDataOffset;
        private readonly int _indirectDataSize;

        public TopologyConversionIndirectCacheKey(
            VulkanRenderer gd,
            IndexBufferPattern pattern,
            int indexSize,
            BufferHolder indirectDataBuffer,
            int indirectDataOffset,
            int indirectDataSize)
        {
            _baseKey = new TopologyConversionCacheKey(gd, pattern, indexSize);
            _indirectDataBuffer = indirectDataBuffer;
            _indirectDataOffset = indirectDataOffset;
            _indirectDataSize = indirectDataSize;
        }

        public bool KeyEqual(ICacheKey other)
        {
            return other is TopologyConversionIndirectCacheKey entry &&
                entry._baseKey.KeyEqual(_baseKey) &&
                entry._indirectDataBuffer == _indirectDataBuffer &&
                entry._indirectDataOffset == _indirectDataOffset &&
                entry._indirectDataSize == _indirectDataSize;
        }

        public void SetBuffer(Auto<DisposableBuffer> buffer)
        {
            _baseKey.SetBuffer(buffer);
        }

        public void Dispose()
        {
            _baseKey.Dispose();
        }
    }

    readonly struct IndirectDataCacheKey : ICacheKey
    {
        private readonly IndexBufferPattern _pattern;

        public IndirectDataCacheKey(IndexBufferPattern pattern)
        {
            _pattern = pattern;
        }

        public bool KeyEqual(ICacheKey other)
        {
            return other is IndirectDataCacheKey entry && entry._pattern == _pattern;
        }

        public void Dispose()
        {
        }
    }

    struct DrawCountCacheKey : ICacheKey
    {
        public readonly bool KeyEqual(ICacheKey other)
        {
            return other is DrawCountCacheKey;
        }

        public readonly void Dispose()
        {
        }
    }

    readonly struct Dependency
    {
        private readonly BufferHolder _buffer;
        private readonly int _offset;
        private readonly int _size;
        private readonly ICacheKey _key;

        public Dependency(BufferHolder buffer, int offset, int size, ICacheKey key)
        {
            _buffer = buffer;
            _offset = offset;
            _size = size;
            _key = key;
        }

        public void RemoveFromOwner()
        {
            _buffer.RemoveCachedConvertedBuffer(_offset, _size, _key);
        }
    }

    struct CacheByRange<T> where T : IDisposable
    {
        private struct Entry
        {
            public ICacheKey Key;
            public T Value;
            public List<Dependency> DependencyList;

            public Entry(ICacheKey key, T value)
            {
                Key = key;
                Value = value;
                DependencyList = null;
            }

            public readonly void InvalidateDependencies()
            {
                if (DependencyList != null)
                {
                    foreach (Dependency dependency in DependencyList)
                    {
                        dependency.RemoveFromOwner();
                    }

                    DependencyList.Clear();
                }
            }
        }

        private Dictionary<ulong, List<Entry>> _ranges;

        public void Add(int offset, int size, ICacheKey key, T value)
        {
            List<Entry> entries = GetEntries(offset, size);

            entries.Add(new Entry(key, value));
        }

        public void AddDependency(int offset, int size, ICacheKey key, Dependency dependency)
        {
            List<Entry> entries = GetEntries(offset, size);

            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];

                if (entry.Key.KeyEqual(key))
                {
                    if (entry.DependencyList == null)
                    {
                        entry.DependencyList = [];
                        entries[i] = entry;
                    }

                    entry.DependencyList.Add(dependency);

                    break;
                }
            }
        }

        public void Remove(int offset, int size, ICacheKey key)
        {
            List<Entry> entries = GetEntries(offset, size);

            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];

                if (entry.Key.KeyEqual(key))
                {
                    entries.RemoveAt(i--);

                    DestroyEntry(entry);
                }
            }

            if (entries.Count == 0)
            {
                _ranges.Remove(PackRange(offset, size));
            }
        }

        public bool TryGetValue(int offset, int size, ICacheKey key, out T value)
        {
            List<Entry> entries = GetEntries(offset, size);

            foreach (Entry entry in entries)
            {
                if (entry.Key.KeyEqual(key))
                {
                    value = entry.Value;

                    return true;
                }
            }

            value = default;
            return false;
        }

        public void Clear()
        {
            if (_ranges != null)
            {
                foreach (List<Entry> entries in _ranges.Values)
                {
                    foreach (Entry entry in entries)
                    {
                        DestroyEntry(entry);
                    }
                }

                _ranges.Clear();
                _ranges = null;
            }
        }

        public readonly void ClearRange(int offset, int size)
        {
            if (_ranges != null && _ranges.Count > 0)
            {
                int end = offset + size;

                List<ulong> toRemove = null;

                foreach (KeyValuePair<ulong, List<Entry>> range in _ranges)
                {
                    (int rOffset, int rSize) = UnpackRange(range.Key);

                    int rEnd = rOffset + rSize;

                    if (rEnd > offset && rOffset < end)
                    {
                        List<Entry> entries = range.Value;

                        foreach (Entry entry in entries)
                        {
                            DestroyEntry(entry);
                        }

                        (toRemove ??= []).Add(range.Key);
                    }
                }

                if (toRemove != null)
                {
                    foreach (ulong range in toRemove)
                    {
                        _ranges.Remove(range);
                    }
                }
            }
        }

        private List<Entry> GetEntries(int offset, int size)
        {
            _ranges ??= new Dictionary<ulong, List<Entry>>();

            ulong key = PackRange(offset, size);

            if (!_ranges.TryGetValue(key, out List<Entry> value))
            {
                value = [];
                _ranges.Add(key, value);
            }

            return value;
        }

        private static void DestroyEntry(Entry entry)
        {
            entry.Key.Dispose();
            entry.Value?.Dispose();
            entry.InvalidateDependencies();
        }

        private static ulong PackRange(int offset, int size)
        {
            return (uint)offset | ((ulong)size << 32);
        }

        private static (int offset, int size) UnpackRange(ulong range)
        {
            return ((int)range, (int)(range >> 32));
        }

        public void Dispose()
        {
            Clear();
        }
    }
}
