using System;
using System.Collections.Generic;
using System.Numerics;

namespace Ryujinx.Graphics.Gpu.Shader.HashTable
{
    /// <summary>
    /// Partitioned hash table.
    /// </summary>
    /// <typeparam name="T">Hash table entry type</typeparam>
    class PartitionHashTable<T>
    {
        /// <summary>
        /// Hash table entry.
        /// </summary>
        private struct Entry
        {
            /// <summary>
            /// Hash <see cref="OwnSize"/> bytes of <see cref="Data"/>.
            /// </summary>
            public readonly uint Hash;

            /// <summary>
            /// If this entry is only a sub-region of <see cref="Data"/>, this indicates the size in bytes
            /// of that region. Otherwise, it should be zero.
            /// </summary>
            public readonly int OwnSize;

            /// <summary>
            /// Data used to compute the hash for this entry.
            /// </summary>
            /// <remarks>
            /// To avoid additional allocations, this might be a instance of the full entry data,
            /// and only a sub-region of it might be actually used by this entry. Such sub-region
            /// has its size indicated by <see cref="OwnSize"/> in this case.
            /// </remarks>
            public readonly byte[] Data;

            /// <summary>
            /// Item associated with this entry.
            /// </summary>
            public T Item;

            /// <summary>
            /// Indicates if the entry is partial, which means that this entry is only for a sub-region of the data.
            /// </summary>
            /// <remarks>
            /// Partial entries have no items associated with them. They just indicates that the data might be present on
            /// the table, and one must keep looking for the full entry on other tables of larger data size.
            /// </remarks>
            public readonly bool IsPartial => OwnSize != 0;

            /// <summary>
            /// Creates a new partial hash table entry.
            /// </summary>
            /// <param name="hash">Hash of the data</param>
            /// <param name="ownerData">Full data</param>
            /// <param name="ownSize">Size of the sub-region of data that belongs to this entry</param>
            public Entry(uint hash, byte[] ownerData, int ownSize)
            {
                Hash = hash;
                OwnSize = ownSize;
                Data = ownerData;
                Item = default;
            }

            /// <summary>
            /// Creates a new full hash table entry.
            /// </summary>
            /// <param name="hash">Hash of the data</param>
            /// <param name="data">Data</param>
            /// <param name="item">Item associated with this entry</param>
            public Entry(uint hash, byte[] data, T item)
            {
                Hash = hash;
                OwnSize = 0;
                Data = data;
                Item = item;
            }

            /// <summary>
            /// Gets the data for this entry, either full or partial.
            /// </summary>
            /// <returns>Data sub-region</returns>
            public readonly ReadOnlySpan<byte> GetData()
            {
                if (OwnSize != 0)
                {
                    return new ReadOnlySpan<byte>(Data)[..OwnSize];
                }

                return Data;
            }
        }

        /// <summary>
        /// Hash table bucket.
        /// </summary>
        private struct Bucket
        {
            /// <summary>
            /// Inline entry, to avoid allocations for the common single entry case.
            /// </summary>
            public Entry InlineEntry;

            /// <summary>
            /// List of additional entries for the not-so-common multiple entries case.
            /// </summary>
            public List<Entry> MoreEntries;
        }

        private Bucket[] _buckets;
        private int _count;

        /// <summary>
        /// Total amount of entries on the hash table.
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// Creates a new instance of the partitioned hash table.
        /// </summary>
        public PartitionHashTable()
        {
            _buckets = [];
        }

        /// <summary>
        /// Gets an item on the table, or adds a new one if not present.
        /// </summary>
        /// <param name="data">Data</param>
        /// <param name="dataHash">Hash of the data</param>
        /// <param name="item">Item to be added if not found</param>
        /// <returns>Existing item if found, or <paramref name="item"/> if not found</returns>
        public T GetOrAdd(byte[] data, uint dataHash, T item)
        {
            if (TryFindItem(dataHash, data, out T existingItem))
            {
                return existingItem;
            }

            Entry entry = new(dataHash, data, item);

            AddToBucket(dataHash, ref entry);

            return item;
        }

        /// <summary>
        /// Adds an item to the hash table.
        /// </summary>
        /// <param name="data">Data</param>
        /// <param name="dataHash">Hash of the data</param>
        /// <param name="item">Item to be added</param>
        /// <returns>True if the item was added, false due to an item associated with the data already being on the table</returns>
        public bool Add(byte[] data, uint dataHash, T item)
        {
            if (TryFindItem(dataHash, data, out _))
            {
                return false;
            }

            Entry entry = new(dataHash, data, item);

            AddToBucket(dataHash, ref entry);

            return true;
        }

        /// <summary>
        /// Adds a partial entry to the hash table.
        /// </summary>
        /// <param name="ownerData">Full data</param>
        /// <param name="ownSize">Size of the sub-region of <paramref name="ownerData"/> used by the partial entry</param>
        /// <returns>True if added, false otherwise</returns>
        public bool AddPartial(byte[] ownerData, int ownSize)
        {
            ReadOnlySpan<byte> data = new ReadOnlySpan<byte>(ownerData)[..ownSize];

            return AddPartial(ownerData, HashState.CalcHash(data), ownSize);
        }

        /// <summary>
        /// Adds a partial entry to the hash table.
        /// </summary>
        /// <param name="ownerData">Full data</param>
        /// <param name="dataHash">Hash of the data sub-region</param>
        /// <param name="ownSize">Size of the sub-region of <paramref name="ownerData"/> used by the partial entry</param>
        /// <returns>True if added, false otherwise</returns>
        public bool AddPartial(byte[] ownerData, uint dataHash, int ownSize)
        {
            ReadOnlySpan<byte> data = new ReadOnlySpan<byte>(ownerData)[..ownSize];

            if (TryFindItem(dataHash, data, out _))
            {
                return false;
            }

            Entry entry = new(dataHash, ownerData, ownSize);

            AddToBucket(dataHash, ref entry);

            return true;
        }

        /// <summary>
        /// Adds entry with a given hash to the table.
        /// </summary>
        /// <param name="dataHash">Hash of the entry</param>
        /// <param name="entry">Entry</param>
        private void AddToBucket(uint dataHash, ref Entry entry)
        {
            int pow2Count = GetPow2Count(++_count);
            if (pow2Count != _buckets.Length)
            {
                Rebuild(pow2Count);
            }

            ref Bucket bucket = ref GetBucketForHash(dataHash);

            AddToBucket(ref bucket, ref entry);
        }

        /// <summary>
        /// Adds an entry to a bucket.
        /// </summary>
        /// <param name="bucket">Bucket to add the entry into</param>
        /// <param name="entry">Entry to be added</param>
        private static void AddToBucket(ref Bucket bucket, ref Entry entry)
        {
            if (bucket.InlineEntry.Data == null)
            {
                bucket.InlineEntry = entry;
            }
            else
            {
                (bucket.MoreEntries ??= []).Add(entry);
            }
        }

        /// <summary>
        /// Creates partial entries on a new hash table for all existing full entries.
        /// </summary>
        /// <remarks>
        /// This should be called every time a new hash table is created, and there are hash
        /// tables with data sizes that are higher than that of the new table.
        /// This will then fill the new hash table with "partial" entries of full entries
        /// on the hash tables with higher size.
        /// </remarks>
        /// <param name="newTable">New hash table</param>
        /// <param name="newEntrySize">Size of the data on the new hash table</param>
        public void FillPartials(PartitionHashTable<T> newTable, int newEntrySize)
        {
            for (int i = 0; i < _buckets.Length; i++)
            {
                ref Bucket bucket = ref _buckets[i];
                ref Entry inlineEntry = ref bucket.InlineEntry;

                if (inlineEntry.Data != null)
                {
                    if (!inlineEntry.IsPartial)
                    {
                        newTable.AddPartial(inlineEntry.Data, newEntrySize);
                    }

                    if (bucket.MoreEntries != null)
                    {
                        foreach (Entry entry in bucket.MoreEntries)
                        {
                            if (entry.IsPartial)
                            {
                                continue;
                            }

                            newTable.AddPartial(entry.Data, newEntrySize);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Tries to find an item on the table.
        /// </summary>
        /// <param name="dataHash">Hash of <paramref name="data"/></param>
        /// <param name="data">Data to find</param>
        /// <param name="item">Item associated with the data</param>
        /// <returns>True if an item was found, false otherwise</returns>
        private bool TryFindItem(uint dataHash, ReadOnlySpan<byte> data, out T item)
        {
            if (_count == 0)
            {
                item = default;
                return false;
            }

            ref Bucket bucket = ref GetBucketForHash(dataHash);

            if (bucket.InlineEntry.Data != null)
            {
                if (bucket.InlineEntry.Hash == dataHash && bucket.InlineEntry.GetData().SequenceEqual(data))
                {
                    item = bucket.InlineEntry.Item;
                    return true;
                }

                if (bucket.MoreEntries != null)
                {
                    foreach (Entry entry in bucket.MoreEntries)
                    {
                        if (entry.Hash == dataHash && entry.GetData().SequenceEqual(data))
                        {
                            item = entry.Item;
                            return true;
                        }
                    }
                }
            }

            item = default;
            return false;
        }

        /// <summary>
        /// Indicates the result of a hash table lookup.
        /// </summary>
        public enum SearchResult
        {
            /// <summary>
            /// No entry was found, the search must continue on hash tables of lower size.
            /// </summary>
            NotFound,

            /// <summary>
            /// A partial entry was found, the search must continue on hash tables of higher size.
            /// </summary>
            FoundPartial,

            /// <summary>
            /// A full entry was found, the search was concluded and the item can be retrieved.
            /// </summary>
            FoundFull,
        }

        /// <summary>
        /// Tries to find an item on the table.
        /// </summary>
        /// <param name="dataAccessor">Data accessor</param>
        /// <param name="size">Size of the hash table data</param>
        /// <param name="item">The item on the table, if found, otherwise unmodified</param>
        /// <param name="data">The data on the table, if found, otherwise unmodified</param>
        /// <returns>Table lookup result</returns>
        public SearchResult TryFindItem(scoped ref SmartDataAccessor dataAccessor, int size, scoped ref T item, scoped ref byte[] data)
        {
            if (_count == 0)
            {
                return SearchResult.NotFound;
            }

            ReadOnlySpan<byte> dataSpan = dataAccessor.GetSpanAndHash(size, out uint dataHash);

            if (dataSpan.Length != size)
            {
                return SearchResult.NotFound;
            }

            ref Bucket bucket = ref GetBucketForHash(dataHash);

            if (bucket.InlineEntry.Data != null)
            {
                if (bucket.InlineEntry.Hash == dataHash && bucket.InlineEntry.GetData().SequenceEqual(dataSpan))
                {
                    item = bucket.InlineEntry.Item;
                    data = bucket.InlineEntry.Data;
                    return bucket.InlineEntry.IsPartial ? SearchResult.FoundPartial : SearchResult.FoundFull;
                }

                if (bucket.MoreEntries != null)
                {
                    foreach (Entry entry in bucket.MoreEntries)
                    {
                        if (entry.Hash == dataHash && entry.GetData().SequenceEqual(dataSpan))
                        {
                            item = entry.Item;
                            data = entry.Data;
                            return entry.IsPartial ? SearchResult.FoundPartial : SearchResult.FoundFull;
                        }
                    }
                }
            }

            return SearchResult.NotFound;
        }

        /// <summary>
        /// Rebuilds the table for a new count.
        /// </summary>
        /// <param name="newPow2Count">New power of two count of the table</param>
        private void Rebuild(int newPow2Count)
        {
            Bucket[] newBuckets = new Bucket[newPow2Count];

            uint mask = (uint)newPow2Count - 1;

            for (int i = 0; i < _buckets.Length; i++)
            {
                ref Bucket bucket = ref _buckets[i];

                if (bucket.InlineEntry.Data != null)
                {
                    AddToBucket(ref newBuckets[(int)(bucket.InlineEntry.Hash & mask)], ref bucket.InlineEntry);

                    if (bucket.MoreEntries != null)
                    {
                        foreach (Entry entry in bucket.MoreEntries)
                        {
                            Entry entryCopy = entry;
                            AddToBucket(ref newBuckets[(int)(entry.Hash & mask)], ref entryCopy);
                        }
                    }
                }
            }

            _buckets = newBuckets;
        }

        /// <summary>
        /// Gets the bucket for a given hash.
        /// </summary>
        /// <param name="hash">Data hash</param>
        /// <returns>Bucket for the hash</returns>
        private ref Bucket GetBucketForHash(uint hash)
        {
            int index = (int)(hash & (_buckets.Length - 1));

            return ref _buckets[index];
        }

        /// <summary>
        /// Gets a power of two count from a regular count.
        /// </summary>
        /// <param name="count">Count</param>
        /// <returns>Power of two count</returns>
        private static int GetPow2Count(int count)
        {
            // This returns the nearest power of two that is lower than count.
            // This was done to optimize memory usage rather than performance.
            return 1 << BitOperations.Log2((uint)count);
        }
    }
}
