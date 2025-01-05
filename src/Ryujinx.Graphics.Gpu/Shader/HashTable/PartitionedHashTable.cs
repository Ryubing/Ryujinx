using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Ryujinx.Graphics.Gpu.Shader.HashTable
{
    /// <summary>
    /// Partitioned hash table.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PartitionedHashTable<T>
    {
        /// <summary>
        /// Entry for a given data size.
        /// </summary>
        private readonly struct SizeEntry
        {
            /// <summary>
            /// Size for the data that will be stored on the hash table on this entry.
            /// </summary>
            public int Size { get; }

            /// <summary>
            /// Number of entries on the hash table.
            /// </summary>
            public int TableCount => _table.Count;

            private readonly PartitionHashTable<T> _table;

            /// <summary>
            /// Creates an entry for a given size.
            /// </summary>
            /// <param name="size">Size of the data to be stored on this entry</param>
            public SizeEntry(int size)
            {
                Size = size;
                _table = new PartitionHashTable<T>();
            }

            /// <summary>
            /// Gets an item for existing data, or adds a new one.
            /// </summary>
            /// <param name="data">Data associated with the item</param>
            /// <param name="dataHash">Hash of <paramref name="data"/></param>
            /// <param name="item">Item to be added</param>
            /// <returns>Existing item, or <paramref name="item"/> if not present</returns>
            public T GetOrAdd(byte[] data, uint dataHash, T item)
            {
                Debug.Assert(data.Length == Size);
                return _table.GetOrAdd(data, dataHash, item);
            }

            /// <summary>
            /// Adds a new item.
            /// </summary>
            /// <param name="data">Data associated with the item</param>
            /// <param name="dataHash">Hash of <paramref name="data"/></param>
            /// <param name="item">Item to be added</param>
            /// <returns>True if added, false otherwise</returns>
            public bool Add(byte[] data, uint dataHash, T item)
            {
                Debug.Assert(data.Length == Size);
                return _table.Add(data, dataHash, item);
            }

            /// <summary>
            /// Adds a partial entry.
            /// </summary>
            /// <param name="ownerData">Full entry data</param>
            /// <param name="dataHash">Hash of the sub-region of the data that belongs to this entry</param>
            /// <returns>True if added, false otherwise</returns>
            public bool AddPartial(byte[] ownerData, uint dataHash)
            {
                return _table.AddPartial(ownerData, dataHash, Size);
            }

            /// <summary>
            /// Fills a new hash table with "partials" of existing full entries of higher size.
            /// </summary>
            /// <param name="newEntry">Entry with the new hash table</param>
            public void FillPartials(SizeEntry newEntry)
            {
                Debug.Assert(newEntry.Size < Size);
                _table.FillPartials(newEntry._table, newEntry.Size);
            }

            /// <summary>
            /// Tries to find an item on the hash table.
            /// </summary>
            /// <param name="dataAccessor">Data accessor</param>
            /// <param name="item">The item on the table, if found, otherwise unmodified</param>
            /// <param name="data">The data on the table, if found, otherwise unmodified</param>
            /// <returns>Table lookup result</returns>
            public PartitionHashTable<T>.SearchResult TryFindItem(scoped ref SmartDataAccessor dataAccessor, scoped ref T item, scoped ref byte[] data)
            {
                return _table.TryFindItem(ref dataAccessor, Size, ref item, ref data);
            }
        }

        private readonly List<SizeEntry> _sizeTable;

        /// <summary>
        /// Creates a new partitioned hash table.
        /// </summary>
        public PartitionedHashTable()
        {
            _sizeTable = [];
        }

        /// <summary>
        /// Adds a new item to the table.
        /// </summary>
        /// <param name="data">Data</param>
        /// <param name="item">Item associated with the data</param>
        public void Add(byte[] data, T item)
        {
            GetOrAdd(data, item);
        }

        /// <summary>
        /// Gets an existing item from the table, or adds a new one if not present.
        /// </summary>
        /// <param name="data">Data</param>
        /// <param name="item">Item associated with the data</param>
        /// <returns>Existing item, or <paramref name="item"/> if not present</returns>
        public T GetOrAdd(byte[] data, T item)
        {
            SizeEntry sizeEntry;

            int index = BinarySearch(_sizeTable, data.Length);
            if (index < _sizeTable.Count && _sizeTable[index].Size == data.Length)
            {
                sizeEntry = _sizeTable[index];
            }
            else
            {
                if (index < _sizeTable.Count && _sizeTable[index].Size < data.Length)
                {
                    index++;
                }

                sizeEntry = new SizeEntry(data.Length);

                _sizeTable.Insert(index, sizeEntry);

                for (int i = index + 1; i < _sizeTable.Count; i++)
                {
                    _sizeTable[i].FillPartials(sizeEntry);
                }
            }

            HashState hashState = new();
            hashState.Initialize();

            for (int i = 0; i < index; i++)
            {
                ReadOnlySpan<byte> dataSlice = new ReadOnlySpan<byte>(data)[.._sizeTable[i].Size];
                hashState.Continue(dataSlice);
                _sizeTable[i].AddPartial(data, hashState.Finalize(dataSlice));
            }

            hashState.Continue(data);
            return sizeEntry.GetOrAdd(data, hashState.Finalize(data), item);
        }

        /// <summary>
        /// Performs binary search on a list of hash tables, each one with a fixed data size.
        /// </summary>
        /// <param name="entries">List of hash tables</param>
        /// <param name="size">Size to search for</param>
        /// <returns>Index of the hash table with the given size, or nearest one otherwise</returns>
        private static int BinarySearch(List<SizeEntry> entries, int size)
        {
            int left = 0;
            int middle = 0;
            int right = entries.Count - 1;

            while (left <= right)
            {
                middle = left + ((right - left) >> 1);

                SizeEntry entry = entries[middle];

                if (size == entry.Size)
                {
                    break;
                }

                if (size < entry.Size)
                {
                    right = middle - 1;
                }
                else
                {
                    left = middle + 1;
                }
            }

            return middle;
        }

        /// <summary>
        /// Tries to find an item on the table.
        /// </summary>
        /// <param name="dataAccessor">Data accessor</param>
        /// <param name="item">Item, if found</param>
        /// <param name="data">Data, if found</param>
        /// <returns>True if the item was found on the table, false otherwise</returns>
        public bool TryFindItem(IDataAccessor dataAccessor, out T item, out byte[] data)
        {
            SmartDataAccessor sda = new(dataAccessor);

            item = default;
            data = null;

            int left = 0;
            int right = _sizeTable.Count;

            while (left != right)
            {
                int index = left + ((right - left) >> 1);

                PartitionHashTable<T>.SearchResult result = _sizeTable[index].TryFindItem(ref sda, ref item, ref data);

                if (result == PartitionHashTable<T>.SearchResult.FoundFull)
                {
                    return true;
                }

                if (result == PartitionHashTable<T>.SearchResult.NotFound)
                {
                    right = index;
                }
                else /* if (result == PartitionHashTable<T>.SearchResult.FoundPartial) */
                {
                    left = index + 1;
                }
            }

            data = null;
            return false;
        }
    }
}
