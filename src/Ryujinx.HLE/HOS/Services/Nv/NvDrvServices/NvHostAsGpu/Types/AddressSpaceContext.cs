using Ryujinx.Graphics.Gpu.Memory;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostAsGpu.Types
{
    class AddressSpaceContext
    {
        private class Range
        {
            public ulong Start { get; }
            public ulong End { get; }

            public Range(ulong address, ulong size)
            {
                Start = address;
                End = size + Start;
            }
        }

        private class MappedMemory : Range
        {
            public ulong PhysicalAddress { get; }
            public bool VaAllocated { get; }

            public MappedMemory(ulong address, ulong size, ulong physicalAddress, bool vaAllocated) : base(address, size)
            {
                PhysicalAddress = physicalAddress;
                VaAllocated = vaAllocated;
            }
        }

        public MemoryManager Gmm { get; }

        private readonly SortedList<ulong, Range> _maps;
        private readonly SortedList<ulong, Range> _reservations;

        public AddressSpaceContext(MemoryManager gmm)
        {
            Gmm = gmm;

            _maps = [];
            _reservations = [];
        }

        public bool ValidateFixedBuffer(ulong address, ulong size, ulong alignment)
        {
            ulong mapEnd = address + size;

            // Check if size is valid (0 is also not allowed).
            if (mapEnd <= address)
            {
                return false;
            }

            // Check if address is aligned.
            if ((address & (alignment - 1)) != 0)
            {
                return false;
            }

            // Check if region is reserved.
            if (BinarySearch(_reservations, address) == null)
            {
                return false;
            }

            // Check for overlap with already mapped buffers.
            Range map = BinarySearchLt(_maps, mapEnd);

            if (map != null && map.End > address)
            {
                return false;
            }

            return true;
        }

        public void AddMap(ulong gpuVa, ulong size, ulong physicalAddress, bool vaAllocated)
        {
            _maps.Add(gpuVa, new MappedMemory(gpuVa, size, physicalAddress, vaAllocated));
        }

        public bool RemoveMap(ulong gpuVa, out ulong size)
        {
            size = 0;

            if (_maps.Remove(gpuVa, out Range value))
            {
                MappedMemory map = (MappedMemory)value;

                if (map.VaAllocated)
                {
                    size = (map.End - map.Start);
                }

                return true;
            }

            return false;
        }

        public bool TryGetMapPhysicalAddress(ulong gpuVa, out ulong physicalAddress)
        {
            Range map = BinarySearch(_maps, gpuVa);

            if (map != null)
            {
                physicalAddress = ((MappedMemory)map).PhysicalAddress;
                return true;
            }

            physicalAddress = 0;
            return false;
        }

        public void AddReservation(ulong gpuVa, ulong size)
        {
            _reservations.Add(gpuVa, new Range(gpuVa, size));
        }

        public bool RemoveReservation(ulong gpuVa)
        {
            return _reservations.Remove(gpuVa);
        }

        private Range BinarySearch(SortedList<ulong, Range> list, ulong address)
        {
            int left = 0;
            int right = list.Count - 1;

            while (left <= right)
            {
                int size = right - left;

                int middle = left + (size >> 1);

                Range rg = list.Values[middle];

                if (address >= rg.Start && address < rg.End)
                {
                    return rg;
                }

                if (address < rg.Start)
                {
                    right = middle - 1;
                }
                else
                {
                    left = middle + 1;
                }
            }

            return null;
        }

        private Range BinarySearchLt(SortedList<ulong, Range> list, ulong address)
        {
            Range ltRg = null;

            int left = 0;
            int right = list.Count - 1;

            while (left <= right)
            {
                int size = right - left;

                int middle = left + (size >> 1);

                Range rg = list.Values[middle];

                if (address < rg.Start)
                {
                    right = middle - 1;
                }
                else
                {
                    left = middle + 1;

                    if (address > rg.Start)
                    {
                        ltRg = rg;
                    }
                }
            }

            return ltRg;
        }
    }
}
