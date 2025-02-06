using Ryujinx.Common.Memory;
using Ryujinx.Graphics.Gpu.Image;
using Ryujinx.Memory;
using Ryujinx.Memory.Range;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Gpu.Memory
{
    /// <summary>
    /// GPU memory manager.
    /// </summary>
    public class MemoryManager : IWritableBlock
    {
        private const int PtLvl0Bits = 14;
        private const int PtLvl1Bits = 14;
        public const int PtPageBits = 12;

        private const ulong PtLvl0Size = 1UL << PtLvl0Bits;
        private const ulong PtLvl1Size = 1UL << PtLvl1Bits;
        public const ulong PageSize = 1UL << PtPageBits;

        private const ulong PtLvl0Mask = PtLvl0Size - 1;
        private const ulong PtLvl1Mask = PtLvl1Size - 1;
        public const ulong PageMask = PageSize - 1;

        private const int PtLvl0Bit = PtPageBits + PtLvl1Bits;
        private const int PtLvl1Bit = PtPageBits;
        private const int AddressSpaceBits = PtPageBits + PtLvl1Bits + PtLvl0Bits;

        public const ulong PteUnmapped = ulong.MaxValue;

        private readonly ulong[][] _pageTable;

        public event EventHandler<UnmapEventArgs> MemoryUnmapped;

        private readonly GpuContext _context;
        private readonly List<PhysicalMemory> _physicalMemoryList;
        private readonly Dictionary<PhysicalMemory, byte> _physicalMemoryMap;

        /// <summary>
        /// Virtual range cache.
        /// </summary>
        internal VirtualRangeCache VirtualRangeCache { get; }

        /// <summary>
        /// Cache of GPU counters.
        /// </summary>
        internal CounterCache CounterCache { get; }

        /// <summary>
        /// Creates a new instance of the GPU memory manager.
        /// </summary>
        /// <param name="context">GPU context</param>
        /// <param name="physicalMemory">Physical memory that this memory manager will map into</param>
        /// <param name="cpuMemorySize">The amount of physical CPU Memory Avaiable on the device.</param>

        internal MemoryManager(GpuContext context, PhysicalMemory physicalMemory, ulong cpuMemorySize)
        {
            _context = context;

            _physicalMemoryList = new List<PhysicalMemory>()
            {
                physicalMemory
            };

            _physicalMemoryMap = new Dictionary<PhysicalMemory, byte>
            {
                { physicalMemory, 0 }
            };

            VirtualRangeCache = new VirtualRangeCache(this);
            CounterCache = new CounterCache();
            _pageTable = new ulong[PtLvl0Size][];
            MemoryUnmapped += physicalMemory.TextureCache.MemoryUnmappedHandler;
            MemoryUnmapped += physicalMemory.BufferCache.MemoryUnmappedHandler;
            MemoryUnmapped += VirtualRangeCache.MemoryUnmappedHandler;
            MemoryUnmapped += CounterCache.MemoryUnmappedHandler;
            physicalMemory.TextureCache.Initialize(cpuMemorySize);
        }

        /// <summary>
        /// Attaches the memory manager to a new GPU channel.
        /// </summary>
        /// <param name="rebind">Action to be performed when the buffer cache changes</param>
        internal void AttachToChannel(Action rebind)
        {
            PhysicalMemory physicalMemory = GetOwnPhysicalMemory();

            physicalMemory.IncrementReferenceCount();
            physicalMemory.BufferCache.NotifyBuffersModified += rebind;
            physicalMemory.BufferCache.QueuePrune();
        }

        /// <summary>
        /// Attaches the memory manager to a new GPU channel.
        /// </summary>
        /// <param name="rebind">Action that was performed when the buffer cache changed</param>
        internal void DetachFromChannel(Action rebind)
        {
            PhysicalMemory physicalMemory = GetOwnPhysicalMemory();

            physicalMemory.BufferCache.NotifyBuffersModified -= rebind;
            physicalMemory.DecrementReferenceCount();
        }

        /// <summary>
        /// Queues a prune of invalid entries on the buffer cache.
        /// </summary>
        internal void QueuePrune()
        {
            GetOwnPhysicalMemory().BufferCache.QueuePrune();
        }

        /// <summary>
        /// Reads data from GPU mapped memory.
        /// </summary>
        /// <typeparam name="T">Type of the data</typeparam>
        /// <param name="va">GPU virtual address where the data is located</param>
        /// <param name="tracked">True if read tracking is triggered on the memory region</param>
        /// <returns>The data at the specified memory location</returns>
        public T Read<T>(ulong va, bool tracked = false) where T : unmanaged
        {
            int size = Unsafe.SizeOf<T>();

            if (IsContiguous(va, size))
            {
                (PhysicalMemory physicalMemory, ulong address) = TranslateWithPhysicalMemory(va);

                if (tracked)
                {
                    return physicalMemory.ReadTracked<T>(address);
                }
                else
                {
                    return physicalMemory.Read<T>(address);
                }
            }
            else
            {
                Span<byte> data = new byte[size];

                ReadImpl(va, data, tracked);

                return MemoryMarshal.Cast<byte, T>(data)[0];
            }
        }

        /// <summary>
        /// Gets a read-only span of data from GPU mapped memory.
        /// </summary>
        /// <param name="va">GPU virtual address where the data is located</param>
        /// <param name="size">Size of the data</param>
        /// <param name="tracked">True if read tracking is triggered on the span</param>
        /// <returns>The span of the data at the specified memory location</returns>
        public ReadOnlySpan<byte> GetSpan(ulong va, int size, bool tracked = false)
        {
            if (IsContiguous(va, size))
            {
                (PhysicalMemory physicalMemory, ulong address) = TranslateWithPhysicalMemory(va);

                return physicalMemory.GetSpan(address, size, tracked);
            }
            else
            {
                Span<byte> data = new byte[size];

                ReadImpl(va, data, tracked);

                return data;
            }
        }

        /// <summary>
        /// Gets a read-only span of data from GPU mapped memory, up to the entire range specified,
        /// or the last mapped page if the range is not fully mapped.
        /// </summary>
        /// <param name="va">GPU virtual address where the data is located</param>
        /// <param name="size">Size of the data</param>
        /// <param name="tracked">True if read tracking is triggered on the span</param>
        /// <returns>The span of the data at the specified memory location</returns>
        public ReadOnlySpan<byte> GetSpanMapped(ulong va, int size, bool tracked = false)
        {
            bool isContiguous = true;
            int mappedSize;

            if (ValidateAddress(va) && IsMappedOnGpuAndPhysical(va))
            {
                ulong endVa = va + (ulong)size;
                ulong endVaAligned = (endVa + PageMask) & ~PageMask;
                ulong currentVa = va & ~PageMask;

                int pages = (int)((endVaAligned - currentVa) / PageSize);

                for (int page = 0; page < pages - 1; page++)
                {
                    ulong nextVa = currentVa + PageSize;
                    ulong nextPa = Translate(nextVa);

                    if (!ValidateAddress(nextVa) || !IsMappedOnGpuAndPhysical(nextVa))
                    {
                        break;
                    }

                    if (Translate(currentVa) + PageSize != nextPa)
                    {
                        isContiguous = false;
                    }

                    currentVa += PageSize;
                }

                currentVa += PageSize;

                if (currentVa > endVa)
                {
                    currentVa = endVa;
                }

                mappedSize = (int)(currentVa - va);
            }
            else
            {
                return ReadOnlySpan<byte>.Empty;
            }

            if (isContiguous)
            {
                (PhysicalMemory physicalMemory, ulong address) = TranslateWithPhysicalMemory(va);

                return physicalMemory.GetSpan(address, mappedSize, tracked);
            }
            else
            {
                Span<byte> data = new byte[mappedSize];

                ReadImpl(va, data, tracked);

                return data;
            }
        }

        /// <summary>
        /// Checks if a page of memory is mapped on the GPU and its backing memory.
        /// </summary>
        /// <param name="va">GPU virtual address of the page</param>
        /// <returns>True if mapped, false otherwise</returns>
        private bool IsMappedOnGpuAndPhysical(ulong va)
        {
            (PhysicalMemory physicalMemory, ulong address) = TranslateWithPhysicalMemory(va);

            if (address == PteUnmapped)
            {
                return false;
            }

            return physicalMemory.IsMapped(address);
        }

        /// <summary>
        /// Reads data from a possibly non-contiguous region of GPU mapped memory.
        /// </summary>
        /// <param name="va">GPU virtual address of the data</param>
        /// <param name="data">Span to write the read data into</param>
        /// <param name="tracked">True to enable write tracking on read, false otherwise</param>
        private void ReadImpl(ulong va, Span<byte> data, bool tracked)
        {
            if (data.Length == 0)
            {
                return;
            }

            int offset = 0, size;

            if ((va & PageMask) != 0)
            {
                (PhysicalMemory physicalMemory, ulong pa) = TranslateWithPhysicalMemory(va);

                size = Math.Min(data.Length, (int)PageSize - (int)(va & PageMask));

                physicalMemory.GetSpan(pa, size, tracked).CopyTo(data[..size]);

                offset += size;
            }

            for (; offset < data.Length; offset += size)
            {
                (PhysicalMemory physicalMemory, ulong pa) = TranslateWithPhysicalMemory(va + (ulong)offset);

                size = Math.Min(data.Length - offset, (int)PageSize);

                physicalMemory.GetSpan(pa, size, tracked).CopyTo(data.Slice(offset, size));
            }
        }

        /// <summary>
        /// Gets a writable region from GPU mapped memory.
        /// </summary>
        /// <param name="va">Start address of the range</param>
        /// <param name="size">Size in bytes to be range</param>
        /// <param name="tracked">True if write tracking is triggered on the span</param>
        /// <returns>A writable region with the data at the specified memory location</returns>
        public WritableRegion GetWritableRegion(ulong va, int size, bool tracked = false)
        {
            if (IsContiguous(va, size))
            {
                (PhysicalMemory physicalMemory, ulong address) = TranslateWithPhysicalMemory(va);

                return physicalMemory.GetWritableRegion(address, size, tracked);
            }
            else
            {
                Memory<byte> memory = new byte[size];

                GetSpan(va, size).CopyTo(memory.Span);

                return new WritableRegion(this, va, memory, tracked);
            }
        }

        /// <summary>
        /// Writes data to GPU mapped memory.
        /// </summary>
        /// <typeparam name="T">Type of the data</typeparam>
        /// <param name="va">GPU virtual address to write the value into</param>
        /// <param name="value">The value to be written</param>
        public void Write<T>(ulong va, T value) where T : unmanaged
        {
            Write(va, MemoryMarshal.Cast<T, byte>(MemoryMarshal.CreateSpan(ref value, 1)));
        }

        /// <summary>
        /// Writes data to GPU mapped memory.
        /// </summary>
        /// <param name="va">GPU virtual address to write the data into</param>
        /// <param name="data">The data to be written</param>
        public void Write(ulong va, ReadOnlySpan<byte> data)
        {
            WriteImpl(va, data, (physical, va, data) => physical.Write(va, data));
        }

        /// <summary>
        /// Writes data to GPU mapped memory, destined for a tracked resource.
        /// </summary>
        /// <param name="va">GPU virtual address to write the data into</param>
        /// <param name="data">The data to be written</param>
        public void WriteTrackedResource(ulong va, ReadOnlySpan<byte> data)
        {
            WriteImpl(va, data, (physical, va, data) => physical.WriteTrackedResource(va, data));
        }

        /// <summary>
        /// Writes data to GPU mapped memory without write tracking.
        /// </summary>
        /// <param name="va">GPU virtual address to write the data into</param>
        /// <param name="data">The data to be written</param>
        public void WriteUntracked(ulong va, ReadOnlySpan<byte> data)
        {
            WriteImpl(va, data, (physical, va, data) => physical.WriteUntracked(va, data));
        }

        private delegate void WriteCallback(PhysicalMemory physicalMemory, ulong address, ReadOnlySpan<byte> data);

        /// <summary>
        /// Writes data to possibly non-contiguous GPU mapped memory.
        /// </summary>
        /// <param name="va">GPU virtual address of the region to write into</param>
        /// <param name="data">Data to be written</param>
        /// <param name="writeCallback">Write callback</param>
        private void WriteImpl(ulong va, ReadOnlySpan<byte> data, WriteCallback writeCallback)
        {
            if (IsContiguous(va, data.Length))
            {
                (PhysicalMemory physicalMemory, ulong address) = TranslateWithPhysicalMemory(va);

                writeCallback(physicalMemory, address, data);
            }
            else
            {
                int offset = 0, size;

                if ((va & PageMask) != 0)
                {
                    (PhysicalMemory physicalMemory, ulong pa) = TranslateWithPhysicalMemory(va);

                    size = Math.Min(data.Length, (int)PageSize - (int)(va & PageMask));

                    writeCallback(physicalMemory, pa, data[..size]);

                    offset += size;
                }

                for (; offset < data.Length; offset += size)
                {
                    (PhysicalMemory physicalMemory, ulong pa) = TranslateWithPhysicalMemory(va + (ulong)offset);

                    size = Math.Min(data.Length - offset, (int)PageSize);

                    writeCallback(physicalMemory, pa, data.Slice(offset, size));
                }
            }
        }

        /// <summary>
        /// Writes data to GPU mapped memory, stopping at the first unmapped page at the memory region, if any.
        /// </summary>
        /// <param name="va">GPU virtual address to write the data into</param>
        /// <param name="data">The data to be written</param>
        public void WriteMapped(ulong va, ReadOnlySpan<byte> data)
        {
            if (IsContiguous(va, data.Length))
            {
                (PhysicalMemory physicalMemory, ulong address) = TranslateWithPhysicalMemory(va);

                physicalMemory.Write(address, data);
            }
            else
            {
                int offset = 0, size;

                if ((va & PageMask) != 0)
                {
                    (PhysicalMemory physicalMemory, ulong pa) = TranslateWithPhysicalMemory(va);

                    size = Math.Min(data.Length, (int)PageSize - (int)(va & PageMask));

                    if (pa != PteUnmapped && physicalMemory.IsMapped(pa))
                    {
                        physicalMemory.Write(pa, data[..size]);
                    }

                    offset += size;
                }

                for (; offset < data.Length; offset += size)
                {
                    (PhysicalMemory physicalMemory, ulong pa) = TranslateWithPhysicalMemory(va + (ulong)offset);

                    size = Math.Min(data.Length - offset, (int)PageSize);

                    if (pa != PteUnmapped && physicalMemory.IsMapped(pa))
                    {
                        physicalMemory.Write(pa, data.Slice(offset, size));
                    }
                }
            }
        }

        /// <summary>
        /// Runs remap actions that are added to an unmap event.
        /// These must run after the mapping completes.
        /// </summary>
        /// <param name="e">Event with remap actions</param>
        private static void RunRemapActions(UnmapEventArgs e)
        {
            if (e.RemapActions != null)
            {
                foreach (Action action in e.RemapActions)
                {
                    action();
                }
            }
        }

        /// <summary>
        /// Maps a given range of pages to the specified CPU virtual address.
        /// </summary>
        /// <remarks>
        /// All addresses and sizes must be page aligned.
        /// </remarks>
        /// <param name="pa">CPU virtual address to map into</param>
        /// <param name="va">GPU virtual address to be mapped</param>
        /// <param name="size">Size in bytes of the mapping</param>
        /// <param name="kind">Kind of the resource located at the mapping</param>
        public void Map(ulong pa, ulong va, ulong size, PteKind kind)
        {
            MapImpl(pa, va, size, kind);
        }

        /// <summary>
        /// Maps a given range of pages to the specified CPU virtual address from a different process.
        /// </summary>
        /// <remarks>
        /// All addresses and sizes must be page aligned.
        /// </remarks>
        /// <param name="pa">CPU virtual address to map into</param>
        /// <param name="va">GPU virtual address to be mapped</param>
        /// <param name="size">Size in bytes of the mapping</param>
        /// <param name="kind">Kind of the resource located at the mapping</param>
        /// <param name="ownedPid">PID of the process that owns the mapping</param>
        public void MapForeign(ulong pa, ulong va, ulong size, PteKind kind, ulong ownedPid)
        {
            if (_context.PhysicalMemoryRegistry.TryGetValue(ownedPid, out PhysicalMemory physicalMemory))
            {
                MapImpl(pa, va, size, kind, physicalMemory);
            }
        }

        /// <summary>
        /// Maps a given range of pages to the specified CPU virtual address.
        /// </summary>
        /// <remarks>
        /// All addresses and sizes must be page aligned.
        /// </remarks>
        /// <param name="pa">CPU virtual address to map into</param>
        /// <param name="va">GPU virtual address to be mapped</param>
        /// <param name="size">Size in bytes of the mapping</param>
        /// <param name="kind">Kind of the resource located at the mapping</param>
        /// <param name="physicalMemory">Optional physical memory to import for the mapping</param>
        private void MapImpl(ulong pa, ulong va, ulong size, PteKind kind, PhysicalMemory physicalMemory = null)
        {
            lock (_pageTable)
            {
                UnmapEventArgs e = new(va, size);
                MemoryUnmapped?.Invoke(this, e);
                byte pIndex = physicalMemory != null ? GetOrAddPhysicalMemory(physicalMemory) : (byte)0;

                for (ulong offset = 0; offset < size; offset += PageSize)
                {
                    SetPte(va + offset, PackPte(pa + offset, pIndex, kind));
                }

                RunRemapActions(e);
            }
        }

        /// <summary>
        /// Unmaps a given range of pages at the specified GPU virtual memory region.
        /// </summary>
        /// <param name="va">GPU virtual address to unmap</param>
        /// <param name="size">Size in bytes of the region being unmapped</param>
        public void Unmap(ulong va, ulong size)
        {
            lock (_pageTable)
            {
                // Event handlers are not expected to be thread safe.
                UnmapEventArgs e = new(va, size);
                MemoryUnmapped?.Invoke(this, e);

                for (ulong offset = 0; offset < size; offset += PageSize)
                {
                    SetPte(va + offset, PteUnmapped);
                }

                RunRemapActions(e);
            }
        }

        /// <summary>
        /// Checks if a region of GPU mapped memory is contiguous.
        /// </summary>
        /// <param name="va">GPU virtual address of the region</param>
        /// <param name="size">Size of the region</param>
        /// <returns>True if the region is contiguous, false otherwise</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsContiguous(ulong va, int size)
        {
            if (!ValidateAddress(va) || GetPte(va) == PteUnmapped)
            {
                return false;
            }

            ulong endVa = (va + (ulong)size + PageMask) & ~PageMask;

            va &= ~PageMask;

            int pages = (int)((endVa - va) / PageSize);

            for (int page = 0; page < pages - 1; page++)
            {
                ulong nextPte = GetPte(va + PageSize);

                if (!ValidateAddress(va + PageSize) || nextPte == PteUnmapped)
                {
                    return false;
                }

                if (GetPte(va) + PageSize != nextPte)
                {
                    return false;
                }

                va += PageSize;
            }

            return true;
        }

        /// <summary>
        /// Gets the physical regions that make up the given virtual address region.
        /// </summary>
        /// <param name="va">Virtual address of the range</param>
        /// <param name="size">Size of the range</param>
        /// <returns>Multi-range with the physical regions</returns>
        public MultiRange GetPhysicalRegions(ulong va, ulong size)
        {
            if (IsContiguous(va, (int)size))
            {
                return new MultiRange(Translate(va), size);
            }

            ulong regionStart = Translate(va);
            ulong regionSize = Math.Min(size, PageSize - (va & PageMask));

            ulong endVa = va + size;
            ulong endVaRounded = (endVa + PageMask) & ~PageMask;

            va &= ~PageMask;

            int pages = (int)((endVaRounded - va) / PageSize);

            var regions = new List<MemoryRange>();

            for (int page = 0; page < pages - 1; page++)
            {
                ulong currPa = Translate(va);
                ulong newPa = Translate(va + PageSize);

                if ((currPa != PteUnmapped || newPa != PteUnmapped) && currPa + PageSize != newPa)
                {
                    regions.Add(new MemoryRange(regionStart, regionSize));
                    regionStart = newPa;
                    regionSize = 0;
                }

                va += PageSize;
                regionSize += Math.Min(endVa - va, PageSize);
            }

            if (regions.Count == 0)
            {
                return new MultiRange(regionStart, regionSize);
            }

            regions.Add(new MemoryRange(regionStart, regionSize));

            return new MultiRange(regions.ToArray());
        }

        /// <summary>
        /// Checks if a given GPU virtual memory range is mapped to the same physical regions
        /// as the specified physical memory multi-range.
        /// </summary>
        /// <param name="range">Physical memory multi-range</param>
        /// <param name="va">GPU virtual memory address</param>
        /// <returns>True if the virtual memory region is mapped into the specified physical one, false otherwise</returns>
        public bool CompareRange(MultiRange range, ulong va)
        {
            va &= ~PageMask;

            for (int i = 0; i < range.Count; i++)
            {
                MemoryRange currentRange = range.GetSubRange(i);

                if (currentRange.Address != PteUnmapped)
                {
                    ulong address = currentRange.Address & ~PageMask;
                    ulong endAddress = (currentRange.EndAddress + PageMask) & ~PageMask;

                    while (address < endAddress)
                    {
                        if (Translate(va) != address)
                        {
                            return false;
                        }

                        va += PageSize;
                        address += PageSize;
                    }
                }
                else
                {
                    ulong endVa = va + (((currentRange.Size) + PageMask) & ~PageMask);

                    while (va < endVa)
                    {
                        if (Translate(va) != PteUnmapped)
                        {
                            return false;
                        }

                        va += PageSize;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Gets the backing memory for a given GPU virtual address.
        /// </summary>
        /// <param name="va">GPU virtual address to get the backing memory from</param>
        /// <returns>The backing memory for the specified GPU virtual address</returns>
        internal PhysicalMemory GetBackingMemory(ulong va)
        {
            ulong pte = GetPte(va);

            if (pte == PteUnmapped)
            {
                return GetOwnPhysicalMemory();
            }

            return _physicalMemoryList[UnpackPIndexFromPte(pte)];
        }

        /// <summary>
        /// Gets the backing memory that is owned by this GPU memory manager.
        /// </summary>
        /// <returns>The backing memory owned by this memory manager</returns>
        private PhysicalMemory GetOwnPhysicalMemory()
        {
            return _physicalMemoryList[0];
        }

        /// <summary>
        /// Gets the index for a given physical memory on the list, adding it to the list if needed.
        /// </summary>
        /// <param name="physicalMemory">Physical memory to get the index from</param>
        /// <returns>The index of the physical memory on the list</returns>
        private byte GetOrAddPhysicalMemory(PhysicalMemory physicalMemory)
        {
            if (!_physicalMemoryMap.TryGetValue(physicalMemory, out byte pIndex))
            {
                pIndex = checked((byte)_physicalMemoryList.Count);
                _physicalMemoryList.Add(physicalMemory);
                _physicalMemoryMap.Add(physicalMemory, pIndex);
            }

            return pIndex;
        }

        /// <summary>
        /// Validates a GPU virtual address.
        /// </summary>
        /// <param name="va">Address to validate</param>
        /// <returns>True if the address is valid, false otherwise</returns>
        private static bool ValidateAddress(ulong va)
        {
            return va < (1UL << AddressSpaceBits);
        }

        /// <summary>
        /// Checks if a given page is mapped.
        /// </summary>
        /// <param name="va">GPU virtual address of the page to check</param>
        /// <returns>True if the page is mapped, false otherwise</returns>
        public bool IsMapped(ulong va)
        {
            return Translate(va) != PteUnmapped;
        }

        /// <summary>
        /// Translates a GPU virtual address to a CPU virtual address.
        /// </summary>
        /// <param name="va">GPU virtual address to be translated</param>
        /// <returns>CPU virtual address, or <see cref="PteUnmapped"/> if unmapped</returns>
        public ulong Translate(ulong va)
        {
            if (!ValidateAddress(va))
            {
                return PteUnmapped;
            }

            ulong pte = GetPte(va);

            if (pte == PteUnmapped)
            {
                return PteUnmapped;
            }

            return UnpackPaFromPte(pte) + (va & PageMask);
        }

        /// <summary>
        /// Translates a GPU virtual address to a CPU virtual address on the first mapped page of memory
        /// on the specified region.
        /// If no page is mapped on the specified region, <see cref="PteUnmapped"/> is returned.
        /// </summary>
        /// <param name="va">GPU virtual address to be translated</param>
        /// <param name="size">Size of the range to be translated</param>
        /// <returns>CPU virtual address, or <see cref="PteUnmapped"/> if unmapped</returns>
        public ulong TranslateFirstMapped(ulong va, ulong size)
        {
            if (!ValidateAddress(va))
            {
                return PteUnmapped;
            }

            ulong endVa = va + size;

            ulong pte = GetPte(va);

            for (; va < endVa && pte == PteUnmapped; va += PageSize - (va & PageMask))
            {
                pte = GetPte(va);
            }

            if (pte == PteUnmapped)
            {
                return PteUnmapped;
            }

            return UnpackPaFromPte(pte) + (va & PageMask);
        }

        /// <summary>
        /// Translates a GPU virtual address and returns the number of bytes that are mapped after it.
        /// </summary>
        /// <param name="va">GPU virtual address to be translated</param>
        /// <param name="maxSize">Maximum size in bytes to scan</param>
        /// <returns>Number of bytes, 0 if unmapped</returns>
        public ulong GetMappedSize(ulong va, ulong maxSize)
        {
            if (!ValidateAddress(va))
            {
                return 0;
            }

            ulong startVa = va;
            ulong endVa = va + maxSize;

            ulong pte = GetPte(va);

            while (pte != PteUnmapped && va < endVa)
            {
                va += PageSize - (va & PageMask);
                pte = GetPte(va);
            }

            return Math.Min(maxSize, va - startVa);
        }

        /// <summary>
        /// Translates a GPU virtual address to a CPU virtual address and the associated physical memory.
        /// </summary>
        /// <param name="va">GPU virtual address to be translated</param>
        /// <returns>CPU virtual address with the physical memory, or <see cref="PteUnmapped"/> if unmapped</returns>
        private (PhysicalMemory, ulong) TranslateWithPhysicalMemory(ulong va)
        {
            if (!ValidateAddress(va))
            {
                return (GetOwnPhysicalMemory(), PteUnmapped);
            }

            ulong pte = GetPte(va);

            if (pte == PteUnmapped)
            {
                return (GetOwnPhysicalMemory(), PteUnmapped);
            }

            return (_physicalMemoryList[UnpackPIndexFromPte(pte)], UnpackPaFromPte(pte) + (va & PageMask));
        }

        /// <summary>
        /// Gets the kind of a given memory page.
        /// This might indicate the type of resource that can be allocated on the page, and also texture tiling.
        /// </summary>
        /// <param name="va">GPU virtual address</param>
        /// <returns>Kind of the memory page</returns>
        public PteKind GetKind(ulong va)
        {
            if (!ValidateAddress(va))
            {
                return PteKind.Invalid;
            }

            ulong pte = GetPte(va);

            if (pte == PteUnmapped)
            {
                return PteKind.Invalid;
            }

            return UnpackKindFromPte(pte);
        }

        public bool IsForeignMapping(ulong va)
        {
            ulong pte = GetPte(va);

            if (pte == PteUnmapped)
            {
                return false;
            }

            return UnpackPIndexFromPte(pte) != 0;
        }

        /// <summary>
        /// Gets the Page Table entry for a given GPU virtual address.
        /// </summary>
        /// <param name="va">GPU virtual address</param>
        /// <returns>Page table entry (CPU virtual address)</returns>
        private ulong GetPte(ulong va)
        {
            ulong l0 = (va >> PtLvl0Bit) & PtLvl0Mask;
            ulong l1 = (va >> PtLvl1Bit) & PtLvl1Mask;

            if (_pageTable[l0] == null)
            {
                return PteUnmapped;
            }

            return _pageTable[l0][l1];
        }

        /// <summary>
        /// Sets a Page Table entry at a given GPU virtual address.
        /// </summary>
        /// <param name="va">GPU virtual address</param>
        /// <param name="pte">Page table entry (CPU virtual address)</param>
        private void SetPte(ulong va, ulong pte)
        {
            ulong l0 = (va >> PtLvl0Bit) & PtLvl0Mask;
            ulong l1 = (va >> PtLvl1Bit) & PtLvl1Mask;

            if (_pageTable[l0] == null)
            {
                _pageTable[l0] = new ulong[PtLvl1Size];

                for (ulong index = 0; index < PtLvl1Size; index++)
                {
                    _pageTable[l0][index] = PteUnmapped;
                }
            }

            _pageTable[l0][l1] = pte;
        }

        /// <summary>
        /// Creates a page table entry from a physical address and kind.
        /// </summary>
        /// <param name="pa">Physical address</param>
        /// <param name="pIndex">Index of the physical memory on the list</param>
        /// <param name="kind">Kind</param>
        /// <returns>Page table entry</returns>
        private static ulong PackPte(ulong pa, byte pIndex, PteKind kind)
        {
            return pa | ((ulong)pIndex << 48) | ((ulong)kind << 56);
        }

        /// <summary>
        /// Unpacks kind from a page table entry.
        /// </summary>
        /// <param name="pte">Page table entry</param>
        /// <returns>Kind</returns>
        private static PteKind UnpackKindFromPte(ulong pte)
        {
            return (PteKind)(pte >> 56);
        }

        /// <summary>
        /// Unpacks the physical memory index in the list from a page table entry.
        /// </summary>
        /// <param name="pte">Page table entry</param>
        /// <returns>Physical memory index</returns>
        private static byte UnpackPIndexFromPte(ulong pte)
        {
            return (byte)(pte >> 48);
        }

        /// <summary>
        /// Unpacks physical address from a page table entry.
        /// </summary>
        /// <param name="pte">Page table entry</param>
        /// <returns>Physical address</returns>
        private static ulong UnpackPaFromPte(ulong pte)
        {
            return pte & 0xffffffffffffUL;
        }
    }
}
