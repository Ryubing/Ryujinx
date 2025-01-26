using Ryujinx.Common.Logging;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace Ryujinx.Ava.Utilities.SystemInfo
{
    [SupportedOSPlatform("macos")]
    partial class MacOSSystemInfo : SystemInfo
    {
        internal MacOSSystemInfo()
        {
            if (SysctlByName("kern.osversion", out string buildRevision) != 0)
            {
                buildRevision = "Unknown Build";
            }

            OsDescription = $"macOS {Environment.OSVersion.Version} ({buildRevision}) ({RuntimeInformation.OSArchitecture})";

            string cpuName = GetCpuidCpuName();

            if (cpuName == null && SysctlByName("machdep.cpu.brand_string", out cpuName) != 0)
            {
                cpuName = "Unknown";
            }

            ulong totalRAM = 0;

            if (SysctlByName("hw.memsize", ref totalRAM) != 0) // Bytes
            {
                totalRAM = 0;
            }

            CpuName = $"{cpuName} ; {LogicalCoreCount} logical";
            RamTotal = totalRAM;
            RamAvailable = GetVMInfoAvailableMemory();
        }

        static ulong GetVMInfoAvailableMemory()
        {
            uint port = mach_host_self();

            uint pageSize = 0;
            int result = host_page_size(port, ref pageSize);

            if (result != 0)
            {
                Logger.Error?.Print(LogClass.Application, $"Failed to query Available RAM. host_page_size() error = {result}");
                return 0;
            }

            const int Flavor = 4; // HOST_VM_INFO64
            uint count = (uint)(Marshal.SizeOf<VMStatistics64>() / sizeof(int)); // HOST_VM_INFO64_COUNT
            VMStatistics64 stats = new();
            result = host_statistics64(port, Flavor, ref stats, ref count);

            if (result != 0)
            {
                Logger.Error?.Print(LogClass.Application, $"Failed to query Available RAM. host_statistics64() error = {result}");
                return 0;
            }

            return (ulong)(stats.FreeCount + stats.InactiveCount) * pageSize;
        }

        private const string SystemLibraryName = "libSystem.dylib";

        [LibraryImport(SystemLibraryName, SetLastError = true)]
        private static partial int sysctlbyname([MarshalAs(UnmanagedType.LPStr)] string name, nint oldValue, ref ulong oldSize, nint newValue, ulong newValueSize);

        private static int SysctlByName(string name, nint oldValue, ref ulong oldSize)
        {
            if (sysctlbyname(name, oldValue, ref oldSize, nint.Zero, 0) == -1)
            {
                int err = Marshal.GetLastWin32Error();

                Logger.Error?.Print(LogClass.Application, $"Cannot retrieve '{name}'. Error Code {err}");

                return err;
            }

            return 0;
        }

        private static int SysctlByName<T>(string name, ref T oldValue)
        {
            unsafe
            {
                ulong oldValueSize = (ulong)Unsafe.SizeOf<T>();

                return SysctlByName(name, (nint)Unsafe.AsPointer(ref oldValue), ref oldValueSize);
            }
        }

        private static int SysctlByName(string name, out string oldValue)
        {
            oldValue = default;

            ulong strSize = 0;

            int res = SysctlByName(name, nint.Zero, ref strSize);

            if (res == 0)
            {
                byte[] rawData = new byte[strSize];

                unsafe
                {
                    fixed (byte* rawDataPtr = rawData)
                    {
                        res = SysctlByName(name, (nint)rawDataPtr, ref strSize);
                    }

                    if (res == 0)
                    {
                        oldValue = Encoding.ASCII.GetString(rawData);
                    }
                }
            }

            return res;
        }

        [LibraryImport(SystemLibraryName, SetLastError = true)]
        private static partial uint mach_host_self();

        [LibraryImport(SystemLibraryName, SetLastError = true)]
        private static partial int host_page_size(uint host, ref uint out_page_size);

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        struct VMStatistics64
        {
            public uint FreeCount;
            public uint ActiveCount;
            public uint InactiveCount;
            public uint WireCount;
            public ulong ZeroFillCount;
            public ulong Reactivations;
            public ulong Pageins;
            public ulong Pageouts;
            public ulong Faults;
            public ulong CowFaults;
            public ulong Lookups;
            public ulong Hits;
            public ulong Purges;
            public uint PurgeableCount;
            public uint SpeculativeCount;
            public ulong Decompressions;
            public ulong Compressions;
            public ulong Swapins;
            public ulong Swapouts;
            public uint CompressorPageCount;
            public uint ThrottledCount;
            public uint ExternalPageCount;
            public uint InternalPageCount;
            public ulong TotalUncompressedPagesInCompressor;
        }

        [LibraryImport(SystemLibraryName, SetLastError = true)]
        private static partial int host_statistics64(uint hostPriv, int hostFlavor, ref VMStatistics64 hostInfo64Out, ref uint hostInfo64OutCnt);
    }
}
