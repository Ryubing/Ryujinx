using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Ryujinx.Graphics.Vulkan
{
    class MemoryAllocator : IDisposable
    {
        private const ulong MaxDeviceMemoryUsageEstimate = 16UL * 1024 * 1024 * 1024;

        private readonly Vk _api;
        private readonly VulkanPhysicalDevice _physicalDevice;
        private readonly Device _device;
        private readonly List<MemoryAllocatorBlockList> _blockLists;
        private readonly int _blockAlignment;
        private readonly ReaderWriterLockSlim _lock;

        public MemoryAllocator(Vk api, VulkanPhysicalDevice physicalDevice, Device device)
        {
            _api = api;
            _physicalDevice = physicalDevice;
            _device = device;
            _blockLists = [];
            _blockAlignment = (int)Math.Min(int.MaxValue, MaxDeviceMemoryUsageEstimate / _physicalDevice.PhysicalDeviceProperties.Limits.MaxMemoryAllocationCount);
            _lock = new(LockRecursionPolicy.NoRecursion);
        }

        public MemoryAllocation AllocateDeviceMemory(
            MemoryRequirements requirements,
            MemoryPropertyFlags flags = 0,
            bool isBuffer = false)
        {
            int memoryTypeIndex = FindSuitableMemoryTypeIndex(requirements.MemoryTypeBits, flags);
            if (memoryTypeIndex < 0)
            {
                return default;
            }

            bool map = flags.HasFlag(MemoryPropertyFlags.HostVisibleBit);
            return Allocate(memoryTypeIndex, requirements.Size, requirements.Alignment, map, isBuffer);
        }

        private MemoryAllocation Allocate(int memoryTypeIndex, ulong size, ulong alignment, bool map, bool isBuffer)
        {
            _lock.EnterReadLock();

            try
            {
                for (int i = 0; i < _blockLists.Count; i++)
                {
                    MemoryAllocatorBlockList bl = _blockLists[i];
                    if (bl.MemoryTypeIndex == memoryTypeIndex && bl.ForBuffer == isBuffer)
                    {
                        return bl.Allocate(size, alignment, map);
                    }
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }

            _lock.EnterWriteLock();

            try
            {
                MemoryAllocatorBlockList newBl = new(_api, _device, memoryTypeIndex, _blockAlignment, isBuffer);
                _blockLists.Add(newBl);

                return newBl.Allocate(size, alignment, map);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        internal int FindSuitableMemoryTypeIndex(uint memoryTypeBits, MemoryPropertyFlags flags)
        {
            for (int i = 0; i < _physicalDevice.PhysicalDeviceMemoryProperties.MemoryTypeCount; i++)
            {
                MemoryType type = _physicalDevice.PhysicalDeviceMemoryProperties.MemoryTypes[i];

                if ((memoryTypeBits & (1 << i)) != 0)
                {
                    if (type.PropertyFlags.HasFlag(flags))
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        public static bool IsDeviceMemoryShared(VulkanPhysicalDevice physicalDevice)
        {
            for (int i = 0; i < physicalDevice.PhysicalDeviceMemoryProperties.MemoryHeapCount; i++)
            {
                if (!physicalDevice.PhysicalDeviceMemoryProperties.MemoryHeaps[i].Flags.HasFlag(MemoryHeapFlags.DeviceLocalBit))
                {
                    return false;
                }
            }

            return true;
        }

        public void Dispose()
        {
            for (int i = 0; i < _blockLists.Count; i++)
            {
                _blockLists[i].Dispose();
            }
        }
    }
}
