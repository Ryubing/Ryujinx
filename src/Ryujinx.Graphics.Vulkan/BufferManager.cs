using Ryujinx.Common.Logging;
using Ryujinx.Graphics.GAL;
using Silk.NET.Vulkan;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using VkBuffer = Silk.NET.Vulkan.Buffer;
using VkFormat = Silk.NET.Vulkan.Format;

namespace Ryujinx.Graphics.Vulkan
{
    readonly struct ScopedTemporaryBuffer : IDisposable
    {
        private readonly BufferManager _bufferManager;
        private readonly bool _isReserved;

        public readonly BufferRange Range;
        public readonly BufferHolder Holder;

        public BufferHandle Handle => Range.Handle;
        public int Offset => Range.Offset;

        public ScopedTemporaryBuffer(BufferManager bufferManager, BufferHolder holder, BufferHandle handle, int offset, int size, bool isReserved)
        {
            _bufferManager = bufferManager;

            Range = new BufferRange(handle, offset, size);
            Holder = holder;

            _isReserved = isReserved;
        }

        public void Dispose()
        {
            if (!_isReserved)
            {
                _bufferManager.Delete(Range.Handle);
            }
        }
    }

    class BufferManager : IDisposable
    {
        public const MemoryPropertyFlags DefaultBufferMemoryFlags =
            MemoryPropertyFlags.HostVisibleBit |
            MemoryPropertyFlags.HostCoherentBit |
            MemoryPropertyFlags.HostCachedBit;

        // Some drivers don't expose a "HostCached" memory type,
        // so we need those alternative flags for the allocation to succeed there.
        private const MemoryPropertyFlags DefaultBufferMemoryNoCacheFlags =
            MemoryPropertyFlags.HostVisibleBit |
            MemoryPropertyFlags.HostCoherentBit;

        private const MemoryPropertyFlags DeviceLocalBufferMemoryFlags =
            MemoryPropertyFlags.DeviceLocalBit;

        private const MemoryPropertyFlags DeviceLocalMappedBufferMemoryFlags =
            MemoryPropertyFlags.DeviceLocalBit |
            MemoryPropertyFlags.HostVisibleBit |
            MemoryPropertyFlags.HostCoherentBit;

        private const BufferUsageFlags DefaultBufferUsageFlags =
            BufferUsageFlags.TransferSrcBit |
            BufferUsageFlags.TransferDstBit |
            BufferUsageFlags.UniformTexelBufferBit |
            BufferUsageFlags.StorageTexelBufferBit |
            BufferUsageFlags.UniformBufferBit |
            BufferUsageFlags.StorageBufferBit |
            BufferUsageFlags.IndexBufferBit |
            BufferUsageFlags.VertexBufferBit |
            BufferUsageFlags.TransformFeedbackBufferBitExt;

        private const BufferUsageFlags HostImportedBufferUsageFlags =
            BufferUsageFlags.TransferSrcBit |
            BufferUsageFlags.TransferDstBit;

        private readonly Device _device;

        private readonly IdList<BufferHolder> _buffers;

        public int BufferCount { get; private set; }

        public StagingBuffer StagingBuffer { get; }

        public MemoryRequirements HostImportedBufferMemoryRequirements { get; }

        public BufferManager(VulkanRenderer gd, Device device)
        {
            _device = device;
            _buffers = new IdList<BufferHolder>();
            StagingBuffer = new StagingBuffer(gd, this);

            HostImportedBufferMemoryRequirements = GetHostImportedUsageRequirements(gd);
        }

        public unsafe BufferHandle CreateHostImported(VulkanRenderer gd, nint pointer, int size)
        {
            BufferUsageFlags usage = HostImportedBufferUsageFlags;

            if (gd.Capabilities.SupportsIndirectParameters)
            {
                usage |= BufferUsageFlags.IndirectBufferBit;
            }

            ExternalMemoryBufferCreateInfo externalMemoryBuffer = new()
            {
                SType = StructureType.ExternalMemoryBufferCreateInfo,
                HandleTypes = ExternalMemoryHandleTypeFlags.HostAllocationBitExt,
            };

            BufferCreateInfo bufferCreateInfo = new()
            {
                SType = StructureType.BufferCreateInfo,
                Size = (ulong)size,
                Usage = usage,
                SharingMode = SharingMode.Exclusive,
                PNext = &externalMemoryBuffer,
            };

            gd.Api.CreateBuffer(_device, in bufferCreateInfo, null, out VkBuffer buffer).ThrowOnError();

            (Auto<MemoryAllocation> allocation, ulong offset) = gd.HostMemoryAllocator.GetExistingAllocation(pointer, (ulong)size);

            gd.Api.BindBufferMemory(_device, buffer, allocation.GetUnsafe().Memory, allocation.GetUnsafe().Offset + offset);

            BufferHolder holder = new(gd, _device, buffer, allocation, size, BufferAllocationType.HostMapped, BufferAllocationType.HostMapped, (int)offset);

            BufferCount++;

            ulong handle64 = (uint)_buffers.Add(holder);

            return Unsafe.As<ulong, BufferHandle>(ref handle64);
        }

        public unsafe BufferHandle CreateSparse(VulkanRenderer gd, ReadOnlySpan<BufferRange> storageBuffers)
        {
            BufferUsageFlags usage = DefaultBufferUsageFlags;

            if (gd.Capabilities.SupportsIndirectParameters)
            {
                usage |= BufferUsageFlags.IndirectBufferBit;
            }

            ulong size = 0;

            foreach (BufferRange range in storageBuffers)
            {
                size += (ulong)range.Size;
            }

            BufferCreateInfo bufferCreateInfo = new()
            {
                SType = StructureType.BufferCreateInfo,
                Size = size,
                Usage = usage,
                SharingMode = SharingMode.Exclusive,
                Flags = BufferCreateFlags.SparseBindingBit | BufferCreateFlags.SparseAliasedBit
            };

            gd.Api.CreateBuffer(_device, in bufferCreateInfo, null, out VkBuffer buffer).ThrowOnError();

            SparseMemoryBind[] memoryBinds = new SparseMemoryBind[storageBuffers.Length];
            Auto<MemoryAllocation>[] storageAllocations = new Auto<MemoryAllocation>[storageBuffers.Length];
            int storageAllocationsCount = 0;

            ulong dstOffset = 0;

            for (int index = 0; index < storageBuffers.Length; index++)
            {
                BufferRange range = storageBuffers[index];

                if (TryGetBuffer(range.Handle, out BufferHolder existingHolder))
                {
                    (DeviceMemory memory, ulong offset) = existingHolder.GetDeviceMemoryAndOffset();

                    memoryBinds[index] = new SparseMemoryBind()
                    {
                        ResourceOffset = dstOffset,
                        Size = (ulong)range.Size,
                        Memory = memory,
                        MemoryOffset = offset + (ulong)range.Offset,
                        Flags = SparseMemoryBindFlags.None
                    };

                    storageAllocations[storageAllocationsCount++] = existingHolder.GetAllocation();
                }
                else
                {
                    memoryBinds[index] = new SparseMemoryBind()
                    {
                        ResourceOffset = dstOffset,
                        Size = (ulong)range.Size,
                        Memory = default,
                        MemoryOffset = 0UL,
                        Flags = SparseMemoryBindFlags.None
                    };
                }

                dstOffset += (ulong)range.Size;
            }

            if (storageAllocations.Length != storageAllocationsCount)
            {
                Array.Resize(ref storageAllocations, storageAllocationsCount);
            }

            fixed (SparseMemoryBind* pMemoryBinds = memoryBinds)
            {
                SparseBufferMemoryBindInfo bufferBind = new()
                {
                    Buffer = buffer,
                    BindCount = (uint)memoryBinds.Length,
                    PBinds = pMemoryBinds
                };

                BindSparseInfo bindSparseInfo = new()
                {
                    SType = StructureType.BindSparseInfo,
                    BufferBindCount = 1,
                    PBufferBinds = &bufferBind
                };

                gd.Api.QueueBindSparse(gd.Queue, 1, in bindSparseInfo, default).ThrowOnError();
            }

            BufferHolder holder = new(gd, _device, buffer, (int)size, storageAllocations);

            BufferCount++;

            ulong handle64 = (uint)_buffers.Add(holder);

            return Unsafe.As<ulong, BufferHandle>(ref handle64);
        }

        public BufferHandle CreateWithHandle(
            VulkanRenderer gd,
            int size,
            bool sparseCompatible = false,
            BufferAllocationType baseType = BufferAllocationType.HostMapped,
            bool forceMirrors = false)
        {
            return CreateWithHandle(gd, size, out _, sparseCompatible, baseType, forceMirrors);
        }

        public BufferHandle CreateWithHandle(
            VulkanRenderer gd,
            int size,
            out BufferHolder holder,
            bool sparseCompatible = false,
            BufferAllocationType baseType = BufferAllocationType.HostMapped,
            bool forceMirrors = false)
        {
            holder = Create(gd, size, forConditionalRendering: false, sparseCompatible, baseType);
            if (holder == null)
            {
                return BufferHandle.Null;
            }

            if (forceMirrors)
            {
                holder.UseMirrors();
            }

            BufferCount++;

            ulong handle64 = (uint)_buffers.Add(holder);

            return Unsafe.As<ulong, BufferHandle>(ref handle64);
        }

        public ScopedTemporaryBuffer ReserveOrCreate(VulkanRenderer gd, CommandBufferScoped cbs, int size)
        {
            StagingBufferReserved? result = StagingBuffer.TryReserveData(cbs, size);

            if (result.HasValue)
            {
                return new ScopedTemporaryBuffer(this, result.Value.Buffer, StagingBuffer.Handle, result.Value.Offset, result.Value.Size, true);
            }
            else
            {
                // Create a temporary buffer.
                BufferHandle handle = CreateWithHandle(gd, size, out BufferHolder holder);

                return new ScopedTemporaryBuffer(this, holder, handle, 0, size, false);
            }
        }

        public unsafe MemoryRequirements GetHostImportedUsageRequirements(VulkanRenderer gd)
        {
            BufferUsageFlags usage = HostImportedBufferUsageFlags;

            if (gd.Capabilities.SupportsIndirectParameters)
            {
                usage |= BufferUsageFlags.IndirectBufferBit;
            }

            BufferCreateInfo bufferCreateInfo = new()
            {
                SType = StructureType.BufferCreateInfo,
                Size = (ulong)Environment.SystemPageSize,
                Usage = usage,
                SharingMode = SharingMode.Exclusive,
            };

            gd.Api.CreateBuffer(_device, in bufferCreateInfo, null, out VkBuffer buffer).ThrowOnError();

            gd.Api.GetBufferMemoryRequirements(_device, buffer, out MemoryRequirements requirements);

            gd.Api.DestroyBuffer(_device, buffer, null);

            return requirements;
        }

        public unsafe (VkBuffer buffer, MemoryAllocation allocation, BufferAllocationType resultType) CreateBacking(
            VulkanRenderer gd,
            int size,
            BufferAllocationType type,
            bool forConditionalRendering = false,
            bool sparseCompatible = false,
            BufferAllocationType fallbackType = BufferAllocationType.Auto)
        {
            BufferUsageFlags usage = DefaultBufferUsageFlags;

            if (forConditionalRendering && gd.Capabilities.SupportsConditionalRendering)
            {
                usage |= BufferUsageFlags.ConditionalRenderingBitExt;
            }
            else if (gd.Capabilities.SupportsIndirectParameters)
            {
                usage |= BufferUsageFlags.IndirectBufferBit;
            }

            BufferCreateInfo bufferCreateInfo = new()
            {
                SType = StructureType.BufferCreateInfo,
                Size = (ulong)size,
                Usage = usage,
                SharingMode = SharingMode.Exclusive,
            };

            gd.Api.CreateBuffer(_device, in bufferCreateInfo, null, out VkBuffer buffer).ThrowOnError();
            gd.Api.GetBufferMemoryRequirements(_device, buffer, out MemoryRequirements requirements);

            if (sparseCompatible)
            {
                requirements.Alignment = Math.Max(requirements.Alignment, Constants.SparseBufferAlignment);
            }

            MemoryAllocation allocation;

            do
            {
                MemoryPropertyFlags allocateFlags = type switch
                {
                    BufferAllocationType.HostMappedNoCache => DefaultBufferMemoryNoCacheFlags,
                    BufferAllocationType.HostMapped => DefaultBufferMemoryFlags,
                    BufferAllocationType.DeviceLocal => DeviceLocalBufferMemoryFlags,
                    BufferAllocationType.DeviceLocalMapped => DeviceLocalMappedBufferMemoryFlags,
                    _ => DefaultBufferMemoryFlags,
                };

                // If an allocation with this memory type fails, fall back to the previous one.
                try
                {
                    allocation = gd.MemoryAllocator.AllocateDeviceMemory(requirements, allocateFlags, true);
                }
                catch (VulkanException)
                {
                    allocation = default;
                }
            }
            while (allocation.Memory.Handle == 0 && (--type != fallbackType));

            if (allocation.Memory.Handle == 0UL)
            {
                gd.Api.DestroyBuffer(_device, buffer, null);
                return default;
            }

            gd.Api.BindBufferMemory(_device, buffer, allocation.Memory, allocation.Offset);

            return (buffer, allocation, type);
        }

        public BufferHolder Create(
            VulkanRenderer gd,
            int size,
            bool forConditionalRendering = false,
            bool sparseCompatible = false,
            BufferAllocationType baseType = BufferAllocationType.HostMapped)
        {
            BufferAllocationType type = baseType;

            if (baseType == BufferAllocationType.Auto)
            {
                type = BufferAllocationType.HostMapped;
            }

            (VkBuffer buffer, MemoryAllocation allocation, BufferAllocationType resultType) =
                CreateBacking(gd, size, type, forConditionalRendering, sparseCompatible);

            if (buffer.Handle != 0)
            {
                BufferHolder holder = new(gd, _device, buffer, allocation, size, baseType, resultType);

                return holder;
            }

            Logger.Error?.Print(LogClass.Gpu, $"Failed to create buffer with size 0x{size:X} and type \"{baseType}\".");

            return null;
        }

        public Auto<DisposableBufferView> CreateView(BufferHandle handle, VkFormat format, int offset, int size, Action invalidateView)
        {
            if (TryGetBuffer(handle, out BufferHolder holder))
            {
                return holder.CreateView(format, offset, size, invalidateView);
            }

            return null;
        }

        public Auto<DisposableBuffer> GetBuffer(CommandBuffer commandBuffer, BufferHandle handle, bool isWrite, bool isSSBO = false)
        {
            if (TryGetBuffer(handle, out BufferHolder holder))
            {
                return holder.GetBuffer(commandBuffer, isWrite, isSSBO);
            }

            return null;
        }

        public Auto<DisposableBuffer> GetBuffer(CommandBuffer commandBuffer, BufferHandle handle, int offset, int size, bool isWrite)
        {
            if (TryGetBuffer(handle, out BufferHolder holder))
            {
                return holder.GetBuffer(commandBuffer, offset, size, isWrite);
            }

            return null;
        }

        public Auto<DisposableBuffer> GetBufferI8ToI16(CommandBufferScoped cbs, BufferHandle handle, int offset, int size)
        {
            if (TryGetBuffer(handle, out BufferHolder holder))
            {
                return holder.GetBufferI8ToI16(cbs, offset, size);
            }

            return null;
        }

        public Auto<DisposableBuffer> GetAlignedVertexBuffer(CommandBufferScoped cbs, BufferHandle handle, int offset, int size, int stride, int alignment)
        {
            if (TryGetBuffer(handle, out BufferHolder holder))
            {
                return holder.GetAlignedVertexBuffer(cbs, offset, size, stride, alignment);
            }

            return null;
        }

        public Auto<DisposableBuffer> GetBufferTopologyConversion(CommandBufferScoped cbs, BufferHandle handle, int offset, int size, IndexBufferPattern pattern, int indexSize)
        {
            if (TryGetBuffer(handle, out BufferHolder holder))
            {
                return holder.GetBufferTopologyConversion(cbs, offset, size, pattern, indexSize);
            }

            return null;
        }

        public (Auto<DisposableBuffer>, Auto<DisposableBuffer>) GetBufferTopologyConversionIndirect(
            VulkanRenderer gd,
            CommandBufferScoped cbs,
            BufferRange indexBuffer,
            BufferRange indirectBuffer,
            BufferRange drawCountBuffer,
            IndexBufferPattern pattern,
            int indexSize,
            bool hasDrawCount,
            int maxDrawCount,
            int indirectDataStride)
        {
            BufferHolder drawCountBufferHolder = null;

            if (!TryGetBuffer(indexBuffer.Handle, out BufferHolder indexBufferHolder) ||
                !TryGetBuffer(indirectBuffer.Handle, out BufferHolder indirectBufferHolder) ||
                (hasDrawCount && !TryGetBuffer(drawCountBuffer.Handle, out drawCountBufferHolder)))
            {
                return (null, null);
            }

            TopologyConversionIndirectCacheKey indexBufferKey = new(
                gd,
                pattern,
                indexSize,
                indirectBufferHolder,
                indirectBuffer.Offset,
                indirectBuffer.Size);

            bool hasConvertedIndexBuffer = indexBufferHolder.TryGetCachedConvertedBuffer(
                indexBuffer.Offset,
                indexBuffer.Size,
                indexBufferKey,
                out BufferHolder convertedIndexBuffer);

            IndirectDataCacheKey indirectBufferKey = new(pattern);
            bool hasConvertedIndirectBuffer = indirectBufferHolder.TryGetCachedConvertedBuffer(
                indirectBuffer.Offset,
                indirectBuffer.Size,
                indirectBufferKey,
                out BufferHolder convertedIndirectBuffer);

            DrawCountCacheKey drawCountBufferKey = new();
            bool hasCachedDrawCount = true;

            if (hasDrawCount)
            {
                hasCachedDrawCount = drawCountBufferHolder.TryGetCachedConvertedBuffer(
                    drawCountBuffer.Offset,
                    drawCountBuffer.Size,
                    drawCountBufferKey,
                    out _);
            }

            if (!hasConvertedIndexBuffer || !hasConvertedIndirectBuffer || !hasCachedDrawCount)
            {
                // The destination index size is always I32.

                int indexCount = indexBuffer.Size / indexSize;

                int convertedCount = pattern.GetConvertedCount(indexCount);

                if (!hasConvertedIndexBuffer)
                {
                    convertedIndexBuffer = Create(gd, convertedCount * 4);
                    indexBufferKey.SetBuffer(convertedIndexBuffer.GetBuffer());
                    indexBufferHolder.AddCachedConvertedBuffer(indexBuffer.Offset, indexBuffer.Size, indexBufferKey, convertedIndexBuffer);
                }

                if (!hasConvertedIndirectBuffer)
                {
                    convertedIndirectBuffer = Create(gd, indirectBuffer.Size);
                    indirectBufferHolder.AddCachedConvertedBuffer(indirectBuffer.Offset, indirectBuffer.Size, indirectBufferKey, convertedIndirectBuffer);
                }

                gd.PipelineInternal.EndRenderPass();
                gd.HelperShader.ConvertIndexBufferIndirect(
                    gd,
                    cbs,
                    indirectBufferHolder,
                    convertedIndirectBuffer,
                    drawCountBuffer,
                    indexBufferHolder,
                    convertedIndexBuffer,
                    pattern,
                    indexSize,
                    indexBuffer.Offset,
                    indexBuffer.Size,
                    indirectBuffer.Offset,
                    hasDrawCount,
                    maxDrawCount,
                    indirectDataStride);

                // Any modification of the indirect buffer should invalidate the index buffers that are associated with it,
                // since we used the indirect data to find the range of the index buffer that is used.

                Dependency indexBufferDependency = new(
                    indexBufferHolder,
                    indexBuffer.Offset,
                    indexBuffer.Size,
                    indexBufferKey);

                indirectBufferHolder.AddCachedConvertedBufferDependency(
                    indirectBuffer.Offset,
                    indirectBuffer.Size,
                    indirectBufferKey,
                    indexBufferDependency);

                if (hasDrawCount)
                {
                    if (!hasCachedDrawCount)
                    {
                        drawCountBufferHolder.AddCachedConvertedBuffer(drawCountBuffer.Offset, drawCountBuffer.Size, drawCountBufferKey, null);
                    }

                    // If we have a draw count, any modification of the draw count should invalidate all indirect buffers
                    // where we used it to find the range of indirect data that is actually used.

                    Dependency indirectBufferDependency = new(
                        indirectBufferHolder,
                        indirectBuffer.Offset,
                        indirectBuffer.Size,
                        indirectBufferKey);

                    drawCountBufferHolder.AddCachedConvertedBufferDependency(
                        drawCountBuffer.Offset,
                        drawCountBuffer.Size,
                        drawCountBufferKey,
                        indirectBufferDependency);
                }
            }

            return (convertedIndexBuffer.GetBuffer(), convertedIndirectBuffer.GetBuffer());
        }

        public Auto<DisposableBuffer> GetBuffer(CommandBuffer commandBuffer, BufferHandle handle, bool isWrite, out int size)
        {
            if (TryGetBuffer(handle, out BufferHolder holder))
            {
                size = holder.Size;
                return holder.GetBuffer(commandBuffer, isWrite);
            }

            size = 0;
            return null;
        }

        public PinnedSpan<byte> GetData(BufferHandle handle, int offset, int size)
        {
            if (TryGetBuffer(handle, out BufferHolder holder))
            {
                return holder.GetData(offset, size);
            }

            return new PinnedSpan<byte>();
        }

        public void SetData<T>(BufferHandle handle, int offset, ReadOnlySpan<T> data) where T : unmanaged
        {
            SetData(handle, offset, MemoryMarshal.Cast<T, byte>(data), null, null);
        }

        public void SetData(BufferHandle handle, int offset, ReadOnlySpan<byte> data, CommandBufferScoped? cbs, Action endRenderPass)
        {
            if (TryGetBuffer(handle, out BufferHolder holder))
            {
                holder.SetData(offset, data, cbs, endRenderPass);
            }
        }

        public void Delete(BufferHandle handle)
        {
            if (TryGetBuffer(handle, out BufferHolder holder))
            {
                holder.Dispose();
                _buffers.Remove((int)Unsafe.As<BufferHandle, ulong>(ref handle));
            }
        }

        private bool TryGetBuffer(BufferHandle handle, out BufferHolder holder)
        {
            return _buffers.TryGetValue((int)Unsafe.As<BufferHandle, ulong>(ref handle), out holder);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                StagingBuffer.Dispose();

                foreach (BufferHolder buffer in _buffers)
                {
                    buffer.Dispose();
                }

                _buffers.Clear();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
