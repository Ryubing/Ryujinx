using Silk.NET.Vulkan;
using System;

namespace Ryujinx.Graphics.Rdna3Vulkan
{
    readonly struct MemoryAllocation : IDisposable
    {
        private readonly MemoryAllocatorBlockList _owner;
        private readonly MemoryAllocatorBlockList.Block _block;
        private readonly HostMemoryAllocator _hostMemory;

        public DeviceMemory Memory { get; }
        public nint HostPointer { get; }
        public ulong Offset { get; }
        public ulong Size { get; }

        public MemoryAllocation(
            MemoryAllocatorBlockList owner,
            MemoryAllocatorBlockList.Block block,
            DeviceMemory memory,
            nint hostPointer,
            ulong offset,
            ulong size)
        {
            _owner = owner;
            _block = block;
            Memory = memory;
            HostPointer = hostPointer;
            Offset = offset;
            Size = size;
        }

        public MemoryAllocation(
            HostMemoryAllocator hostMemory,
            DeviceMemory memory,
            nint hostPointer,
            ulong offset,
            ulong size)
        {
            _hostMemory = hostMemory;
            Memory = memory;
            HostPointer = hostPointer;
            Offset = offset;
            Size = size;
        }

        public void Dispose()
        {
            if (_hostMemory != null)
            {
                _hostMemory.Free(Memory, Offset, Size);
            }
            else
            {
                _owner.Free(_block, Offset, Size);
            }
        }
    }
}
