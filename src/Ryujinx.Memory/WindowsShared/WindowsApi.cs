using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Ryujinx.Memory.WindowsShared
{
    [SupportedOSPlatform("windows")]
    static partial class WindowsApi
    {
        public static readonly nint InvalidHandleValue = new(-1);
        public static readonly nint CurrentProcessHandle = new(-1);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        public static partial nint VirtualAlloc(
            nint lpAddress,
            nint dwSize,
            AllocationType flAllocationType,
            MemoryProtection flProtect);

        [LibraryImport("KernelBase.dll", SetLastError = true)]
        public static partial nint VirtualAlloc2(
            nint process,
            nint lpAddress,
            nint dwSize,
            AllocationType flAllocationType,
            MemoryProtection flProtect,
            nint extendedParameters,
            ulong parameterCount);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool VirtualProtect(
            nint lpAddress,
            nint dwSize,
            MemoryProtection flNewProtect,
            out MemoryProtection lpflOldProtect);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool VirtualFree(nint lpAddress, nint dwSize, AllocationType dwFreeType);

        [LibraryImport("kernel32.dll", SetLastError = true, EntryPoint = "CreateFileMappingW")]
        public static partial nint CreateFileMapping(
            nint hFile,
            nint lpFileMappingAttributes,
            FileMapProtection flProtect,
            uint dwMaximumSizeHigh,
            uint dwMaximumSizeLow,
            [MarshalAs(UnmanagedType.LPWStr)] string lpName);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool CloseHandle(nint hObject);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        public static partial nint MapViewOfFile(
            nint hFileMappingObject,
            uint dwDesiredAccess,
            uint dwFileOffsetHigh,
            uint dwFileOffsetLow,
            nint dwNumberOfBytesToMap);

        [LibraryImport("KernelBase.dll", SetLastError = true)]
        public static partial nint MapViewOfFile3(
            nint hFileMappingObject,
            nint process,
            nint baseAddress,
            ulong offset,
            nint dwNumberOfBytesToMap,
            ulong allocationType,
            MemoryProtection dwDesiredAccess,
            nint extendedParameters,
            ulong parameterCount);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool UnmapViewOfFile(nint lpBaseAddress);

        [LibraryImport("KernelBase.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool UnmapViewOfFile2(nint process, nint lpBaseAddress, ulong unmapFlags);

        [LibraryImport("kernel32.dll")]
        public static partial uint GetLastError();

        [LibraryImport("kernel32.dll")]
        public static partial int GetCurrentThreadId();

        public static MemoryProtection GetProtection(MemoryPermission permission)
        {
            return permission switch
            {
                MemoryPermission.None => MemoryProtection.NoAccess,
                MemoryPermission.Read => MemoryProtection.ReadOnly,
                MemoryPermission.ReadAndWrite => MemoryProtection.ReadWrite,
                MemoryPermission.ReadAndExecute => MemoryProtection.ExecuteRead,
                MemoryPermission.ReadWriteExecute => MemoryProtection.ExecuteReadWrite,
                MemoryPermission.Execute => MemoryProtection.Execute,
                _ => throw new MemoryProtectionException(permission),
            };
        }
    }
}
