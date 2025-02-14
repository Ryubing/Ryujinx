using Ryujinx.Memory.Range;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Ryujinx.Memory
{
    /// <summary>
    /// Represents a address space manager.
    /// Supports virtual memory region mapping, address translation and read/write access to mapped regions.
    /// </summary>
    public sealed class AddressSpaceManager : VirtualMemoryManagerBase, IVirtualMemoryManager
    {
        /// <inheritdoc/>
        public bool UsesPrivateAllocations => false;

        /// <summary>
        /// Address space width in bits.
        /// </summary>
        public int AddressSpaceBits { get; }

        private readonly MemoryBlock _backingMemory;
        private readonly PageTable<nuint> _pageTable;

        protected override ulong AddressSpaceSize { get; }

        /// <summary>
        /// Creates a new instance of the memory manager.
        /// </summary>
        /// <param name="backingMemory">Physical backing memory where virtual memory will be mapped to</param>
        /// <param name="addressSpaceSize">Size of the address space</param>
        public AddressSpaceManager(MemoryBlock backingMemory, ulong addressSpaceSize)
        {
            ulong asSize = PageSize;
            int asBits = PageBits;

            while (asSize < addressSpaceSize)
            {
                asSize <<= 1;
                asBits++;
            }

            AddressSpaceBits = asBits;
            AddressSpaceSize = asSize;
            _backingMemory = backingMemory;
            _pageTable = new PageTable<nuint>();
        }

        /// <inheritdoc/>
        public void Map(ulong va, ulong pa, ulong size, MemoryMapFlags flags)
        {
            AssertValidAddressAndSize(va, size);

            while (size != 0)
            {
                _pageTable.Map(va, (nuint)(ulong)_backingMemory.GetPointer(pa, PageSize));

                va += PageSize;
                pa += PageSize;
                size -= PageSize;
            }
        }

        public override void MapForeign(ulong va, nuint hostPointer, ulong size)
        {
            AssertValidAddressAndSize(va, size);

            while (size != 0)
            {
                _pageTable.Map(va, hostPointer);

                va += PageSize;
                hostPointer += PageSize;
                size -= PageSize;
            }
        }

        /// <inheritdoc/>
        public void Unmap(ulong va, ulong size)
        {
            AssertValidAddressAndSize(va, size);

            while (size != 0)
            {
                _pageTable.Unmap(va);

                va += PageSize;
                size -= PageSize;
            }
        }

        /// <inheritdoc/>
        public unsafe ref T GetRef<T>(ulong va) where T : unmanaged
        {
            if (!IsContiguous(va, Unsafe.SizeOf<T>()))
            {
                ThrowMemoryNotContiguous();
            }

            return ref *(T*)GetHostAddress(va);
        }

        /// <inheritdoc/>
        public IEnumerable<HostMemoryRange> GetHostRegions(ulong va, ulong size)
        {
            if (size == 0)
            {
                yield break;
            }

            foreach (HostMemoryRange hostRegion in GetHostRegionsImpl(va, size))
            {
                yield return hostRegion;
            }
        }

        /// <inheritdoc/>
        public IEnumerable<MemoryRange> GetPhysicalRegions(ulong va, ulong size)
        {
            if (size == 0)
            {
                yield break;
            }

            IEnumerable<HostMemoryRange> hostRegions = GetHostRegionsImpl(va, size);
            if (hostRegions == null)
            {
                yield break;
            }

            ulong backingStart = (ulong)_backingMemory.Pointer;
            ulong backingEnd = backingStart + _backingMemory.Size;

            foreach (HostMemoryRange hostRegion in hostRegions)
            {
                if (hostRegion.Address >= backingStart && hostRegion.Address < backingEnd)
                {
                    yield return new MemoryRange(hostRegion.Address - backingStart, hostRegion.Size);
                }
            }
        }

        private IEnumerable<HostMemoryRange> GetHostRegionsImpl(ulong va, ulong size)
        {
            if (!ValidateAddress(va) || !ValidateAddressAndSize(va, size))
            {
                yield break;
            }

            int pages = GetPagesCount(va, size, out va);

            nuint regionStart = GetHostAddress(va);
            ulong regionSize = PageSize;

            for (int page = 0; page < pages - 1; page++)
            {
                if (!ValidateAddress(va + PageSize))
                {
                    yield break;
                }

                nuint newHostAddress = GetHostAddress(va + PageSize);

                if (GetHostAddress(va) + PageSize != newHostAddress)
                {
                    yield return new HostMemoryRange(regionStart, regionSize);
                    regionStart = newHostAddress;
                    regionSize = 0;
                }

                va += PageSize;
                regionSize += PageSize;
            }

            yield return new HostMemoryRange(regionStart, regionSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool IsMapped(ulong va)
        {
            if (!ValidateAddress(va))
            {
                return false;
            }

            return _pageTable.Read(va) != 0;
        }

        /// <inheritdoc/>
        public bool IsRangeMapped(ulong va, ulong size)
        {
            if (size == 0)
            {
                return true;
            }

            if (!ValidateAddressAndSize(va, size))
            {
                return false;
            }

            int pages = GetPagesCount(va, (uint)size, out va);

            for (int page = 0; page < pages; page++)
            {
                if (!IsMapped(va))
                {
                    return false;
                }

                va += PageSize;
            }

            return true;
        }

        private nuint GetHostAddress(ulong va)
        {
            return _pageTable.Read(va) + (nuint)(va & PageMask);
        }

        /// <inheritdoc/>
        public void Reprotect(ulong va, ulong size, MemoryPermission protection)
        {
        }

        /// <inheritdoc/>
        public void TrackingReprotect(ulong va, ulong size, MemoryPermission protection, bool guest = false)
        {
            throw new NotImplementedException();
        }

        protected unsafe override Memory<byte> GetPhysicalAddressMemory(nuint pa, int size)
            => new NativeMemoryManager<byte>((byte*)pa, size).Memory;

        protected override unsafe Span<byte> GetPhysicalAddressSpan(nuint pa, int size) => new((void*)pa, size);

        protected override nuint TranslateVirtualAddressChecked(ulong va)
            => GetHostAddress(va);

        protected override nuint TranslateVirtualAddressUnchecked(ulong va)
            => GetHostAddress(va);
    }
}
