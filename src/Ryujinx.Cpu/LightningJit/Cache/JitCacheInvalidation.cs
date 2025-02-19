using ARMeilleure.Memory;
using System.Runtime.InteropServices;

namespace Ryujinx.Cpu.LightningJit.Cache
{
    class JitCacheInvalidation
    {
        private static readonly int[] _invalidationCode =
        [
            unchecked((int)0xd53b0022), // mrs  x2, ctr_el0
            unchecked((int)0xd3504c44), // ubfx x4, x2, #16, #4
            unchecked((int)0x52800083), // mov  w3, #0x4
            unchecked((int)0x12000c45), // and  w5, w2, #0xf
            unchecked((int)0x1ac42064), // lsl  w4, w3, w4
            unchecked((int)0x51000482), // sub  w2, w4, #0x1
            unchecked((int)0x8a220002), // bic  x2, x0, x2
            unchecked((int)0x1ac52063), // lsl  w3, w3, w5
            unchecked((int)0xeb01005f), // cmp  x2, x1
            unchecked((int)0x93407c84), // sxtw x4, w4
            unchecked((int)0x540000a2), // b.cs 3c <do_ic_clear>
            unchecked((int)0xd50b7b22), // dc   cvau, x2
            unchecked((int)0x8b040042), // add  x2, x2, x4
            unchecked((int)0xeb02003f), // cmp  x1, x2
            unchecked((int)0x54ffffa8), // b.hi 2c <dc_clear_loop>
            unchecked((int)0xd5033b9f), // dsb  ish
            unchecked((int)0x51000462), // sub  w2, w3, #0x1
            unchecked((int)0x93407c63), // sxtw x3, w3
            unchecked((int)0x8a220000), // bic  x0, x0, x2
            unchecked((int)0xeb00003f), // cmp  x1, x0
            unchecked((int)0x540000a9), // b.ls 64 <exit>
            unchecked((int)0xd50b7520), // ic   ivau, x0
            unchecked((int)0x8b030000), // add  x0, x0, x3
            unchecked((int)0xeb00003f), // cmp  x1, x0
            unchecked((int)0x54ffffa8), // b.hi 54 <ic_clear_loop>
            unchecked((int)0xd5033b9f), // dsb  ish
            unchecked((int)0xd5033fdf), // isb
            unchecked((int)0xd65f03c0) // ret
        ];

        private delegate void InvalidateCache(ulong start, ulong end);

        private readonly InvalidateCache _invalidateCache;
        private readonly ReservedRegion _invalidateCacheCodeRegion;

        private readonly bool _needsInvalidation;

        public JitCacheInvalidation(IJitMemoryAllocator allocator)
        {
            // On macOS and Windows, a different path is used to write to the JIT cache, which does the invalidation.
            if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                ulong size = (ulong)_invalidationCode.Length * sizeof(int);
                ulong mask = (ulong)ReservedRegion.DefaultGranularity - 1;

                size = (size + mask) & ~mask;

                _invalidateCacheCodeRegion = new ReservedRegion(allocator, size);
                _invalidateCacheCodeRegion.ExpandIfNeeded(size);

                Marshal.Copy(_invalidationCode, 0, _invalidateCacheCodeRegion.Pointer, _invalidationCode.Length);

                _invalidateCacheCodeRegion.Block.MapAsRx(0, size);

                _invalidateCache = Marshal.GetDelegateForFunctionPointer<InvalidateCache>(_invalidateCacheCodeRegion.Pointer);

                _needsInvalidation = true;
            }
        }

        public void Invalidate(nint basePointer, ulong size)
        {
            if (_needsInvalidation)
            {
                _invalidateCache((ulong)basePointer, (ulong)basePointer + size);
            }
        }
    }
}
