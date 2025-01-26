using Ryujinx.Common.Logging;
using System;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Ryujinx.Ava.Utilities.SystemInfo
{
    [SupportedOSPlatform("windows")]
    partial class WindowsSystemInfo : SystemInfo
    {
        internal WindowsSystemInfo()
        {
            CpuName = $"{GetCpuidCpuName() ?? GetCpuNameWMI()} ; {LogicalCoreCount} logical"; // WMI is very slow
            (RamTotal, RamAvailable) = GetMemoryStats();
        }

        private static (ulong Total, ulong Available) GetMemoryStats()
        {
            MemoryStatusEx memStatus = new();
            if (GlobalMemoryStatusEx(ref memStatus))
            {
                return (memStatus.TotalPhys, memStatus.AvailPhys); // Bytes
            }

            Logger.Error?.Print(LogClass.Application, $"GlobalMemoryStatusEx failed. Error {Marshal.GetLastWin32Error():X}");

            return (0, 0);
        }

        private static string GetCpuNameWMI()
        {
            ManagementObjectCollection cpuObjs = GetWMIObjects("root\\CIMV2", "SELECT * FROM Win32_Processor");

            if (cpuObjs != null)
            {
                foreach (ManagementBaseObject cpuObj in cpuObjs)
                {
                    return cpuObj["Name"].ToString().Trim();
                }
            }

            return Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER")?.Trim();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MemoryStatusEx
        {
            public uint Length;
            public uint MemoryLoad;
            public ulong TotalPhys;
            public ulong AvailPhys;
            public ulong TotalPageFile;
            public ulong AvailPageFile;
            public ulong TotalVirtual;
            public ulong AvailVirtual;
            public ulong AvailExtendedVirtual;

            public MemoryStatusEx()
            {
                Length = (uint)Marshal.SizeOf<MemoryStatusEx>();
            }
        }

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GlobalMemoryStatusEx(ref MemoryStatusEx lpBuffer);

        private static ManagementObjectCollection GetWMIObjects(string scope, string query)
        {
            try
            {
                return new ManagementObjectSearcher(scope, query).Get();
            }
            catch (PlatformNotSupportedException ex)
            {
                Logger.Error?.Print(LogClass.Application, $"WMI isn't available : {ex.Message}");
            }
            catch (COMException ex)
            {
                Logger.Error?.Print(LogClass.Application, $"WMI isn't available : {ex.Message}");
            }

            return null;
        }
    }
}
