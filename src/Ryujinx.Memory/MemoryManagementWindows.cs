using Ryujinx.Memory.WindowsShared;
using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Ryujinx.Memory
{
    [SupportedOSPlatform("windows")]
    static class MemoryManagementWindows
    {
        public const int PageSize = 0x1000;

        private static readonly PlaceholderManager _placeholders = new();

        public static nint Allocate(nint size)
        {
            return AllocateInternal(size, AllocationType.Reserve | AllocationType.Commit);
        }

        public static nint Reserve(nint size, bool viewCompatible)
        {
            if (viewCompatible)
            {
                nint baseAddress = AllocateInternal2(size, AllocationType.Reserve | AllocationType.ReservePlaceholder);

                _placeholders.ReserveRange((ulong)baseAddress, (ulong)size);

                return baseAddress;
            }

            return AllocateInternal(size, AllocationType.Reserve);
        }

        private static nint AllocateInternal(nint size, AllocationType flags = 0)
        {
            nint ptr = WindowsApi.VirtualAlloc(nint.Zero, size, flags, MemoryProtection.ReadWrite);

            if (ptr == nint.Zero)
            {
                throw new SystemException(Marshal.GetLastPInvokeErrorMessage());
            }

            return ptr;
        }

        private static nint AllocateInternal2(nint size, AllocationType flags = 0)
        {
            nint ptr = WindowsApi.VirtualAlloc2(WindowsApi.CurrentProcessHandle, nint.Zero, size, flags, MemoryProtection.NoAccess, nint.Zero, 0);

            if (ptr == nint.Zero)
            {
                throw new SystemException(Marshal.GetLastPInvokeErrorMessage());
            }

            return ptr;
        }

        public static void Commit(nint location, nint size)
        {
            if (WindowsApi.VirtualAlloc(location, size, AllocationType.Commit, MemoryProtection.ReadWrite) == nint.Zero)
            {
                throw new SystemException(Marshal.GetLastPInvokeErrorMessage());
            }
        }

        public static void Decommit(nint location, nint size)
        {
            if (!WindowsApi.VirtualFree(location, size, AllocationType.Decommit))
            {
                throw new SystemException(Marshal.GetLastPInvokeErrorMessage());
            }
        }

        public static void MapView(nint sharedMemory, ulong srcOffset, nint location, nint size, MemoryBlock owner)
        {
            _placeholders.MapView(sharedMemory, srcOffset, location, size, owner);
        }

        public static void UnmapView(nint sharedMemory, nint location, nint size, MemoryBlock owner)
        {
            _placeholders.UnmapView(sharedMemory, location, size, owner);
        }

        public static bool Reprotect(nint address, nint size, MemoryPermission permission, bool forView)
        {
            if (forView)
            {
                return _placeholders.ReprotectView(address, size, permission);
            }
            else
            {
                return WindowsApi.VirtualProtect(address, size, WindowsApi.GetProtection(permission), out _);
            }
        }

        public static bool Free(nint address, nint size)
        {
            _placeholders.UnreserveRange((ulong)address, (ulong)size);

            return WindowsApi.VirtualFree(address, nint.Zero, AllocationType.Release);
        }

        public static nint CreateSharedMemory(nint size, bool reserve)
        {
            FileMapProtection prot = reserve ? FileMapProtection.SectionReserve : FileMapProtection.SectionCommit;

            nint handle = WindowsApi.CreateFileMapping(
                WindowsApi.InvalidHandleValue,
                nint.Zero,
                FileMapProtection.PageReadWrite | prot,
                (uint)(size.ToInt64() >> 32),
                (uint)size.ToInt64(),
                null);

            if (handle == nint.Zero)
            {
                throw new SystemException(Marshal.GetLastPInvokeErrorMessage());
            }

            return handle;
        }

        public static void DestroySharedMemory(nint handle)
        {
            if (!WindowsApi.CloseHandle(handle))
            {
                throw new ArgumentException("Invalid handle.", nameof(handle));
            }
        }

        public static nint MapSharedMemory(nint handle)
        {
            nint ptr = WindowsApi.MapViewOfFile(handle, 4 | 2, 0, 0, nint.Zero);

            if (ptr == nint.Zero)
            {
                throw new SystemException(Marshal.GetLastPInvokeErrorMessage());
            }

            return ptr;
        }

        public static void UnmapSharedMemory(nint address)
        {
            if (!WindowsApi.UnmapViewOfFile(address))
            {
                throw new ArgumentException("Invalid address.", nameof(address));
            }
        }
    }
}
