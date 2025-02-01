using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Ryujinx.Cpu.LightningJit.Cache
{
    [SupportedOSPlatform("macos")]
    static partial class JitSupportDarwin
    {
        [LibraryImport("libarmeilleure-jitsupport", EntryPoint = "armeilleure_jit_memcpy")]
        public static partial void Copy(nint dst, nint src, ulong n);

        [LibraryImport("libc", EntryPoint = "sys_icache_invalidate", SetLastError = true)]
        public static partial void SysIcacheInvalidate(nint start, nint len);
    }
}
