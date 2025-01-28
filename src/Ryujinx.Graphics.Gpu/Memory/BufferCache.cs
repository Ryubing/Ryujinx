using Ryujinx.Graphics.GAL;
using Ryujinx.Memory.Range;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Ryujinx.Graphics.Gpu.Memory
{
    /// <summary>
    /// Buffer cache.
    /// </summary>
    class BufferCache : IDisposable
    {
        /// <summary>
        /// Initial size for the array holding overlaps.
        /// </summary>
        public const int OverlapsBufferInitialCapacity = 10;

        /// <summary>
        /// Maximum size that an array holding overlaps may have after trimming.
        /// </summary>
        public const int OverlapsBufferMaxCapacity = 10000;

        private const ulong BufferAlignmentSize = 0x1000;
        private const ulong BufferAlignmentMask = BufferAlignmentSize - 1;

        /// <summary>
        /// Alignment required for sparse buffer mappings.
        /// </summary>
        public const ulong SparseBufferAlignmentSize = 0x10000;

        private const ulong MaxDynamicGrowthSize = 0x100000;

        private readonly GpuContext _context;
        private readonly PhysicalMemory _physicalMemory;

        /// <remarks>
        /// Only modified from the GPU thread. Must lock for add/remove.
        /// Must lock for any access from other threads.
        /// </remarks>
        private readonly RangeList<Buffer> _buffers;
        private readonly MultiRangeList<MultiRangeBuffer> _multiRangeBuffers;

        private Buffer[] _bufferOverlaps;

        private readonly Dictionary<ulong, BufferCacheEntry> _dirtyCache;
        private readonly Dictionary<ulong, BufferCacheEntry> _modifiedCache;
        private bool _pruneCaches;
        private int _virtualModifiedSequenceNumber;

        public event Action NotifyBuffersModified;

        /// <summary>
        /// Creates a new instance of the buffer manager.
        /// </summary>
        /// <param name="context">The GPU context that the buffer manager belongs to</param>
        /// <param name="physicalMemory">Physical memory where the cached buffers are mapped</param>
        public BufferCache(GpuContext context, PhysicalMemory physicalMemory)
        {
            _context = context;
            _physicalMemory = physicalMemory;

            _buffers = [];
            _multiRangeBuffers = [];

            _bufferOverlaps = new Buffer[OverlapsBufferInitialCapacity];

            _dirtyCache = new Dictionary<ulong, BufferCacheEntry>();

            // There are a lot more entries on the modified cache, so it is separate from the one for ForceDirty.
            _modifiedCache = new Dictionary<ulong, BufferCacheEntry>();
        }

        /// <summary>
        /// Handles removal of buffers written to a memory region being unmapped.
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        public void MemoryUnmappedHandler(object sender, UnmapEventArgs e)
        {
            Buffer[] overlaps = new Buffer[10];
            int overlapCount;

            MultiRange range = ((MemoryManager)sender).GetPhysicalRegions(e.Address, e.Size);

            for (int index = 0; index < range.Count; index++)
            {
                MemoryRange subRange = range.GetSubRange(index);

                lock (_buffers)
                {
                    overlapCount = _buffers.FindOverlaps(subRange.Address, subRange.Size, ref overlaps);
                }

                for (int i = 0; i < overlapCount; i++)
                {
                    overlaps[i].Unmapped(subRange.Address, subRange.Size);
                }
            }
        }

        /// <summary>
        /// Performs address translation of the GPU virtual address, and creates a
        /// new buffer, if needed, for the specified contiguous range.
        /// </summary>
        /// <param name="memoryManager">GPU memory manager where the buffer is mapped</param>
        /// <param name="gpuVa">Start GPU virtual address of the buffer</param>
        /// <param name="size">Size in bytes of the buffer</param>
        /// <param name="stage">The type of usage that created the buffer</param>
        /// <returns>Contiguous physical range of the buffer, after address translation</returns>
        public MultiRange TranslateAndCreateBuffer(MemoryManager memoryManager, ulong gpuVa, ulong size, BufferStage stage)
        {
            if (gpuVa == 0)
            {
                return new MultiRange(MemoryManager.PteUnmapped, size);
            }

            ulong address = memoryManager.Translate(gpuVa);

            if (address != MemoryManager.PteUnmapped)
            {
                CreateBuffer(address, size, stage);
            }

            return new MultiRange(address, size);
        }

        /// <summary>
        /// Performs address translation of the GPU virtual address, and creates
        /// new physical and virtual buffers, if needed, for the specified range.
        /// </summary>
        /// <param name="memoryManager">GPU memory manager where the buffer is mapped</param>
        /// <param name="gpuVa">Start GPU virtual address of the buffer</param>
        /// <param name="size">Size in bytes of the buffer</param>
        /// <param name="stage">The type of usage that created the buffer</param>
        /// <returns>Physical ranges of the buffer, after address translation</returns>
        public MultiRange TranslateAndCreateMultiBuffers(MemoryManager memoryManager, ulong gpuVa, ulong size, BufferStage stage)
        {
            if (gpuVa == 0)
            {
                return new MultiRange(MemoryManager.PteUnmapped, size);
            }

            // Fast path not taken for non-contiguous ranges,
            // since multi-range buffers are not coalesced, so a buffer that covers
            // the entire cached range might not actually exist.
            if (memoryManager.VirtualRangeCache.TryGetOrAddRange(gpuVa, size, out MultiRange range) &&
                range.Count == 1)
            {
                return range;
            }

            CreateBuffer(range, stage);

            return range;
        }

        /// <summary>
        /// Performs address translation of the GPU virtual address, and creates
        /// new physical buffers, if needed, for the specified range.
        /// </summary>
        /// <param name="memoryManager">GPU memory manager where the buffer is mapped</param>
        /// <param name="gpuVa">Start GPU virtual address of the buffer</param>
        /// <param name="size">Size in bytes of the buffer</param>
        /// <param name="stage">The type of usage that created the buffer</param>
        /// <returns>Physical ranges of the buffer, after address translation</returns>
        public MultiRange TranslateAndCreateMultiBuffersPhysicalOnly(MemoryManager memoryManager, ulong gpuVa, ulong size, BufferStage stage)
        {
            if (gpuVa == 0)
            {
                return new MultiRange(MemoryManager.PteUnmapped, size);
            }

            // Fast path not taken for non-contiguous ranges,
            // since multi-range buffers are not coalesced, so a buffer that covers
            // the entire cached range might not actually exist.
            if (memoryManager.VirtualRangeCache.TryGetOrAddRange(gpuVa, size, out MultiRange range) &&
                range.Count == 1)
            {
                return range;
            }

            for (int i = 0; i < range.Count; i++)
            {
                MemoryRange subRange = range.GetSubRange(i);

                if (subRange.Address != MemoryManager.PteUnmapped)
                {
                    if (range.Count > 1)
                    {
                        CreateBuffer(subRange.Address, subRange.Size, stage, SparseBufferAlignmentSize);
                    }
                    else
                    {
                        CreateBuffer(subRange.Address, subRange.Size, stage);
                    }
                }
            }

            return range;
        }

        /// <summary>
        /// Creates a new buffer for the specified range, if it does not yet exist.
        /// This can be used to ensure the existance of a buffer.
        /// </summary>
        /// <param name="range">Physical ranges of memory where the buffer data is located</param>
        /// <param name="stage">The type of usage that created the buffer</param>
        public void CreateBuffer(MultiRange range, BufferStage stage)
        {
            if (range.Count > 1)
            {
                CreateMultiRangeBuffer(range, stage);
            }
            else
            {
                MemoryRange subRange = range.GetSubRange(0);

                if (subRange.Address != MemoryManager.PteUnmapped)
                {
                    CreateBuffer(subRange.Address, subRange.Size, stage);
                }
            }
        }

        /// <summary>
        /// Creates a new buffer for the specified range, if it does not yet exist.
        /// This can be used to ensure the existance of a buffer.
        /// </summary>
        /// <param name="address">Address of the buffer in memory</param>
        /// <param name="size">Size of the buffer in bytes</param>
        /// <param name="stage">The type of usage that created the buffer</param>
        public void CreateBuffer(ulong address, ulong size, BufferStage stage)
        {
            ulong endAddress = address + size;

            ulong alignedAddress = address & ~BufferAlignmentMask;
            ulong alignedEndAddress = (endAddress + BufferAlignmentMask) & ~BufferAlignmentMask;

            // The buffer must have the size of at least one page.
            if (alignedEndAddress == alignedAddress)
            {
                alignedEndAddress += BufferAlignmentSize;
            }

            CreateBufferAligned(alignedAddress, alignedEndAddress - alignedAddress, stage);
        }

        /// <summary>
        /// Creates a new buffer for the specified range, if it does not yet exist.
        /// This can be used to ensure the existance of a buffer.
        /// </summary>
        /// <param name="address">Address of the buffer in memory</param>
        /// <param name="size">Size of the buffer in bytes</param>
        /// <param name="stage">The type of usage that created the buffer</param>
        /// <param name="alignment">Alignment of the start address of the buffer in bytes</param>
        public void CreateBuffer(ulong address, ulong size, BufferStage stage, ulong alignment)
        {
            ulong alignmentMask = alignment - 1;
            ulong pageAlignmentMask = BufferAlignmentMask;
            ulong endAddress = address + size;

            ulong alignedAddress = address & ~alignmentMask;
            ulong alignedEndAddress = (endAddress + pageAlignmentMask) & ~pageAlignmentMask;

            // The buffer must have the size of at least one page.
            if (alignedEndAddress == alignedAddress)
            {
                alignedEndAddress += pageAlignmentMask;
            }

            CreateBufferAligned(alignedAddress, alignedEndAddress - alignedAddress, stage, alignment);
        }

        /// <summary>
        /// Creates a buffer for a memory region composed of multiple physical ranges,
        /// if it does not exist yet.
        /// </summary>
        /// <param name="range">Physical ranges of memory</param>
        /// <param name="stage">The type of usage that created the buffer</param>
        private void CreateMultiRangeBuffer(MultiRange range, BufferStage stage)
        {
            // Ensure all non-contiguous buffer we might use are sparse aligned.
            for (int i = 0; i < range.Count; i++)
            {
                MemoryRange subRange = range.GetSubRange(i);

                if (subRange.Address != MemoryManager.PteUnmapped)
                {
                    CreateBuffer(subRange.Address, subRange.Size, stage, SparseBufferAlignmentSize);
                }
            }

            // Create sparse buffer.
            MultiRangeBuffer[] overlaps = new MultiRangeBuffer[10];

            int overlapCount = _multiRangeBuffers.FindOverlaps(range, ref overlaps);

            for (int index = 0; index < overlapCount; index++)
            {
                if (overlaps[index].Range.Contains(range))
                {
                    return;
                }
            }

            for (int index = 0; index < overlapCount; index++)
            {
                if (range.Contains(overlaps[index].Range))
                {
                    _multiRangeBuffers.Remove(overlaps[index]);
                    overlaps[index].Dispose();
                }
            }

            MultiRangeBuffer multiRangeBuffer;

            MemoryRange[] alignedSubRanges = new MemoryRange[range.Count];

            ulong alignmentMask = SparseBufferAlignmentSize - 1;

            if (_context.Capabilities.SupportsSparseBuffer)
            {
                BufferRange[] storages = new BufferRange[range.Count];

                for (int i = 0; i < range.Count; i++)
                {
                    MemoryRange subRange = range.GetSubRange(i);

                    if (subRange.Address != MemoryManager.PteUnmapped)
                    {
                        ulong endAddress = subRange.Address + subRange.Size;

                        ulong alignedAddress = subRange.Address & ~alignmentMask;
                        ulong alignedEndAddress = (endAddress + alignmentMask) & ~alignmentMask;
                        ulong alignedSize = alignedEndAddress - alignedAddress;

                        Buffer buffer = _buffers.FindFirstOverlap(alignedAddress, alignedSize);
                        BufferRange bufferRange = buffer.GetRange(alignedAddress, alignedSize, false);

                        alignedSubRanges[i] = new MemoryRange(alignedAddress, alignedSize);
                        storages[i] = bufferRange;
                    }
                    else
                    {
                        ulong alignedSize = (subRange.Size + alignmentMask) & ~alignmentMask;

                        alignedSubRanges[i] = new MemoryRange(MemoryManager.PteUnmapped, alignedSize);
                        storages[i] = new BufferRange(BufferHandle.Null, 0, (int)alignedSize);
                    }
                }

                multiRangeBuffer = new(_context, new MultiRange(alignedSubRanges), storages);
            }
            else
            {
                for (int i = 0; i < range.Count; i++)
                {
                    MemoryRange subRange = range.GetSubRange(i);

                    if (subRange.Address != MemoryManager.PteUnmapped)
                    {
                        ulong endAddress = subRange.Address + subRange.Size;

                        ulong alignedAddress = subRange.Address & ~alignmentMask;
                        ulong alignedEndAddress = (endAddress + alignmentMask) & ~alignmentMask;
                        ulong alignedSize = alignedEndAddress - alignedAddress;

                        alignedSubRanges[i] = new MemoryRange(alignedAddress, alignedSize);
                    }
                    else
                    {
                        ulong alignedSize = (subRange.Size + alignmentMask) & ~alignmentMask;

                        alignedSubRanges[i] = new MemoryRange(MemoryManager.PteUnmapped, alignedSize);
                    }
                }

                multiRangeBuffer = new(_context, new MultiRange(alignedSubRanges));

                UpdateVirtualBufferDependencies(multiRangeBuffer);
            }

            _multiRangeBuffers.Add(multiRangeBuffer);
        }

        /// <summary>
        /// Adds two-way dependencies to all physical buffers contained within a given virtual buffer.
        /// </summary>
        /// <param name="virtualBuffer">Virtual buffer to have dependencies added</param>
        private void UpdateVirtualBufferDependencies(MultiRangeBuffer virtualBuffer)
        {
            virtualBuffer.ClearPhysicalDependencies();

            ulong dstOffset = 0;

            HashSet<Buffer> physicalBuffers = [];

            for (int i = 0; i < virtualBuffer.Range.Count; i++)
            {
                MemoryRange subRange = virtualBuffer.Range.GetSubRange(i);

                if (subRange.Address != MemoryManager.PteUnmapped)
                {
                    Buffer buffer = _buffers.FindFirstOverlap(subRange.Address, subRange.Size);

                    virtualBuffer.AddPhysicalDependency(buffer, subRange.Address, dstOffset, subRange.Size);
                    physicalBuffers.Add(buffer);
                }

                dstOffset += subRange.Size;
            }

            foreach (Buffer buffer in physicalBuffers)
            {
                buffer.CopyToDependantVirtualBuffer(virtualBuffer);
            }
        }

        /// <summary>
        /// Performs address translation of the GPU virtual address, and attempts to force
        /// the buffer in the region as dirty.
        /// The buffer lookup for this function is cached in a dictionary for quick access, which
        /// accelerates common UBO updates.
        /// </summary>
        /// <param name="memoryManager">GPU memory manager where the buffer is mapped</param>
        /// <param name="gpuVa">Start GPU virtual address of the buffer</param>
        /// <param name="size">Size in bytes of the buffer</param>
        public void ForceDirty(MemoryManager memoryManager, ulong gpuVa, ulong size)
        {
            if (_pruneCaches)
            {
                Prune();
            }

            if (!_dirtyCache.TryGetValue(gpuVa, out BufferCacheEntry result) ||
                result.EndGpuAddress < gpuVa + size ||
                result.UnmappedSequence != result.Buffer.UnmappedSequence)
            {
                MultiRange range = TranslateAndCreateBuffer(memoryManager, gpuVa, size, BufferStage.Internal);
                ulong address = range.GetSubRange(0).Address;
                result = new BufferCacheEntry(address, gpuVa, GetBuffer(address, size, BufferStage.Internal));

                _dirtyCache[gpuVa] = result;
            }

            result.Buffer.ForceDirty(result.Address, size);
        }

        /// <summary>
        /// Checks if the given buffer range has been GPU modifed.
        /// </summary>
        /// <param name="memoryManager">GPU memory manager where the buffer is mapped</param>
        /// <param name="gpuVa">Start GPU virtual address of the buffer</param>
        /// <param name="size">Size in bytes of the buffer</param>
        /// <returns>True if modified, false otherwise</returns>
        public bool CheckModified(MemoryManager memoryManager, ulong gpuVa, ulong size, out ulong outAddr)
        {
            if (_pruneCaches)
            {
                Prune();
            }

            // Align the address to avoid creating too many entries on the quick lookup dictionary.
            ulong mask = BufferAlignmentMask;
            ulong alignedGpuVa = gpuVa & (~mask);
            ulong alignedEndGpuVa = (gpuVa + size + mask) & (~mask);

            size = alignedEndGpuVa - alignedGpuVa;

            if (!_modifiedCache.TryGetValue(alignedGpuVa, out BufferCacheEntry result) ||
                result.EndGpuAddress < alignedEndGpuVa ||
                result.UnmappedSequence != result.Buffer.UnmappedSequence)
            {
                MultiRange range = TranslateAndCreateBuffer(memoryManager, alignedGpuVa, size, BufferStage.None);
                ulong address = range.GetSubRange(0).Address;
                result = new BufferCacheEntry(address, alignedGpuVa, GetBuffer(address, size, BufferStage.None));

                _modifiedCache[alignedGpuVa] = result;
            }

            outAddr = result.Address | (gpuVa & mask);

            return result.Buffer.IsModified(result.Address, size);
        }

        /// <summary>
        /// Creates a new buffer for the specified range, if needed.
        /// If a buffer where this range can be fully contained already exists,
        /// then the creation of a new buffer is not necessary.
        /// </summary>
        /// <param name="address">Address of the buffer in guest memory</param>
        /// <param name="size">Size in bytes of the buffer</param>
        /// <param name="stage">The type of usage that created the buffer</param>
        private void CreateBufferAligned(ulong address, ulong size, BufferStage stage)
        {
            Buffer[] overlaps = _bufferOverlaps;
            int overlapsCount = _buffers.FindOverlapsNonOverlapping(address, size, ref overlaps);

            if (overlapsCount != 0)
            {
                // The buffer already exists. We can just return the existing buffer
                // if the buffer we need is fully contained inside the overlapping buffer.
                // Otherwise, we must delete the overlapping buffers and create a bigger buffer
                // that fits all the data we need. We also need to copy the contents from the
                // old buffer(s) to the new buffer.

                ulong endAddress = address + size;
                Buffer overlap0 = overlaps[0];

                if (overlap0.Address > address || overlap0.EndAddress < endAddress)
                {
                    bool anySparseCompatible = false;

                    // Check if the following conditions are met:
                    // - We have a single overlap.
                    // - The overlap starts at or before the requested range. That is, the overlap happens at the end.
                    // - The size delta between the new, merged buffer and the old one is of at most 2 pages.
                    // In this case, we attempt to extend the buffer further than the requested range,
                    // this can potentially avoid future resizes if the application keeps using overlapping
                    // sequential memory.
                    // Allowing for 2 pages (rather than just one) is necessary to catch cases where the
                    // range crosses a page, and after alignment, ends having a size of 2 pages.
                    if (overlapsCount == 1 &&
                        address >= overlap0.Address &&
                        endAddress - overlap0.EndAddress <= BufferAlignmentSize * 2)
                    {
                        // Try to grow the buffer by 1.5x of its current size.
                        // This improves performance in the cases where the buffer is resized often by small amounts.
                        ulong existingSize = overlap0.Size;
                        ulong growthSize = (existingSize + Math.Min(existingSize >> 1, MaxDynamicGrowthSize)) & ~BufferAlignmentMask;

                        size = Math.Max(size, growthSize);
                        endAddress = address + size;

                        overlapsCount = _buffers.FindOverlapsNonOverlapping(address, size, ref overlaps);
                    }

                    for (int index = 0; index < overlapsCount; index++)
                    {
                        Buffer buffer = overlaps[index];

                        anySparseCompatible |= buffer.SparseCompatible;

                        address = Math.Min(address, buffer.Address);
                        endAddress = Math.Max(endAddress, buffer.EndAddress);

                        lock (_buffers)
                        {
                            _buffers.Remove(buffer);
                        }
                    }

                    ulong newSize = endAddress - address;

                    CreateBufferAligned(address, newSize, stage, anySparseCompatible, overlaps, overlapsCount);
                }
            }
            else
            {
                // No overlap, just create a new buffer.
                Buffer buffer = new(_context, _physicalMemory, address, size, stage, sparseCompatible: false);

                lock (_buffers)
                {
                    _buffers.Add(buffer);
                }
            }

            ShrinkOverlapsBufferIfNeeded();
        }

        /// <summary>
        /// Creates a new buffer for the specified range, if needed.
        /// If a buffer where this range can be fully contained already exists,
        /// then the creation of a new buffer is not necessary.
        /// </summary>
        /// <param name="address">Address of the buffer in guest memory</param>
        /// <param name="size">Size in bytes of the buffer</param>
        /// <param name="stage">The type of usage that created the buffer</param>
        /// <param name="alignment">Alignment of the start address of the buffer</param>
        private void CreateBufferAligned(ulong address, ulong size, BufferStage stage, ulong alignment)
        {
            Buffer[] overlaps = _bufferOverlaps;
            int overlapsCount = _buffers.FindOverlapsNonOverlapping(address, size, ref overlaps);
            bool sparseAligned = alignment >= SparseBufferAlignmentSize;

            if (overlapsCount != 0)
            {
                // If the buffer already exists, make sure if covers the entire range,
                // and make sure it is properly aligned, otherwise sparse mapping may fail.

                ulong endAddress = address + size;
                Buffer overlap0 = overlaps[0];

                if (overlap0.Address > address ||
                    overlap0.EndAddress < endAddress ||
                    (overlap0.Address & (alignment - 1)) != 0 ||
                    (!overlap0.SparseCompatible && sparseAligned))
                {
                    // We need to make sure the new buffer is properly aligned.
                    // However, after the range is aligned, it is possible that it
                    // overlaps more buffers, so try again after each extension
                    // and ensure we cover all overlaps.

                    int oldOverlapsCount;

                    do
                    {
                        for (int index = 0; index < overlapsCount; index++)
                        {
                            Buffer buffer = overlaps[index];

                            address = Math.Min(address, buffer.Address);
                            endAddress = Math.Max(endAddress, buffer.EndAddress);
                        }

                        address &= ~(alignment - 1);

                        oldOverlapsCount = overlapsCount;
                        overlapsCount = _buffers.FindOverlapsNonOverlapping(address, endAddress - address, ref overlaps);
                    }
                    while (oldOverlapsCount != overlapsCount);

                    lock (_buffers)
                    {
                        for (int index = 0; index < overlapsCount; index++)
                        {
                            _buffers.Remove(overlaps[index]);
                        }
                    }

                    ulong newSize = endAddress - address;

                    CreateBufferAligned(address, newSize, stage, sparseAligned, overlaps, overlapsCount);
                }
            }
            else
            {
                // No overlap, just create a new buffer.
                Buffer buffer = new(_context, _physicalMemory, address, size, stage, sparseAligned);

                lock (_buffers)
                {
                    _buffers.Add(buffer);
                }
            }

            ShrinkOverlapsBufferIfNeeded();
        }

        /// <summary>
        /// Creates a new buffer for the specified range, if needed.
        /// If a buffer where this range can be fully contained already exists,
        /// then the creation of a new buffer is not necessary.
        /// </summary>
        /// <param name="address">Address of the buffer in guest memory</param>
        /// <param name="size">Size in bytes of the buffer</param>
        /// <param name="stage">The type of usage that created the buffer</param>
        /// <param name="sparseCompatible">Indicates if the buffer can be used in a sparse buffer mapping</param>
        /// <param name="overlaps">Buffers overlapping the range</param>
        /// <param name="overlapsCount">Total of overlaps</param>
        private void CreateBufferAligned(ulong address, ulong size, BufferStage stage, bool sparseCompatible, Buffer[] overlaps, int overlapsCount)
        {
            Buffer newBuffer = new(_context, _physicalMemory, address, size, stage, sparseCompatible, overlaps.Take(overlapsCount));

            lock (_buffers)
            {
                _buffers.Add(newBuffer);
            }

            for (int index = 0; index < overlapsCount; index++)
            {
                Buffer buffer = overlaps[index];

                int dstOffset = (int)(buffer.Address - newBuffer.Address);

                buffer.CopyTo(newBuffer, dstOffset);
                newBuffer.InheritModifiedRanges(buffer);

                buffer.DecrementReferenceCount();
            }

            newBuffer.SynchronizeMemory(address, size);

            // Existing buffers were modified, we need to rebind everything.
            NotifyBuffersModified?.Invoke();

            RecreateMultiRangeBuffers(address, size);
        }

        /// <summary>
        /// Recreates all the multi-range buffers that overlaps a given physical memory range.
        /// </summary>
        /// <param name="address">Start address of the range</param>
        /// <param name="size">Size of the range in bytes</param>
        private void RecreateMultiRangeBuffers(ulong address, ulong size)
        {
            if ((address & (SparseBufferAlignmentSize - 1)) != 0 || (size & (SparseBufferAlignmentSize - 1)) != 0)
            {
                return;
            }

            MultiRangeBuffer[] overlaps = new MultiRangeBuffer[10];

            int overlapCount = _multiRangeBuffers.FindOverlaps(address, size, ref overlaps);

            for (int index = 0; index < overlapCount; index++)
            {
                _multiRangeBuffers.Remove(overlaps[index]);
                overlaps[index].Dispose();
            }

            for (int index = 0; index < overlapCount; index++)
            {
                CreateMultiRangeBuffer(overlaps[index].Range, BufferStage.None);
            }
        }

        /// <summary>
        /// Resizes the temporary buffer used for range list intersection results, if it has grown too much.
        /// </summary>
        private void ShrinkOverlapsBufferIfNeeded()
        {
            if (_bufferOverlaps.Length > OverlapsBufferMaxCapacity)
            {
                Array.Resize(ref _bufferOverlaps, OverlapsBufferMaxCapacity);
            }
        }

        /// <summary>
        /// Copy a buffer data from a given address to another.
        /// </summary>
        /// <remarks>
        /// This does a GPU side copy.
        /// </remarks>
        /// <param name="memoryManager">GPU memory manager where the buffer is mapped</param>
        /// <param name="srcVa">GPU virtual address of the copy source</param>
        /// <param name="dstVa">GPU virtual address of the copy destination</param>
        /// <param name="size">Size in bytes of the copy</param>
        public void CopyBuffer(MemoryManager memoryManager, ulong srcVa, ulong dstVa, ulong size)
        {
            MultiRange srcRange = TranslateAndCreateMultiBuffersPhysicalOnly(memoryManager, srcVa, size, BufferStage.Copy);
            MultiRange dstRange = TranslateAndCreateMultiBuffersPhysicalOnly(memoryManager, dstVa, size, BufferStage.Copy);

            if (srcRange.Count == 1 && dstRange.Count == 1)
            {
                CopyBufferSingleRange(memoryManager, srcRange.GetSubRange(0).Address, dstRange.GetSubRange(0).Address, size);
            }
            else
            {
                ulong copiedSize = 0;
                ulong srcOffset = 0;
                ulong dstOffset = 0;
                int srcRangeIndex = 0;
                int dstRangeIndex = 0;

                while (copiedSize < size)
                {
                    if (srcRange.GetSubRange(srcRangeIndex).Size == srcOffset)
                    {
                        srcRangeIndex++;
                        srcOffset = 0;
                    }

                    if (dstRange.GetSubRange(dstRangeIndex).Size == dstOffset)
                    {
                        dstRangeIndex++;
                        dstOffset = 0;
                    }

                    MemoryRange srcSubRange = srcRange.GetSubRange(srcRangeIndex);
                    MemoryRange dstSubRange = dstRange.GetSubRange(dstRangeIndex);

                    ulong srcSize = srcSubRange.Size - srcOffset;
                    ulong dstSize = dstSubRange.Size - dstOffset;
                    ulong copySize = Math.Min(srcSize, dstSize);

                    CopyBufferSingleRange(memoryManager, srcSubRange.Address + srcOffset, dstSubRange.Address + dstOffset, copySize);

                    srcOffset += copySize;
                    dstOffset += copySize;
                    copiedSize += copySize;
                }
            }
        }

        /// <summary>
        /// Copy a buffer data from a given address to another.
        /// </summary>
        /// <remarks>
        /// This does a GPU side copy.
        /// </remarks>
        /// <param name="memoryManager">GPU memory manager where the buffer is mapped</param>
        /// <param name="srcAddress">Physical address of the copy source</param>
        /// <param name="dstAddress">Physical address of the copy destination</param>
        /// <param name="size">Size in bytes of the copy</param>
        private void CopyBufferSingleRange(MemoryManager memoryManager, ulong srcAddress, ulong dstAddress, ulong size)
        {
            Buffer srcBuffer = GetBuffer(srcAddress, size, BufferStage.Copy);
            Buffer dstBuffer = GetBuffer(dstAddress, size, BufferStage.Copy);

            int srcOffset = (int)(srcAddress - srcBuffer.Address);
            int dstOffset = (int)(dstAddress - dstBuffer.Address);

            _context.Renderer.Pipeline.CopyBuffer(
                srcBuffer.Handle,
                dstBuffer.Handle,
                srcOffset,
                dstOffset,
                (int)size);

            if (srcBuffer.IsModified(srcAddress, size))
            {
                dstBuffer.SignalModified(dstAddress, size, BufferStage.Copy);
            }
            else
            {
                // Optimization: If the data being copied is already in memory, then copy it directly instead of flushing from GPU.

                dstBuffer.ClearModified(dstAddress, size);
                memoryManager.Physical.WriteTrackedResource(dstAddress, memoryManager.Physical.GetSpan(srcAddress, (int)size), ResourceKind.Buffer);
            }

            dstBuffer.CopyToDependantVirtualBuffers(dstAddress, size);
        }

        /// <summary>
        /// Clears a buffer at a given address with the specified value.
        /// </summary>
        /// <remarks>
        /// Both the address and size must be aligned to 4 bytes.
        /// </remarks>
        /// <param name="memoryManager">GPU memory manager where the buffer is mapped</param>
        /// <param name="gpuVa">GPU virtual address of the region to clear</param>
        /// <param name="size">Number of bytes to clear</param>
        /// <param name="value">Value to be written into the buffer</param>
        public void ClearBuffer(MemoryManager memoryManager, ulong gpuVa, ulong size, uint value)
        {
            MultiRange range = TranslateAndCreateMultiBuffersPhysicalOnly(memoryManager, gpuVa, size, BufferStage.Copy);

            for (int index = 0; index < range.Count; index++)
            {
                MemoryRange subRange = range.GetSubRange(index);
                Buffer buffer = GetBuffer(subRange.Address, subRange.Size, BufferStage.Copy);

                int offset = (int)(subRange.Address - buffer.Address);

                _context.Renderer.Pipeline.ClearBuffer(buffer.Handle, offset, (int)subRange.Size, value);

                memoryManager.Physical.FillTrackedResource(subRange.Address, subRange.Size, value, ResourceKind.Buffer);

                buffer.CopyToDependantVirtualBuffers(subRange.Address, subRange.Size);
            }
        }

        /// <summary>
        /// Gets a buffer sub-range starting at a given memory address, aligned to the next page boundary.
        /// </summary>
        /// <param name="range">Physical regions of memory where the buffer is mapped</param>
        /// <param name="stage">Buffer stage that triggered the access</param>
        /// <param name="write">Whether the buffer will be written to by this use</param>
        /// <returns>The buffer sub-range starting at the given memory address</returns>
        public BufferRange GetBufferRangeAligned(MultiRange range, BufferStage stage, bool write = false)
        {
            if (range.Count > 1)
            {
                return GetBuffer(range, stage, write).GetRange(range);
            }
            else
            {
                MemoryRange subRange = range.GetSubRange(0);
                return GetBuffer(subRange.Address, subRange.Size, stage, write).GetRangeAligned(subRange.Address, subRange.Size, write);
            }
        }

        /// <summary>
        /// Gets a buffer sub-range for a given memory range.
        /// </summary>
        /// <param name="range">Physical regions of memory where the buffer is mapped</param>
        /// <param name="stage">Buffer stage that triggered the access</param>
        /// <param name="write">Whether the buffer will be written to by this use</param>
        /// <returns>The buffer sub-range for the given range</returns>
        public BufferRange GetBufferRange(MultiRange range, BufferStage stage, bool write = false)
        {
            if (range.Count > 1)
            {
                return GetBuffer(range, stage, write).GetRange(range);
            }
            else
            {
                MemoryRange subRange = range.GetSubRange(0);
                return GetBuffer(subRange.Address, subRange.Size, stage, write).GetRange(subRange.Address, subRange.Size, write);
            }
        }

        /// <summary>
        /// Gets a buffer for a given memory range.
        /// A buffer overlapping with the specified range is assumed to already exist on the cache.
        /// </summary>
        /// <param name="range">Physical regions of memory where the buffer is mapped</param>
        /// <param name="stage">Buffer stage that triggered the access</param>
        /// <param name="write">Whether the buffer will be written to by this use</param>
        /// <returns>The buffer where the range is fully contained</returns>
        private MultiRangeBuffer GetBuffer(MultiRange range, BufferStage stage, bool write = false)
        {
            for (int i = 0; i < range.Count; i++)
            {
                MemoryRange subRange = range.GetSubRange(i);

                Buffer subBuffer = _buffers.FindFirstOverlap(subRange.Address, subRange.Size);

                subBuffer.SynchronizeMemory(subRange.Address, subRange.Size);

                if (write)
                {
                    subBuffer.SignalModified(subRange.Address, subRange.Size, stage);
                }
            }

            MultiRangeBuffer[] overlaps = new MultiRangeBuffer[10];

            int overlapCount = _multiRangeBuffers.FindOverlaps(range, ref overlaps);

            MultiRangeBuffer buffer = null;

            for (int i = 0; i < overlapCount; i++)
            {
                if (overlaps[i].Range.Contains(range))
                {
                    buffer = overlaps[i];
                    break;
                }
            }

            if (write && buffer != null && !_context.Capabilities.SupportsSparseBuffer)
            {
                buffer.AddModifiedRegion(range, ++_virtualModifiedSequenceNumber);
            }

            return buffer;
        }

        /// <summary>
        /// Gets a buffer for a given memory range.
        /// A buffer overlapping with the specified range is assumed to already exist on the cache.
        /// </summary>
        /// <param name="address">Start address of the memory range</param>
        /// <param name="size">Size in bytes of the memory range</param>
        /// <param name="stage">Buffer stage that triggered the access</param>
        /// <param name="write">Whether the buffer will be written to by this use</param>
        /// <returns>The buffer where the range is fully contained</returns>
        private Buffer GetBuffer(ulong address, ulong size, BufferStage stage, bool write = false)
        {
            Buffer buffer;

            if (size != 0)
            {
                buffer = _buffers.FindFirstOverlap(address, size);

                buffer.CopyFromDependantVirtualBuffers();
                buffer.SynchronizeMemory(address, size);

                if (write)
                {
                    buffer.SignalModified(address, size, stage);
                }
            }
            else
            {
                buffer = _buffers.FindFirstOverlap(address, 1);
            }

            return buffer;
        }

        /// <summary>
        /// Performs guest to host memory synchronization of a given memory range.
        /// </summary>
        /// <param name="range">Physical regions of memory where the buffer is mapped</param>
        public void SynchronizeBufferRange(MultiRange range)
        {
            if (range.Count == 1)
            {
                MemoryRange subRange = range.GetSubRange(0);
                SynchronizeBufferRange(subRange.Address, subRange.Size, copyBackVirtual: true);
            }
            else
            {
                for (int index = 0; index < range.Count; index++)
                {
                    MemoryRange subRange = range.GetSubRange(index);
                    SynchronizeBufferRange(subRange.Address, subRange.Size, copyBackVirtual: false);
                }
            }
        }

        /// <summary>
        /// Performs guest to host memory synchronization of a given memory range.
        /// </summary>
        /// <param name="address">Start address of the memory range</param>
        /// <param name="size">Size in bytes of the memory range</param>
        /// <param name="copyBackVirtual">Whether virtual buffers that uses this buffer as backing memory should have its data copied back if modified</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SynchronizeBufferRange(ulong address, ulong size, bool copyBackVirtual)
        {
            if (size != 0)
            {
                Buffer buffer = _buffers.FindFirstOverlap(address, size);

                if (copyBackVirtual)
                {
                    buffer.CopyFromDependantVirtualBuffers();
                }

                buffer.SynchronizeMemory(address, size);
            }
        }

        /// <summary>
        /// Signal that the given buffer's handle has changed,
        /// forcing rebind and any overlapping multi-range buffers to be recreated.
        /// </summary>
        /// <param name="buffer">The buffer that has changed handle</param>
        public void BufferBackingChanged(Buffer buffer)
        {
            NotifyBuffersModified?.Invoke();

            RecreateMultiRangeBuffers(buffer.Address, buffer.Size);
        }

        /// <summary>
        /// Prune any invalid entries from a quick access dictionary.
        /// </summary>
        /// <param name="dictionary">Dictionary to prune</param>
        /// <param name="toDelete">List used to track entries to delete</param>
        private static void Prune(Dictionary<ulong, BufferCacheEntry> dictionary, ref List<ulong> toDelete)
        {
            foreach (KeyValuePair<ulong, BufferCacheEntry> entry in dictionary)
            {
                if (entry.Value.UnmappedSequence != entry.Value.Buffer.UnmappedSequence)
                {
                    (toDelete ??= []).Add(entry.Key);
                }
            }

            if (toDelete != null)
            {
                foreach (ulong entry in toDelete)
                {
                    dictionary.Remove(entry);
                }
            }
        }

        /// <summary>
        /// Prune any invalid entries from the quick access dictionaries.
        /// </summary>
        private void Prune()
        {
            List<ulong> toDelete = null;

            Prune(_dirtyCache, ref toDelete);

            toDelete?.Clear();

            Prune(_modifiedCache, ref toDelete);

            _pruneCaches = false;
        }

        /// <summary>
        /// Queues a prune of invalid entries the next time a dictionary cache is accessed.
        /// </summary>
        public void QueuePrune()
        {
            _pruneCaches = true;
        }

        /// <summary>
        /// Disposes all buffers in the cache.
        /// It's an error to use the buffer cache after disposal.
        /// </summary>
        public void Dispose()
        {
            lock (_buffers)
            {
                foreach (Buffer buffer in _buffers)
                {
                    buffer.Dispose();
                }
            }
        }
    }
}
