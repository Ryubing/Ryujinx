using Ryujinx.Audio.Renderer.Common;
using Ryujinx.Audio.Renderer.Parameter;
using Ryujinx.Audio.Renderer.Utils;
using Ryujinx.Common.Logging;
using System;
using static Ryujinx.Audio.Renderer.Common.BehaviourParameter;

using CpuAddress = System.UInt64;
using DspAddress = System.UInt64;

namespace Ryujinx.Audio.Renderer.Server.MemoryPool
{
    /// <summary>
    /// Memory pool mapping helper.
    /// </summary>
    public class PoolMapper
    {
        const uint CurrentProcessPseudoHandle = 0xFFFF8001;

        /// <summary>
        /// The result of <see cref="Update(ref MemoryPoolState, ref MemoryPoolInParameter, ref MemoryPoolOutStatus)"/>.
        /// </summary>
        public enum UpdateResult : uint
        {
            /// <summary>
            /// No error reported.
            /// </summary>
            Success = 0,

            /// <summary>
            /// The user parameters were invalid.
            /// </summary>
            InvalidParameter = 1,

            /// <summary>
            /// <see cref="Dsp.AudioProcessor"/> mapping failed.
            /// </summary>
            MapError = 2,

            /// <summary>
            /// <see cref="Dsp.AudioProcessor"/> unmapping failed.
            /// </summary>
            UnmapError = 3,
        }

        /// <summary>
        /// The handle of the process owning the CPU memory manipulated.
        /// </summary>
        private readonly uint _processHandle;

        /// <summary>
        /// The <see cref="Memory{MemoryPoolState}"/> that will be manipulated.
        /// </summary>
        private readonly Memory<MemoryPoolState> _memoryPools;

        /// <summary>
        /// If set to true, this will try to force map memory pool even if their state are considered invalid.
        /// </summary>
        private readonly bool _isForceMapEnabled;

        /// <summary>
        /// Create a new <see cref="PoolMapper"/> used for system mapping.
        /// </summary>
        /// <param name="processHandle">The handle of the process owning the CPU memory manipulated.</param>
        /// <param name="isForceMapEnabled">If set to true, this will try to force map memory pool even if their state are considered invalid.</param>
        public PoolMapper(uint processHandle, bool isForceMapEnabled)
        {
            _processHandle = processHandle;
            _isForceMapEnabled = isForceMapEnabled;
            _memoryPools = Memory<MemoryPoolState>.Empty;
        }

        /// <summary>
        /// Create a new <see cref="PoolMapper"/> used for user mapping.
        /// </summary>
        /// <param name="processHandle">The handle of the process owning the CPU memory manipulated.</param>
        /// <param name="memoryPool">The user memory pools.</param>
        /// <param name="isForceMapEnabled">If set to true, this will try to force map memory pool even if their state are considered invalid.</param>
        public PoolMapper(uint processHandle, Memory<MemoryPoolState> memoryPool, bool isForceMapEnabled)
        {
            _processHandle = processHandle;
            _memoryPools = memoryPool;
            _isForceMapEnabled = isForceMapEnabled;
        }

        /// <summary>
        /// Initialize the <see cref="MemoryPoolState"/> for system use.
        /// </summary>
        /// <param name="memoryPool">The <see cref="MemoryPoolState"/> for system use.</param>
        /// <param name="cpuAddress">The <see cref="CpuAddress"/> to assign.</param>
        /// <param name="size">The size to assign.</param>
        /// <returns>Returns true if mapping on the <see cref="Dsp.AudioProcessor"/> succeeded.</returns>
        public bool InitializeSystemPool(ref MemoryPoolState memoryPool, CpuAddress cpuAddress, ulong size)
        {
            if (memoryPool.Location != MemoryPoolState.LocationType.Dsp)
            {
                return false;
            }

            return InitializePool(ref memoryPool, cpuAddress, size);
        }

        /// <summary>
        /// Initialize the <see cref="MemoryPoolState"/>.
        /// </summary>
        /// <param name="memoryPool">The <see cref="MemoryPoolState"/>.</param>
        /// <param name="cpuAddress">The <see cref="CpuAddress"/> to assign.</param>
        /// <param name="size">The size to assign.</param>
        /// <returns>Returns true if mapping on the <see cref="Dsp.AudioProcessor"/> succeeded.</returns>
        public bool InitializePool(ref MemoryPoolState memoryPool, CpuAddress cpuAddress, ulong size)
        {
            memoryPool.SetCpuAddress(cpuAddress, size);

            return Map(ref memoryPool) != 0;
        }

        /// <summary>
        /// Get the process handle associated to the <see cref="MemoryPoolState"/>.
        /// </summary>
        /// <param name="memoryPool">The <see cref="MemoryPoolState"/>.</param>
        /// <returns>Returns the process handle associated to the <see cref="MemoryPoolState"/>.</returns>
        public uint GetProcessHandle(ref MemoryPoolState memoryPool)
        {
            if (memoryPool.Location == MemoryPoolState.LocationType.Cpu)
            {
                return CurrentProcessPseudoHandle;
            }

            if (memoryPool.Location == MemoryPoolState.LocationType.Dsp)
            {
                return _processHandle;
            }

            return 0;
        }

        /// <summary>
        /// Map the <see cref="MemoryPoolState"/> on the <see cref="Dsp.AudioProcessor"/>.
        /// </summary>
        /// <param name="memoryPool">The <see cref="MemoryPoolState"/> to map.</param>
        /// <returns>Returns the DSP address mapped.</returns>
        public DspAddress Map(ref MemoryPoolState memoryPool)
        {
            DspAddress result = AudioProcessorMemoryManager.Map(GetProcessHandle(ref memoryPool), memoryPool.CpuAddress, memoryPool.Size);

            if (result != 0)
            {
                memoryPool.DspAddress = result;
            }

            return result;
        }

        /// <summary>
        /// Unmap the <see cref="MemoryPoolState"/> from the <see cref="Dsp.AudioProcessor"/>.
        /// </summary>
        /// <param name="memoryPool">The <see cref="MemoryPoolState"/> to unmap.</param>
        /// <returns>Returns true if unmapped.</returns>
        public bool Unmap(ref MemoryPoolState memoryPool)
        {
            if (memoryPool.IsUsed)
            {
                return false;
            }

            AudioProcessorMemoryManager.Unmap(GetProcessHandle(ref memoryPool), memoryPool.CpuAddress, memoryPool.Size);

            memoryPool.SetCpuAddress(0, 0);
            memoryPool.DspAddress = 0;

            return true;
        }

        /// <summary>
        /// Find a <see cref="MemoryPoolState"/> associated to the region given.
        /// </summary>
        /// <param name="cpuAddress">The region <see cref="CpuAddress"/>.</param>
        /// <param name="size">The region size.</param>
        /// <returns>Returns the <see cref="MemoryPoolState"/> found or <see cref="Memory{MemoryPoolState}.Empty"/> if not found.</returns>
        private Span<MemoryPoolState> FindMemoryPool(CpuAddress cpuAddress, ulong size)
        {
            if (!_memoryPools.IsEmpty && _memoryPools.Length > 0)
            {
                for (int i = 0; i < _memoryPools.Length; i++)
                {
                    if (_memoryPools.Span[i].Contains(cpuAddress, size))
                    {
                        return _memoryPools.Span.Slice(i, 1);
                    }
                }
            }

            return [];
        }

        /// <summary>
        /// Force unmap the given <see cref="AddressInfo"/>.
        /// </summary>
        /// <param name="addressInfo">The <see cref="AddressInfo"/> to force unmap</param>
        public void ForceUnmap(ref AddressInfo addressInfo)
        {
            if (_isForceMapEnabled)
            {
                Span<MemoryPoolState> memoryPool = FindMemoryPool(addressInfo.CpuAddress, addressInfo.Size);

                if (!memoryPool.IsEmpty)
                {
                    AudioProcessorMemoryManager.Unmap(_processHandle, memoryPool[0].CpuAddress, memoryPool[0].Size);

                    return;
                }

                AudioProcessorMemoryManager.Unmap(_processHandle, addressInfo.CpuAddress, 0);
            }
        }

        /// <summary>
        /// Try to attach the given region to the <see cref="AddressInfo"/>.
        /// </summary>
        /// <param name="errorInfo">The error information if an error was generated.</param>
        /// <param name="addressInfo">The <see cref="AddressInfo"/> to attach the region to.</param>
        /// <param name="cpuAddress">The region <see cref="CpuAddress"/>.</param>
        /// <param name="size">The region size.</param>
        /// <returns>Returns true if mapping was performed.</returns>
        public bool TryAttachBuffer(out ErrorInfo errorInfo, ref AddressInfo addressInfo, CpuAddress cpuAddress, ulong size)
        {
            errorInfo = new ErrorInfo();

            addressInfo.Setup(cpuAddress, size);

            if (AssignDspAddress(ref addressInfo))
            {
                errorInfo.ErrorCode = 0x0;
                errorInfo.ExtraErrorInfo = 0x0;

                return true;
            }

            errorInfo.ErrorCode = ResultCode.InvalidAddressInfo;
            errorInfo.ExtraErrorInfo = addressInfo.CpuAddress;

            return _isForceMapEnabled;
        }

        /// <summary>
        /// Update a <see cref="MemoryPoolState"/> using user parameters.
        /// </summary>
        /// <param name="memoryPool">The <see cref="MemoryPoolState"/> to update.</param>
        /// <param name="inParameter">Input user parameter.</param>
        /// <param name="outStatus">Output user parameter.</param>
        /// <returns>Returns the <see cref="UpdateResult"/> of the operations performed.</returns>
        public UpdateResult Update(ref MemoryPoolState memoryPool, in MemoryPoolInParameter inParameter, ref MemoryPoolOutStatus outStatus)
        {
            MemoryPoolUserState inputState = inParameter.State;

            MemoryPoolUserState outputState;

            const uint PageSize = 0x1000;

            if (inputState != MemoryPoolUserState.RequestAttach && inputState != MemoryPoolUserState.RequestDetach)
            {
                return UpdateResult.Success;
            }

            if (inParameter.CpuAddress == 0 || (inParameter.CpuAddress % PageSize) != 0)
            {
                return UpdateResult.InvalidParameter;
            }

            if (inParameter.Size == 0 || (inParameter.Size % PageSize) != 0)
            {
                return UpdateResult.InvalidParameter;
            }

            if (inputState == MemoryPoolUserState.RequestAttach)
            {
                bool initializeSuccess = InitializePool(ref memoryPool, inParameter.CpuAddress, inParameter.Size);

                if (!initializeSuccess)
                {
                    memoryPool.SetCpuAddress(0, 0);

                    Logger.Error?.Print(LogClass.AudioRenderer, $"Map of memory pool (address: 0x{inParameter.CpuAddress:x}, size 0x{inParameter.Size:x}) failed!");
                    return UpdateResult.MapError;
                }

                outputState = MemoryPoolUserState.Attached;
            }
            else
            {
                if (memoryPool.CpuAddress != inParameter.CpuAddress || memoryPool.Size != inParameter.Size)
                {
                    return UpdateResult.InvalidParameter;
                }

                if (!Unmap(ref memoryPool))
                {
                    Logger.Error?.Print(LogClass.AudioRenderer, $"Unmap of memory pool (address: 0x{memoryPool.CpuAddress:x}, size 0x{memoryPool.Size:x}) failed!");
                    return UpdateResult.UnmapError;
                }

                outputState = MemoryPoolUserState.Detached;
            }

            outStatus.State = outputState;

            return UpdateResult.Success;
        }

        /// <summary>
        /// Map the <see cref="AddressInfo"/> to the <see cref="Dsp.AudioProcessor"/>.
        /// </summary>
        /// <param name="addressInfo">The <see cref="AddressInfo"/> to map.</param>
        /// <returns>Returns true if mapping was performed.</returns>
        private bool AssignDspAddress(ref AddressInfo addressInfo)
        {
            if (addressInfo.CpuAddress == 0)
            {
                return false;
            }

            if (_memoryPools.Length > 0)
            {
                Span<MemoryPoolState> memoryPool = FindMemoryPool(addressInfo.CpuAddress, addressInfo.Size);

                if (!memoryPool.IsEmpty)
                {
                    addressInfo.SetupMemoryPool(memoryPool);

                    return true;
                }
            }

            if (_isForceMapEnabled)
            {
                DspAddress dspAddress = AudioProcessorMemoryManager.Map(_processHandle, addressInfo.CpuAddress, addressInfo.Size);

                addressInfo.ForceMappedDspAddress = dspAddress;

                AudioProcessorMemoryManager.Map(_processHandle, addressInfo.CpuAddress, addressInfo.Size);
            }
            else
            {
                unsafe
                {
                    addressInfo.SetupMemoryPool(MemoryPoolState.Null);
                }
            }

            return false;
        }

        /// <summary>
        /// Remove the usage flag from all the <see cref="MemoryPoolState"/>.
        /// </summary>
        /// <param name="memoryPool">The <see cref="Memory{MemoryPoolState}"/> to reset.</param>
        public static void ClearUsageState(Memory<MemoryPoolState> memoryPool)
        {
            foreach (ref MemoryPoolState info in memoryPool.Span)
            {
                info.IsUsed = false;
            }
        }
    }
}
