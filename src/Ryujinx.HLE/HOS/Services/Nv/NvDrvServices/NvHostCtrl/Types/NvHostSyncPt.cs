using Ryujinx.Common.Logging;
using Ryujinx.Graphics.Gpu.Synchronization;
using System;
using System.Threading;

namespace Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostCtrl
{
    class NvHostSyncpt
    {
        public const int VBlank0SyncpointId = 26;
        public const int VBlank1SyncpointId = 27;

        private readonly int[] _counterMin;
        private readonly int[] _counterMax;
        private readonly bool[] _clientManaged;
        private readonly bool[] _assigned;

        private readonly Switch _device;

        private readonly Lock _syncpointAllocatorLock = new();

        public NvHostSyncpt(Switch device)
        {
            _device = device;
            _counterMin = new int[SynchronizationManager.MaxHardwareSyncpoints];
            _counterMax = new int[SynchronizationManager.MaxHardwareSyncpoints];
            _clientManaged = new bool[SynchronizationManager.MaxHardwareSyncpoints];
            _assigned = new bool[SynchronizationManager.MaxHardwareSyncpoints];

            // Reserve VBLANK syncpoints
            ReserveSyncpointLocked(VBlank0SyncpointId, true);
            ReserveSyncpointLocked(VBlank1SyncpointId, true);
        }

        private void ReserveSyncpointLocked(uint id, bool isClientManaged)
        {
            if (id >= SynchronizationManager.MaxHardwareSyncpoints || _assigned[id])
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            _assigned[id] = true;
            _clientManaged[id] = isClientManaged;
        }

        public uint AllocateSyncpoint(bool isClientManaged)
        {
            lock (_syncpointAllocatorLock)
            {
                for (uint i = 1; i < SynchronizationManager.MaxHardwareSyncpoints; i++)
                {
                    if (!_assigned[i])
                    {
                        ReserveSyncpointLocked(i, isClientManaged);
                        return i;
                    }
                }
            }

            Logger.Error?.Print(LogClass.ServiceNv, "Cannot allocate a new syncpoint!");

            return 0;
        }

        public void ReleaseSyncpoint(uint id)
        {
            if (id == 0)
            {
                return;
            }

            lock (_syncpointAllocatorLock)
            {
                if (id >= SynchronizationManager.MaxHardwareSyncpoints || !_assigned[id])
                {
                    throw new ArgumentOutOfRangeException(nameof(id));
                }

                _assigned[id] = false;
                _clientManaged[id] = false;

                SetSyncpointMinEqualSyncpointMax(id);
            }
        }

        public void SetSyncpointMinEqualSyncpointMax(uint id)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(id, (uint)SynchronizationManager.MaxHardwareSyncpoints);

            int value = (int)ReadSyncpointValue(id);

            Interlocked.Exchange(ref _counterMax[id], value);
        }

        public uint ReadSyncpointValue(uint id)
        {
            return UpdateMin(id);
        }

        public uint ReadSyncpointMinValue(uint id)
        {
            return (uint)_counterMin[id];
        }

        public uint ReadSyncpointMaxValue(uint id)
        {
            return (uint)_counterMax[id];
        }

        private bool IsClientManaged(uint id)
        {
            if (id >= SynchronizationManager.MaxHardwareSyncpoints)
            {
                return false;
            }

            return _clientManaged[id];
        }

        public void Increment(uint id)
        {
            if (IsClientManaged(id))
            {
                IncrementSyncpointMax(id);
            }

            IncrementSyncpointGPU(id);
        }

        public uint UpdateMin(uint id)
        {
            uint newValue = _device.Gpu.Synchronization.GetSyncpointValue(id);

            Interlocked.Exchange(ref _counterMin[id], (int)newValue);

            return newValue;
        }

        private void IncrementSyncpointGPU(uint id)
        {
            _device.Gpu.Synchronization.IncrementSyncpoint(id);
        }

        public void IncrementSyncpointMin(uint id)
        {
            Interlocked.Increment(ref _counterMin[id]);
        }

        public uint IncrementSyncpointMaxExt(uint id, int count)
        {
            if (count == 0)
            {
                return ReadSyncpointMaxValue(id);
            }

            uint result = 0;

            for (int i = 0; i < count; i++)
            {
                result = IncrementSyncpointMax(id);
            }

            return result;
        }

        private uint IncrementSyncpointMax(uint id)
        {
            return (uint)Interlocked.Increment(ref _counterMax[id]);
        }

        public uint IncrementSyncpointMax(uint id, uint incrs)
        {
            return (uint)Interlocked.Add(ref _counterMax[id], (int)incrs);
        }

        public bool IsSyncpointExpired(uint id, uint threshold)
        {
            return MinCompare(id, _counterMin[id], _counterMax[id], (int)threshold);
        }

        private bool MinCompare(uint id, int min, int max, int threshold)
        {
            int minDiff = min - threshold;
            int maxDiff = max - threshold;

            if (IsClientManaged(id))
            {
                return minDiff >= 0;
            }
            else
            {
                return (uint)maxDiff >= (uint)minDiff;
            }
        }
    }
}
