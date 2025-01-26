using Ryujinx.Common;
using Ryujinx.Common.Collections;
using Ryujinx.Memory;
using System;
using System.Diagnostics;
using System.Threading;

namespace Ryujinx.Cpu.Jit.HostTracked
{
    readonly struct PrivateRange
    {
        public readonly MemoryBlock Memory;
        public readonly ulong Offset;
        public readonly ulong Size;

        public static PrivateRange Empty => new(null, 0, 0);

        public PrivateRange(MemoryBlock memory, ulong offset, ulong size)
        {
            Memory = memory;
            Offset = offset;
            Size = size;
        }
    }

    class AddressSpacePartition : IDisposable
    {
        public const ulong GuestPageSize = 0x1000;

        private const int DefaultBlockAlignment = 1 << 20;

        private enum MappingType : byte
        {
            None,
            Private,
        }

        private class Mapping : IntrusiveRedBlackTreeNode<Mapping>, IComparable<Mapping>, IComparable<ulong>
        {
            public ulong Address { get; private set; }
            public ulong Size { get; private set; }
            public ulong EndAddress => Address + Size;
            public MappingType Type { get; private set; }

            public Mapping(ulong address, ulong size, MappingType type)
            {
                Address = address;
                Size = size;
                Type = type;
            }

            public Mapping Split(ulong splitAddress)
            {
                ulong leftSize = splitAddress - Address;
                ulong rightSize = EndAddress - splitAddress;

                Mapping left = new(Address, leftSize, Type);

                Address = splitAddress;
                Size = rightSize;

                return left;
            }

            public void UpdateState(MappingType newType)
            {
                Type = newType;
            }

            public void Extend(ulong sizeDelta)
            {
                Size += sizeDelta;
            }

            public int CompareTo(Mapping other)
            {
                if (Address < other.Address)
                {
                    return -1;
                }
                else if (Address <= other.EndAddress - 1UL)
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            }

            public int CompareTo(ulong address)
            {
                if (address < Address)
                {
                    return -1;
                }
                else if (address <= EndAddress - 1UL)
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            }
        }

        private class PrivateMapping : IntrusiveRedBlackTreeNode<PrivateMapping>, IComparable<PrivateMapping>, IComparable<ulong>
        {
            public ulong Address { get; private set; }
            public ulong Size { get; private set; }
            public ulong EndAddress => Address + Size;
            public PrivateMemoryAllocation PrivateAllocation { get; private set; }

            public PrivateMapping(ulong address, ulong size, PrivateMemoryAllocation privateAllocation)
            {
                Address = address;
                Size = size;
                PrivateAllocation = privateAllocation;
            }

            public PrivateMapping Split(ulong splitAddress)
            {
                ulong leftSize = splitAddress - Address;
                ulong rightSize = EndAddress - splitAddress;

                Debug.Assert(leftSize > 0);
                Debug.Assert(rightSize > 0);

                (PrivateMemoryAllocation leftAllocation, PrivateAllocation) = PrivateAllocation.Split(leftSize);

                PrivateMapping left = new(Address, leftSize, leftAllocation);

                Address = splitAddress;
                Size = rightSize;

                return left;
            }

            public void Map(AddressSpacePartitionMultiAllocation baseBlock, ulong baseAddress, PrivateMemoryAllocation newAllocation)
            {
                baseBlock.MapView(newAllocation.Memory, newAllocation.Offset, Address - baseAddress, Size);
                PrivateAllocation = newAllocation;
            }

            public void Unmap(AddressSpacePartitionMultiAllocation baseBlock, ulong baseAddress)
            {
                if (PrivateAllocation.IsValid)
                {
                    baseBlock.UnmapView(PrivateAllocation.Memory, Address - baseAddress, Size);
                    PrivateAllocation.Dispose();
                }

                PrivateAllocation = default;
            }

            public void Extend(ulong sizeDelta)
            {
                Size += sizeDelta;
            }

            public int CompareTo(PrivateMapping other)
            {
                if (Address < other.Address)
                {
                    return -1;
                }
                else if (Address <= other.EndAddress - 1UL)
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            }

            public int CompareTo(ulong address)
            {
                if (address < Address)
                {
                    return -1;
                }
                else if (address <= EndAddress - 1UL)
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            }
        }

        private readonly MemoryBlock _backingMemory;
        private readonly AddressSpacePartitionMultiAllocation _baseMemory;
        private readonly PrivateMemoryAllocator _privateMemoryAllocator;

        private readonly AddressIntrusiveRedBlackTree<Mapping> _mappingTree;
        private readonly AddressIntrusiveRedBlackTree<PrivateMapping> _privateTree;

        private readonly ReaderWriterLockSlim _treeLock;

        private readonly ulong _hostPageSize;

        private ulong? _firstPagePa;
        private ulong? _lastPagePa;
        private ulong _cachedFirstPagePa;
        private ulong _cachedLastPagePa;
        private MemoryBlock _firstPageMemoryForUnmap;
        private ulong _firstPageOffsetForLateMap;
        private MemoryPermission _firstPageMemoryProtection;

        public ulong Address { get; }
        public ulong Size { get; }
        public ulong EndAddress => Address + Size;

        public AddressSpacePartition(AddressSpacePartitionAllocation baseMemory, MemoryBlock backingMemory, ulong address, ulong size)
        {
            _privateMemoryAllocator = new PrivateMemoryAllocator(DefaultBlockAlignment, MemoryAllocationFlags.Mirrorable);
            _mappingTree = new AddressIntrusiveRedBlackTree<Mapping>();
            _privateTree = new AddressIntrusiveRedBlackTree<PrivateMapping>();
            _treeLock = new ReaderWriterLockSlim();

            _mappingTree.Add(new Mapping(address, size, MappingType.None));
            _privateTree.Add(new PrivateMapping(address, size, default));

            _hostPageSize = MemoryBlock.GetPageSize();

            _backingMemory = backingMemory;
            _baseMemory = new(baseMemory);

            _cachedFirstPagePa = ulong.MaxValue;
            _cachedLastPagePa = ulong.MaxValue;

            Address = address;
            Size = size;
        }

        public bool IsEmpty()
        {
            _treeLock.EnterReadLock();

            try
            {
                Mapping map = _mappingTree.GetNode(Address);

                return map != null && map.Address == Address && map.Size == Size && map.Type == MappingType.None;
            }
            finally
            {
                _treeLock.ExitReadLock();
            }
        }

        public void Map(ulong va, ulong pa, ulong size)
        {
            Debug.Assert(va >= Address);
            Debug.Assert(va + size <= EndAddress);

            if (va == Address)
            {
                _firstPagePa = pa;
            }

            if (va <= EndAddress - GuestPageSize && va + size > EndAddress - GuestPageSize)
            {
                _lastPagePa = pa + ((EndAddress - GuestPageSize) - va);
            }

            Update(va, pa, size, MappingType.Private);
        }

        public void Unmap(ulong va, ulong size)
        {
            Debug.Assert(va >= Address);
            Debug.Assert(va + size <= EndAddress);

            if (va == Address)
            {
                _firstPagePa = null;
            }

            if (va <= EndAddress - GuestPageSize && va + size > EndAddress - GuestPageSize)
            {
                _lastPagePa = null;
            }

            Update(va, 0UL, size, MappingType.None);
        }

        public void ReprotectAligned(ulong va, ulong size, MemoryPermission protection)
        {
            Debug.Assert(va >= Address);
            Debug.Assert(va + size <= EndAddress);

            _baseMemory.Reprotect(va - Address, size, protection, false);

            if (va == Address)
            {
                _firstPageMemoryProtection = protection;
            }
        }

        public void Reprotect(
            ulong va,
            ulong size,
            MemoryPermission protection,
            AddressSpacePartitioned addressSpace,
            Action<ulong, nint, ulong> updatePtCallback)
        {
            if (_baseMemory.LazyInitMirrorForProtection(addressSpace, Address, Size, protection))
            {
                LateMap();
            }

            updatePtCallback(va, _baseMemory.GetPointerForProtection(va - Address, size, protection), size);
        }

        public nint GetPointer(ulong va, ulong size)
        {
            Debug.Assert(va >= Address);
            Debug.Assert(va + size <= EndAddress);

            return _baseMemory.GetPointer(va - Address, size);
        }

        public void InsertBridgeAtEnd(AddressSpacePartition partitionAfter, bool useProtectionMirrors)
        {
            ulong firstPagePa = partitionAfter?._firstPagePa ?? ulong.MaxValue;
            ulong lastPagePa = _lastPagePa ?? ulong.MaxValue;

            if (firstPagePa != _cachedFirstPagePa || lastPagePa != _cachedLastPagePa)
            {
                if (partitionAfter != null && partitionAfter._firstPagePa.HasValue)
                {
                    (MemoryBlock firstPageMemory, ulong firstPageOffset) = partitionAfter.GetFirstPageMemoryAndOffset();

                    _baseMemory.MapView(firstPageMemory, firstPageOffset, Size, _hostPageSize);

                    if (!useProtectionMirrors)
                    {
                        _baseMemory.Reprotect(Size, _hostPageSize, partitionAfter._firstPageMemoryProtection, throwOnFail: false);
                    }

                    _firstPageMemoryForUnmap = firstPageMemory;
                    _firstPageOffsetForLateMap = firstPageOffset;
                }
                else
                {
                    MemoryBlock firstPageMemoryForUnmap = _firstPageMemoryForUnmap;

                    if (firstPageMemoryForUnmap != null)
                    {
                        _baseMemory.UnmapView(firstPageMemoryForUnmap, Size, _hostPageSize);
                        _firstPageMemoryForUnmap = null;
                    }
                }

                _cachedFirstPagePa = firstPagePa;
                _cachedLastPagePa = lastPagePa;
            }
        }

        public void ReprotectBridge(MemoryPermission protection)
        {
            if (_firstPageMemoryForUnmap != null)
            {
                _baseMemory.Reprotect(Size, _hostPageSize, protection, throwOnFail: false);
            }
        }

        private (MemoryBlock, ulong) GetFirstPageMemoryAndOffset()
        {
            _treeLock.EnterReadLock();

            try
            {
                PrivateMapping map = _privateTree.GetNode(Address);

                if (map != null && map.PrivateAllocation.IsValid)
                {
                    return (map.PrivateAllocation.Memory, map.PrivateAllocation.Offset + (Address - map.Address));
                }
            }
            finally
            {
                _treeLock.ExitReadLock();
            }

            return (_backingMemory, _firstPagePa.Value);
        }

        public PrivateRange GetPrivateAllocation(ulong va)
        {
            _treeLock.EnterReadLock();

            try
            {
                PrivateMapping map = _privateTree.GetNode(va);

                if (map != null && map.PrivateAllocation.IsValid)
                {
                    return new(map.PrivateAllocation.Memory, map.PrivateAllocation.Offset + (va - map.Address), map.Size - (va - map.Address));
                }
            }
            finally
            {
                _treeLock.ExitReadLock();
            }

            return PrivateRange.Empty;
        }

        private void Update(ulong va, ulong pa, ulong size, MappingType type)
        {
            _treeLock.EnterWriteLock();

            try
            {
                Mapping map = _mappingTree.GetNode(va);

                Update(map, va, pa, size, type);
            }
            finally
            {
                _treeLock.ExitWriteLock();
            }
        }

        private Mapping Update(Mapping map, ulong va, ulong pa, ulong size, MappingType type)
        {
            ulong endAddress = va + size;

            for (; map != null; map = map.Successor)
            {
                if (map.Address < va)
                {
                    _mappingTree.Add(map.Split(va));
                }

                if (map.EndAddress > endAddress)
                {
                    Mapping newMap = map.Split(endAddress);
                    _mappingTree.Add(newMap);
                    map = newMap;
                }

                switch (type)
                {
                    case MappingType.None:
                        ulong alignment = _hostPageSize;

                        bool unmappedBefore = map.Predecessor == null ||
                            (map.Predecessor.Type == MappingType.None && map.Predecessor.Address <= BitUtils.AlignDown(va, alignment));

                        bool unmappedAfter = map.Successor == null ||
                            (map.Successor.Type == MappingType.None && map.Successor.EndAddress >= BitUtils.AlignUp(endAddress, alignment));

                        UnmapPrivate(va, size, unmappedBefore, unmappedAfter);
                        break;
                    case MappingType.Private:
                        MapPrivate(va, size);
                        break;
                }

                map.UpdateState(type);
                map = TryCoalesce(map);

                if (map.EndAddress >= endAddress)
                {
                    break;
                }
            }

            return map;
        }

        private Mapping TryCoalesce(Mapping map)
        {
            Mapping previousMap = map.Predecessor;
            Mapping nextMap = map.Successor;

            if (previousMap != null && CanCoalesce(previousMap, map))
            {
                previousMap.Extend(map.Size);
                _mappingTree.Remove(map);
                map = previousMap;
            }

            if (nextMap != null && CanCoalesce(map, nextMap))
            {
                map.Extend(nextMap.Size);
                _mappingTree.Remove(nextMap);
            }

            return map;
        }

        private static bool CanCoalesce(Mapping left, Mapping right)
        {
            return left.Type == right.Type;
        }

        private void MapPrivate(ulong va, ulong size)
        {
            ulong endAddress = va + size;

            ulong alignment = _hostPageSize;

            // Expand the range outwards based on page size to ensure that at least the requested region is mapped.
            ulong vaAligned = BitUtils.AlignDown(va, alignment);
            ulong endAddressAligned = BitUtils.AlignUp(endAddress, alignment);

            PrivateMapping map = _privateTree.GetNode(va);

            for (; map != null; map = map.Successor)
            {
                if (!map.PrivateAllocation.IsValid)
                {
                    if (map.Address < vaAligned)
                    {
                        _privateTree.Add(map.Split(vaAligned));
                    }

                    if (map.EndAddress > endAddressAligned)
                    {
                        PrivateMapping newMap = map.Split(endAddressAligned);
                        _privateTree.Add(newMap);
                        map = newMap;
                    }

                    map.Map(_baseMemory, Address, _privateMemoryAllocator.Allocate(map.Size, _hostPageSize));
                }

                if (map.EndAddress >= endAddressAligned)
                {
                    break;
                }
            }
        }

        private void UnmapPrivate(ulong va, ulong size, bool unmappedBefore, bool unmappedAfter)
        {
            ulong endAddress = va + size;

            ulong alignment = _hostPageSize;

            // If the adjacent mappings are unmapped, expand the range outwards,
            // otherwise shrink it inwards. We must ensure we won't unmap pages that might still be in use.
            ulong vaAligned = unmappedBefore ? BitUtils.AlignDown(va, alignment) : BitUtils.AlignUp(va, alignment);
            ulong endAddressAligned = unmappedAfter ? BitUtils.AlignUp(endAddress, alignment) : BitUtils.AlignDown(endAddress, alignment);

            if (endAddressAligned <= vaAligned)
            {
                return;
            }

            PrivateMapping map = _privateTree.GetNode(vaAligned);

            for (; map != null; map = map.Successor)
            {
                if (map.PrivateAllocation.IsValid)
                {
                    if (map.Address < vaAligned)
                    {
                        _privateTree.Add(map.Split(vaAligned));
                    }

                    if (map.EndAddress > endAddressAligned)
                    {
                        PrivateMapping newMap = map.Split(endAddressAligned);
                        _privateTree.Add(newMap);
                        map = newMap;
                    }

                    map.Unmap(_baseMemory, Address);
                    map = TryCoalesce(map);
                }

                if (map.EndAddress >= endAddressAligned)
                {
                    break;
                }
            }
        }

        private PrivateMapping TryCoalesce(PrivateMapping map)
        {
            PrivateMapping previousMap = map.Predecessor;
            PrivateMapping nextMap = map.Successor;

            if (previousMap != null && CanCoalesce(previousMap, map))
            {
                previousMap.Extend(map.Size);
                _privateTree.Remove(map);
                map = previousMap;
            }

            if (nextMap != null && CanCoalesce(map, nextMap))
            {
                map.Extend(nextMap.Size);
                _privateTree.Remove(nextMap);
            }

            return map;
        }

        private static bool CanCoalesce(PrivateMapping left, PrivateMapping right)
        {
            return !left.PrivateAllocation.IsValid && !right.PrivateAllocation.IsValid;
        }

        private void LateMap()
        {
            // Map all existing private allocations.
            // This is necessary to ensure mirrors that are lazily created have the same mappings as the main one.

            PrivateMapping map = _privateTree.GetNode(Address);

            for (; map != null; map = map.Successor)
            {
                if (map.PrivateAllocation.IsValid)
                {
                    _baseMemory.LateMapView(map.PrivateAllocation.Memory, map.PrivateAllocation.Offset, map.Address - Address, map.Size);
                }
            }

            MemoryBlock firstPageMemory = _firstPageMemoryForUnmap;
            ulong firstPageOffset = _firstPageOffsetForLateMap;

            if (firstPageMemory != null)
            {
                _baseMemory.LateMapView(firstPageMemory, firstPageOffset, Size, _hostPageSize);
            }
        }

        public PrivateRange GetFirstPrivateAllocation(ulong va, ulong size, out ulong nextVa)
        {
            _treeLock.EnterReadLock();

            try
            {
                PrivateMapping map = _privateTree.GetNode(va);

                nextVa = map.EndAddress;

                if (map != null && map.PrivateAllocation.IsValid)
                {
                    ulong startOffset = va - map.Address;

                    return new(
                        map.PrivateAllocation.Memory,
                        map.PrivateAllocation.Offset + startOffset,
                        Math.Min(map.PrivateAllocation.Size - startOffset, size));
                }
            }
            finally
            {
                _treeLock.ExitReadLock();
            }

            return PrivateRange.Empty;
        }

        public bool HasPrivateAllocation(ulong va, ulong size, ulong startVa, ulong startSize, ref PrivateRange range)
        {
            ulong endVa = va + size;

            _treeLock.EnterReadLock();

            try
            {
                for (PrivateMapping map = _privateTree.GetNode(va); map != null && map.Address < endVa; map = map.Successor)
                {
                    if (map.PrivateAllocation.IsValid)
                    {
                        if (map.Address <= startVa && map.EndAddress >= startVa + startSize)
                        {
                            ulong startOffset = startVa - map.Address;

                            range = new(
                                map.PrivateAllocation.Memory,
                                map.PrivateAllocation.Offset + startOffset,
                                Math.Min(map.PrivateAllocation.Size - startOffset, startSize));
                        }

                        return true;
                    }
                }
            }
            finally
            {
                _treeLock.ExitReadLock();
            }

            return false;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            _privateMemoryAllocator.Dispose();
            _baseMemory.Dispose();
        }
    }
}
