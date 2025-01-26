using System;
using System.Runtime.CompilerServices;

namespace Ryujinx.Memory.Tracking
{
    /// <summary>
    /// A MultiRegionHandle that attempts to segment a region's handles into the regions requested
    /// to avoid iterating over granular chunks for canonically large regions.
    /// If minimum granularity is to be expected, use MultiRegionHandle.
    /// </summary>
    public class SmartMultiRegionHandle : IMultiRegionHandle
    {
        /// <summary>
        /// A list of region handles starting at each granularity size increment.
        /// </summary>
        private readonly RegionHandle[] _handles;
        private readonly ulong _address;
        private readonly ulong _granularity;
        private readonly ulong _size;
        private readonly MemoryTracking _tracking;
        private readonly int _id;

        public bool Dirty { get; private set; } = true;

        internal SmartMultiRegionHandle(MemoryTracking tracking, ulong address, ulong size, ulong granularity, int id)
        {
            // For this multi-region handle, the handle list starts empty.
            // As regions are queried, they are added to the _handles array at their start index.
            // When a region being added overlaps another, the existing region is split.
            // A query can therefore scan multiple regions, though with no overlaps they can cover a large area.

            _tracking = tracking;
            _handles = new RegionHandle[size / granularity];
            _granularity = granularity;

            _address = address;
            _size = size;
            _id = id;
        }

        public void SignalWrite()
        {
            Dirty = true;
        }

        public void ForceDirty(ulong address, ulong size)
        {
            foreach (RegionHandle handle in _handles)
            {
                if (handle != null && handle.OverlapsWith(address, size))
                {
                    handle.ForceDirty();
                }
            }
        }

        public void RegisterAction(RegionSignal action)
        {
            foreach (RegionHandle handle in _handles)
            {
                if (handle != null)
                {
                    handle?.RegisterAction((address, size) => action(handle.Address, handle.Size));
                }
            }
        }

        public void RegisterPreciseAction(PreciseRegionSignal action)
        {
            foreach (RegionHandle handle in _handles)
            {
                if (handle != null)
                {
                    handle?.RegisterPreciseAction((address, size, write) => action(handle.Address, handle.Size, write));
                }
            }
        }

        public void QueryModified(Action<ulong, ulong> modifiedAction)
        {
            if (!Dirty)
            {
                return;
            }

            Dirty = false;

            QueryModified(_address, _size, modifiedAction);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ulong HandlesToBytes(int handles)
        {
            return (ulong)handles * _granularity;
        }

        private void SplitHandle(int handleIndex, int splitIndex)
        {
            RegionHandle handle = _handles[handleIndex];
            ulong address = _address + HandlesToBytes(handleIndex);
            ulong size = HandlesToBytes(splitIndex - handleIndex);

            // First, the target handle must be removed. Its data can still be used to determine the new handles.
            RegionSignal signal = handle.PreAction;
            handle.Dispose();

            RegionHandle splitLow = _tracking.BeginTracking(address, size, _id);
            splitLow.Parent = this;
            if (signal != null)
            {
                splitLow.RegisterAction(signal);
            }
            _handles[handleIndex] = splitLow;

            RegionHandle splitHigh = _tracking.BeginTracking(address + size, handle.Size - size, _id);
            splitHigh.Parent = this;
            if (signal != null)
            {
                splitHigh.RegisterAction(signal);
            }
            _handles[splitIndex] = splitHigh;
        }

        private void CreateHandle(int startHandle, int lastHandle)
        {
            ulong startAddress = _address + HandlesToBytes(startHandle);

            // Scan for the first handle before us. If it's overlapping us, it must be split.
            for (int i = startHandle - 1; i >= 0; i--)
            {
                RegionHandle handle = _handles[i];
                if (handle != null)
                {
                    if (handle.EndAddress > startAddress)
                    {
                        SplitHandle(i, startHandle);
                        return; // The remainer of this handle should be filled in later on.
                    }
                    break;
                }
            }

            // Scan for handles after us. We should create a handle that goes up to this handle's start point, if present.
            for (int i = startHandle + 1; i <= lastHandle; i++)
            {
                RegionHandle handle = _handles[i];
                if (handle != null)
                {
                    // Fill up to the found handle.
                    handle = _tracking.BeginTracking(startAddress, HandlesToBytes(i - startHandle), _id);
                    handle.Parent = this;
                    _handles[startHandle] = handle;
                    return;
                }
            }

            // Can fill the whole range.
            _handles[startHandle] = _tracking.BeginTracking(startAddress, HandlesToBytes(1 + lastHandle - startHandle), _id);
            _handles[startHandle].Parent = this;
        }

        public void QueryModified(ulong address, ulong size, Action<ulong, ulong> modifiedAction)
        {
            int startHandle = (int)((address - _address) / _granularity);
            int lastHandle = (int)((address + (size - 1) - _address) / _granularity);

            ulong rgStart = _address + (ulong)startHandle * _granularity;
            ulong rgSize = 0;

            ulong endAddress = _address + ((ulong)lastHandle + 1) * _granularity;

            int i = startHandle;

            while (i <= lastHandle)
            {
                RegionHandle handle = _handles[i];
                if (handle == null)
                {
                    // Missing handle. A new handle must be created.
                    CreateHandle(i, lastHandle);
                    handle = _handles[i];
                }

                if (handle.EndAddress > endAddress)
                {
                    // End address of handle is beyond the end of the search. Force a split.
                    SplitHandle(i, lastHandle + 1);
                    handle = _handles[i];
                }

                if (handle.Dirty)
                {
                    rgSize += handle.Size;
                    handle.Reprotect();
                }
                else
                {
                    // Submit the region scanned so far as dirty
                    if (rgSize != 0)
                    {
                        modifiedAction(rgStart, rgSize);
                        rgSize = 0;
                    }
                    rgStart = handle.EndAddress;
                }

                i += (int)(handle.Size / _granularity);
            }

            if (rgSize != 0)
            {
                modifiedAction(rgStart, rgSize);
            }
        }

        public void QueryModified(ulong address, ulong size, Action<ulong, ulong> modifiedAction, int sequenceNumber)
        {
            int startHandle = (int)((address - _address) / _granularity);
            int lastHandle = (int)((address + (size - 1) - _address) / _granularity);

            ulong rgStart = _address + (ulong)startHandle * _granularity;
            ulong rgSize = 0;

            ulong endAddress = _address + ((ulong)lastHandle + 1) * _granularity;

            int i = startHandle;

            while (i <= lastHandle)
            {
                RegionHandle handle = _handles[i];
                if (handle == null)
                {
                    // Missing handle. A new handle must be created.
                    CreateHandle(i, lastHandle);
                    handle = _handles[i];
                }

                if (handle.EndAddress > endAddress)
                {
                    // End address of handle is beyond the end of the search. Force a split.
                    SplitHandle(i, lastHandle + 1);
                    handle = _handles[i];
                }

                if (handle.Dirty && sequenceNumber != handle.SequenceNumber)
                {
                    rgSize += handle.Size;
                    handle.Reprotect();
                }
                else
                {
                    // Submit the region scanned so far as dirty
                    if (rgSize != 0)
                    {
                        modifiedAction(rgStart, rgSize);
                        rgSize = 0;
                    }
                    rgStart = handle.EndAddress;
                }

                handle.SequenceNumber = sequenceNumber;

                i += (int)(handle.Size / _granularity);
            }

            if (rgSize != 0)
            {
                modifiedAction(rgStart, rgSize);
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            foreach (RegionHandle handle in _handles)
            {
                handle?.Dispose();
            }
        }
    }
}
