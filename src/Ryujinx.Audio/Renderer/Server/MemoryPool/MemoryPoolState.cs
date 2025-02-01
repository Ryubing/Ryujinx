using System.Runtime.InteropServices;
using CpuAddress = System.UInt64;
using DspAddress = System.UInt64;

namespace Ryujinx.Audio.Renderer.Server.MemoryPool
{
    /// <summary>
    /// Server state for a memory pool.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 0x20, Pack = Alignment)]
    public struct MemoryPoolState
    {
        public const int Alignment = 0x10;

        /// <summary>
        /// The location of the <see cref="MemoryPoolState"/>.
        /// </summary>
        public enum LocationType : uint
        {
            /// <summary>
            /// <see cref="MemoryPoolState"/> located on the CPU side for user use.
            /// </summary>
            Cpu,

            /// <summary>
            /// <see cref="MemoryPoolState"/> located on the DSP side for system use.
            /// </summary>
            Dsp,
        }

        /// <summary>
        /// The CPU address associated to the <see cref="MemoryPoolState"/>.
        /// </summary>
        public CpuAddress CpuAddress;

        /// <summary>
        /// The DSP address associated to the <see cref="MemoryPoolState"/>.
        /// </summary>
        public DspAddress DspAddress;

        /// <summary>
        /// The size associated to the <see cref="MemoryPoolState"/>.
        /// </summary>
        public ulong Size;

        /// <summary>
        /// The <see cref="LocationType"/> associated to the <see cref="MemoryPoolState"/>.
        /// </summary>
        public LocationType Location;

        /// <summary>
        /// Set to true if the <see cref="MemoryPoolState"/> is used.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool IsUsed;

        public static unsafe MemoryPoolState* Null => (MemoryPoolState*)nint.Zero.ToPointer();

        /// <summary>
        /// Create a new <see cref="MemoryPoolState"/> with the given <see cref="LocationType"/>.
        /// </summary>
        /// <param name="location">The location type to use.</param>
        /// <returns>A new <see cref="MemoryPoolState"/> with the given <see cref="LocationType"/>.</returns>
        public static MemoryPoolState Create(LocationType location)
        {
            return new MemoryPoolState
            {
                CpuAddress = 0,
                DspAddress = 0,
                Size = 0,
                Location = location,
            };
        }

        /// <summary>
        /// Set the <see cref="CpuAddress"/> and size of the <see cref="MemoryPoolState"/>.
        /// </summary>
        /// <param name="cpuAddress">The <see cref="CpuAddress"/>.</param>
        /// <param name="size">The size.</param>
        public void SetCpuAddress(CpuAddress cpuAddress, ulong size)
        {
            CpuAddress = cpuAddress;
            Size = size;
        }

        /// <summary>
        /// Check if the given <see cref="CpuAddress"/> and size is contains in the <see cref="MemoryPoolState"/>.
        /// </summary>
        /// <param name="targetCpuAddress">The <see cref="CpuAddress"/>.</param>
        /// <param name="size">The size.</param>
        /// <returns>True if the <see cref="CpuAddress"/> is contained inside the <see cref="MemoryPoolState"/>.</returns>
        public readonly bool Contains(CpuAddress targetCpuAddress, ulong size)
        {
            if (CpuAddress <= targetCpuAddress && size + targetCpuAddress <= Size + CpuAddress)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Translate the given CPU address to a DSP address.
        /// </summary>
        /// <param name="targetCpuAddress">The <see cref="CpuAddress"/>.</param>
        /// <param name="size">The size.</param>
        /// <returns>the target DSP address.</returns>
        public readonly DspAddress Translate(CpuAddress targetCpuAddress, ulong size)
        {
            if (Contains(targetCpuAddress, size) && IsMapped())
            {
                ulong offset = targetCpuAddress - CpuAddress;

                return DspAddress + offset;
            }

            return 0;
        }

        /// <summary>
        /// Is the <see cref="MemoryPoolState"/> mapped on the DSP?
        /// </summary>
        /// <returns>Returns true if the <see cref="MemoryPoolState"/> is mapped on the DSP.</returns>
        public readonly bool IsMapped()
        {
            return DspAddress != 0;
        }
    }
}
