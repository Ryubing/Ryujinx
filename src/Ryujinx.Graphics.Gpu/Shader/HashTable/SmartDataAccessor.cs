using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gpu.Shader.HashTable
{
    /// <summary>
    /// Smart data accessor that can cache data and hashes to avoid reading and re-hashing the same memory regions.
    /// </summary>
    ref struct SmartDataAccessor
    {
        private readonly IDataAccessor _dataAccessor;
        private ReadOnlySpan<byte> _data;
        private readonly SortedList<int, HashState> _cachedHashes;

        /// <summary>
        /// Creates a new smart data accessor.
        /// </summary>
        /// <param name="dataAccessor">Data accessor</param>
        public SmartDataAccessor(IDataAccessor dataAccessor)
        {
            _dataAccessor = dataAccessor;
            _data = [];
            _cachedHashes = [];
        }

        /// <summary>
        /// Get a spans of a given size.
        /// </summary>
        /// <remarks>
        /// The actual length of the span returned depends on the <see cref="IDataAccessor"/>
        /// and might be less than requested.
        /// </remarks>
        /// <param name="length">Size in bytes</param>
        /// <returns>Span with the requested size</returns>
        public ReadOnlySpan<byte> GetSpan(int length)
        {
            if (_data.Length < length)
            {
                _data = _dataAccessor.GetSpan(0, length);
            }
            else if (_data.Length > length)
            {
                return _data[..length];
            }

            return _data;
        }

        /// <summary>
        /// Gets a span of the requested size, and a hash of its data.
        /// </summary>
        /// <param name="length">Length of the span</param>
        /// <param name="hash">Hash of the span data</param>
        /// <returns>Span of data</returns>
        public ReadOnlySpan<byte> GetSpanAndHash(int length, out uint hash)
        {
            ReadOnlySpan<byte> data = GetSpan(length);
            hash = data.Length == length ? CalcHashCached(data) : 0;
            return data;
        }

        /// <summary>
        /// Calculates the hash for a requested span.
        /// This will try to use a cached hash if the data was already accessed before, to avoid re-hashing.
        /// </summary>
        /// <param name="data">Data to be hashed</param>
        /// <returns>Hash of the data</returns>
        private readonly uint CalcHashCached(ReadOnlySpan<byte> data)
        {
            HashState state = default;
            bool found = false;

            for (int i = _cachedHashes.Count - 1; i >= 0; i--)
            {
                int cachedHashSize = _cachedHashes.Keys[i];

                if (cachedHashSize < data.Length)
                {
                    state = _cachedHashes.Values[i];
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                state = new HashState();
                state.Initialize();
            }

            state.Continue(data);
            _cachedHashes[data.Length & ~7] = state;
            return state.Finalize(data);
        }
    }
}
