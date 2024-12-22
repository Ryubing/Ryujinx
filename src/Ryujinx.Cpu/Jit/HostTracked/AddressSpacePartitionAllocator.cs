using Ryujinx.Common;
using Ryujinx.Common.Collections;
using Ryujinx.Memory;
using Ryujinx.Memory.Tracking;
using System;

namespace Ryujinx.Cpu.Jit.HostTracked
{
    readonly struct AddressSpacePartitionAllocation : IDisposable
    {
        private readonly AddressSpacePartitionAllocator _owner;
        private readonly PrivateMemoryAllocatorImpl<AddressSpacePartitionAllocator.Block>.Allocation _allocation;

        public nint Pointer => (nint)((ulong)_allocation.Block.Memory.Pointer + _allocation.Offset);

        public bool IsValid => _owner != null;

        public AddressSpacePartitionAllocation(
            AddressSpacePartitionAllocator owner,
            PrivateMemoryAllocatorImpl<AddressSpacePartitionAllocator.Block>.Allocation allocation)
        {
            _owner = owner;
            _allocation = allocation;
        }

        public void RegisterMapping(ulong va, ulong endVa)
        {
            _allocation.Block.AddMapping(_allocation.Offset, _allocation.Size, va, endVa);
        }

        public void MapView(MemoryBlock srcBlock, ulong srcOffset, ulong dstOffset, ulong size)
        {
            _allocation.Block.Memory.MapView(srcBlock, srcOffset, _allocation.Offset + dstOffset, size);
        }

        public void UnmapView(MemoryBlock srcBlock, ulong offset, ulong size)
        {
            _allocation.Block.Memory.UnmapView(srcBlock, _allocation.Offset + offset, size);
        }

        public void Reprotect(ulong offset, ulong size, MemoryPermission permission, bool throwOnFail)
        {
            _allocation.Block.Memory.Reprotect(_allocation.Offset + offset, size, permission, throwOnFail);
        }

        public nint GetPointer(ulong offset, ulong size)
        {
            return _allocation.Block.Memory.GetPointer(_allocation.Offset + offset, size);
        }

        public void Dispose()
        {
            _allocation.Block.RemoveMapping(_allocation.Offset, _allocation.Size);
            _owner.Free(_allocation.Block, _allocation.Offset, _allocation.Size);
        }
    }

    class AddressSpacePartitionAllocator : PrivateMemoryAllocatorImpl<AddressSpacePartitionAllocator.Block>
    {
        private const ulong DefaultBlockAlignment = 1UL << 32; // 4GB

        public class Block : PrivateMemoryAllocator.Block
        {
            private readonly MemoryTracking _tracking;
            private readonly Func<ulong, ulong> _readPtCallback;
            private readonly MemoryEhMeilleure _memoryEh;

            private class Mapping : IntrusiveRedBlackTreeNode<Mapping>, IComparable<Mapping>, IComparable<ulong>
            {
                public ulong Address { get; }
                public ulong Size { get; }
                public ulong EndAddress => Address + Size;
                public ulong Va { get; }
                public ulong EndVa { get; }

                public Mapping(ulong address, ulong size, ulong va, ulong endVa)
                {
                    Address = address;
                    Size = size;
                    Va = va;
                    EndVa = endVa;
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

            private readonly AddressIntrusiveRedBlackTree<Mapping> _mappingTree;
            
            // type is not Lock due to the unique usage of this mechanism,
            // an arbitrary object is used as the lock passed in by constructor.
            private readonly object _lock;

            public Block(MemoryTracking tracking, Func<ulong, ulong> readPtCallback, MemoryBlock memory, ulong size, object locker) : base(memory, size)
            {
                _tracking = tracking;
                _readPtCallback = readPtCallback;
                _memoryEh = new(memory, null, tracking, VirtualMemoryEvent);
                _mappingTree = new();
                _lock = locker;
            }

            public void AddMapping(ulong offset, ulong size, ulong va, ulong endVa)
            {
                _mappingTree.Add(new(offset, size, va, endVa));
            }

            public void RemoveMapping(ulong offset, ulong size)
            {
                _mappingTree.Remove(_mappingTree.GetNode(offset));
            }

            private ulong VirtualMemoryEvent(ulong address, ulong size, bool write)
            {
                Mapping map;

                lock (_lock)
                {
                    map = _mappingTree.GetNode(address);
                }

                if (map == null)
                {
                    return 0;
                }

                address -= map.Address;

                ulong addressAligned = BitUtils.AlignDown(address, AddressSpacePartition.GuestPageSize);
                ulong endAddressAligned = BitUtils.AlignUp(address + size, AddressSpacePartition.GuestPageSize);
                ulong sizeAligned = endAddressAligned - addressAligned;

                if (!_tracking.VirtualMemoryEvent(map.Va + addressAligned, sizeAligned, write))
                {
                    return 0;
                }

                return _readPtCallback(map.Va + address);
            }

            public override void Destroy()
            {
                _memoryEh.Dispose();

                base.Destroy();
            }
        }

        private readonly MemoryTracking _tracking;
        private readonly Func<ulong, ulong> _readPtCallback;
        
        // type is not Lock due to the unique usage of this mechanism,
        // an arbitrary object is used as the lock passed in by constructor.
        private readonly object _lock;

        public AddressSpacePartitionAllocator(
            MemoryTracking tracking,
            Func<ulong, ulong> readPtCallback,
            object locker) : base(DefaultBlockAlignment, MemoryAllocationFlags.Reserve | MemoryAllocationFlags.ViewCompatible)
        {
            _tracking = tracking;
            _readPtCallback = readPtCallback;
            _lock = locker;
        }

        public AddressSpacePartitionAllocation Allocate(ulong va, ulong size)
        {
            AddressSpacePartitionAllocation allocation = new(this, Allocate(size, MemoryBlock.GetPageSize(), CreateBlock));
            allocation.RegisterMapping(va, va + size);

            return allocation;
        }

        private Block CreateBlock(MemoryBlock memory, ulong size)
        {
            return new Block(_tracking, _readPtCallback, memory, size, _lock);
        }
    }
}
