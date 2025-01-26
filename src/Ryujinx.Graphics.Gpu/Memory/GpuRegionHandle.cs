using Ryujinx.Memory.Tracking;
using System;

namespace Ryujinx.Graphics.Gpu.Memory
{
    /// <summary>
    /// A tracking handle for a region of GPU VA, represented by one or more tracking handles in CPU VA.
    /// </summary>
    class GpuRegionHandle : IRegionHandle
    {
        private readonly RegionHandle[] _cpuRegionHandles;

        public bool Dirty
        {
            get
            {
                foreach (RegionHandle regionHandle in _cpuRegionHandles)
                {
                    if (regionHandle.Dirty)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public ulong Address => throw new NotSupportedException();
        public ulong Size => throw new NotSupportedException();
        public ulong EndAddress => throw new NotSupportedException();

        /// <summary>
        /// Create a new GpuRegionHandle, made up of mulitple CpuRegionHandles.
        /// </summary>
        /// <param name="cpuRegionHandles">The CpuRegionHandles that make up this handle</param>
        public GpuRegionHandle(RegionHandle[] cpuRegionHandles)
        {
            _cpuRegionHandles = cpuRegionHandles;
        }

        /// <summary>
        /// Dispose the child handles.
        /// </summary>
        public void Dispose()
        {
            foreach (RegionHandle regionHandle in _cpuRegionHandles)
            {
                regionHandle.Dispose();
            }
        }

        /// <summary>
        /// Register an action to perform when the tracked region is read or written.
        /// The action is automatically removed after it runs.
        /// </summary>
        /// <param name="action">Action to call on read or write</param>
        public void RegisterAction(RegionSignal action)
        {
            foreach (RegionHandle regionHandle in _cpuRegionHandles)
            {
                regionHandle.RegisterAction(action);
            }
        }

        /// <summary>
        /// Register an action to perform when a precise access occurs (one with exact address and size).
        /// If the action returns true, read/write tracking are skipped.
        /// </summary>
        /// <param name="action">Action to call on read or write</param>
        public void RegisterPreciseAction(PreciseRegionSignal action)
        {
            foreach (RegionHandle regionHandle in _cpuRegionHandles)
            {
                regionHandle.RegisterPreciseAction(action);
            }
        }

        /// <summary>
        /// Consume the dirty flag for the handles, and reprotect so it can be set on the next write.
        /// </summary>
        public void Reprotect(bool asDirty = false)
        {
            foreach (RegionHandle regionHandle in _cpuRegionHandles)
            {
                regionHandle.Reprotect(asDirty);
            }
        }

        /// <summary>
        /// Force the handles to be dirty, without reprotecting.
        /// </summary>
        public void ForceDirty()
        {
            foreach (RegionHandle regionHandle in _cpuRegionHandles)
            {
                regionHandle.ForceDirty();
            }
        }
    }
}
