using ARMeilleure.CodeGen;
using ARMeilleure.CodeGen.Unwinding;
using ARMeilleure.Memory;
using ARMeilleure.Native;
using Humanizer;
using Ryujinx.Common.Logging;
using Ryujinx.Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

namespace ARMeilleure.Translation.Cache
{
    static partial class JitCache
    {
        private static readonly int _pageSize = (int)MemoryBlock.GetPageSize();
        private static readonly int _pageMask = _pageSize - 1;

        private const int CodeAlignment = 4; // Bytes.
        private const int CacheSize = 256 * 1024 * 1024;

        private static JitCacheInvalidation _jitCacheInvalidator;

        private static List<CacheMemoryAllocator> _cacheAllocators = [];

        private static readonly List<CacheEntry> _cacheEntries = [];

        private static readonly Lock _lock = new();
        private static bool _initialized;

        private static readonly List<ReservedRegion> _jitRegions = [];
        private static int _activeRegionIndex = 0;

        [SupportedOSPlatform("windows")]
        [LibraryImport("kernel32.dll", SetLastError = true)]
        public static partial nint FlushInstructionCache(nint hProcess, nint lpAddress, nuint dwSize);

        public static void Initialize(IJitMemoryAllocator allocator)
        {
            lock (_lock)
            {
                if (_initialized)
                {
                    if (OperatingSystem.IsWindows())
                    {
                        JitUnwindWindows.RemoveFunctionTableHandler(
                            _jitRegions[0].Pointer);
                    }

                    for (int i = 0; i < _jitRegions.Count; i++)
                    {
                        _jitRegions[i].Dispose();
                    }

                    _jitRegions.Clear();
                    _cacheAllocators.Clear();
                }
                else
                {
                    _initialized = true;
                }

                _activeRegionIndex = 0;

                ReservedRegion firstRegion = new(allocator, CacheSize);
                _jitRegions.Add(firstRegion);

                CacheMemoryAllocator firstCacheAllocator = new(CacheSize);
                _cacheAllocators.Add(firstCacheAllocator);

                if (!OperatingSystem.IsWindows() && !OperatingSystem.IsMacOS())
                {
                    _jitCacheInvalidator = new JitCacheInvalidation(allocator);
                }

                if (OperatingSystem.IsWindows())
                {
                    JitUnwindWindows.InstallFunctionTableHandler(
                        firstRegion.Pointer, CacheSize, firstRegion.Pointer + Allocate(_pageSize)
                    );
                }
            }
        }

        public static nint Map(CompiledFunction func)
        {
            byte[] code = func.Code;

            lock (_lock)
            {
                Debug.Assert(_initialized);

                int funcOffset = Allocate(code.Length);
                ReservedRegion targetRegion = _jitRegions[_activeRegionIndex];
                nint funcPtr = targetRegion.Pointer + funcOffset;

                if (OperatingSystem.IsMacOS() && RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                {
                    unsafe
                    {
                        fixed (byte* codePtr = code)
                        {
                            JitSupportDarwin.Copy(funcPtr, (nint)codePtr, (ulong)code.Length);
                        }
                    }
                }
                else
                {
                    ReprotectAsWritable(targetRegion, funcOffset, code.Length);
                    Marshal.Copy(code, 0, funcPtr, code.Length);
                    ReprotectAsExecutable(targetRegion, funcOffset, code.Length);

                    if (OperatingSystem.IsWindows() && RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                    {
                        FlushInstructionCache(Process.GetCurrentProcess().Handle, funcPtr, (nuint)code.Length);
                    }
                    else
                    {
                        _jitCacheInvalidator?.Invalidate(funcPtr, (ulong)code.Length);
                    }
                }

                Add(funcOffset, code.Length, func.UnwindInfo);

                return funcPtr;
            }
        }

        public static void Unmap(nint pointer)
        {
            lock (_lock)
            {
                Debug.Assert(_initialized);

                foreach (ReservedRegion region in _jitRegions)
                {
                    if (pointer.ToInt64() < region.Pointer.ToInt64() ||
                        pointer.ToInt64() >= (region.Pointer + CacheSize).ToInt64())
                    {
                        continue;
                    }

                    int funcOffset = (int)(pointer.ToInt64() - region.Pointer.ToInt64());

                    if (TryFind(funcOffset, out CacheEntry entry, out int entryIndex) && entry.Offset == funcOffset)
                    {
                        _cacheAllocators[_activeRegionIndex].Free(funcOffset, AlignCodeSize(entry.Size));
                        _cacheEntries.RemoveAt(entryIndex);
                    }

                    return;
                }
            }
        }

        private static void ReprotectAsWritable(ReservedRegion region, int offset, int size)
        {
            int endOffs = offset + size;
            int regionStart = offset & ~_pageMask;
            int regionEnd = (endOffs + _pageMask) & ~_pageMask;

            region.Block.MapAsRwx((ulong)regionStart, (ulong)(regionEnd - regionStart));
        }

        private static void ReprotectAsExecutable(ReservedRegion region, int offset, int size)
        {
            int endOffs = offset + size;
            int regionStart = offset & ~_pageMask;
            int regionEnd = (endOffs + _pageMask) & ~_pageMask;

            region.Block.MapAsRx((ulong)regionStart, (ulong)(regionEnd - regionStart));
        }

        private static int Allocate(int codeSize)
        {
            codeSize = AlignCodeSize(codeSize);

            int allocOffset = _cacheAllocators[_activeRegionIndex].Allocate(codeSize);

            if (allocOffset >= 0)
            {
                _jitRegions[_activeRegionIndex].ExpandIfNeeded((ulong)allocOffset + (ulong)codeSize);
                return allocOffset;
            }

            int exhaustedRegion = _activeRegionIndex;
            ReservedRegion newRegion = new(_jitRegions[0].Allocator, CacheSize);
            _jitRegions.Add(newRegion);
            _activeRegionIndex = _jitRegions.Count - 1;

            Logger.Warning?.Print(LogClass.Cpu, $"JIT Cache Region {exhaustedRegion} exhausted, creating new Cache Region {_activeRegionIndex} ({((long)(_activeRegionIndex + 1) * CacheSize).Bytes()} Total Allocation).");

            _cacheAllocators.Add(new CacheMemoryAllocator(CacheSize));

            int allocOffsetNew = _cacheAllocators[_activeRegionIndex].Allocate(codeSize);
            if (allocOffsetNew < 0)
            {
                throw new OutOfMemoryException("Failed to allocate in new Cache Region!");
            }

            newRegion.ExpandIfNeeded((ulong)allocOffsetNew + (ulong)codeSize);
            return allocOffsetNew;
        }


        private static int AlignCodeSize(int codeSize)
        {
            return checked(codeSize + (CodeAlignment - 1)) & ~(CodeAlignment - 1);
        }

        private static void Add(int offset, int size, UnwindInfo unwindInfo)
        {
            CacheEntry entry = new(offset, size, unwindInfo);

            int index = _cacheEntries.BinarySearch(entry);

            if (index < 0)
            {
                index = ~index;
            }

            _cacheEntries.Insert(index, entry);
        }

        public static bool TryFind(int offset, out CacheEntry entry, out int entryIndex)
        {
            lock (_lock)
            {
                foreach (ReservedRegion _ in _jitRegions)
                {
                    int index = _cacheEntries.BinarySearch(new CacheEntry(offset, 0, default));

                    if (index < 0)
                    {
                        index = ~index - 1;
                    }

                    if (index >= 0)
                    {
                        entry = _cacheEntries[index];
                        entryIndex = index;
                        return true;
                    }
                }
            }

            entry = default;
            entryIndex = 0;
            return false;
        }
    }
}
