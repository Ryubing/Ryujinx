using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Versioning;

namespace ARMeilleure.CodeGen.Arm64
{
    static partial class HardwareCapabilities
    {
        static HardwareCapabilities()
        {
            if (!ArmBase.Arm64.IsSupported)
            {
                return;
            }

            if (OperatingSystem.IsLinux())
            {
                LinuxFeatureInfoHwCap = (LinuxFeatureFlagsHwCap)getauxval(AT_HWCAP);
                LinuxFeatureInfoHwCap2 = (LinuxFeatureFlagsHwCap2)getauxval(AT_HWCAP2);
            }

            if (OperatingSystem.IsMacOS())
            {
                for (int i = 0; i < _sysctlNames.Length; i++)
                {
                    if (CheckSysctlName(_sysctlNames[i]))
                    {
                        MacOsFeatureInfo |= (MacOsFeatureFlags)(1 << i);
                    }
                }
            }
        }

        #region Linux

        private const ulong AT_HWCAP = 16;
        private const ulong AT_HWCAP2 = 26;

        [LibraryImport("libc", SetLastError = true)]
        private static partial ulong getauxval(ulong type);

        [Flags]
        public enum LinuxFeatureFlagsHwCap : ulong
        {
            Fp = 1 << 0,
            Asimd = 1 << 1,
            Evtstrm = 1 << 2,
            Aes = 1 << 3,
            Pmull = 1 << 4,
            Sha1 = 1 << 5,
            Sha2 = 1 << 6,
            Crc32 = 1 << 7,
            Atomics = 1 << 8,
            FpHp = 1 << 9,
            AsimdHp = 1 << 10,
            CpuId = 1 << 11,
            AsimdRdm = 1 << 12,
            Jscvt = 1 << 13,
            Fcma = 1 << 14,
            Lrcpc = 1 << 15,
            DcpOp = 1 << 16,
            Sha3 = 1 << 17,
            Sm3 = 1 << 18,
            Sm4 = 1 << 19,
            AsimdDp = 1 << 20,
            Sha512 = 1 << 21,
            Sve = 1 << 22,
            AsimdFhm = 1 << 23,
            Dit = 1 << 24,
            Uscat = 1 << 25,
            Ilrcpc = 1 << 26,
            FlagM = 1 << 27,
            Ssbs = 1 << 28,
            Sb = 1 << 29,
            Paca = 1 << 30,
            Pacg = 1UL << 31,
        }

        [Flags]
        public enum LinuxFeatureFlagsHwCap2 : ulong
        {
            Dcpodp = 1 << 0,
            Sve2 = 1 << 1,
            SveAes = 1 << 2,
            SvePmull = 1 << 3,
            SveBitperm = 1 << 4,
            SveSha3 = 1 << 5,
            SveSm4 = 1 << 6,
            FlagM2 = 1 << 7,
            Frint = 1 << 8,
            SveI8mm = 1 << 9,
            SveF32mm = 1 << 10,
            SveF64mm = 1 << 11,
            SveBf16 = 1 << 12,
            I8mm = 1 << 13,
            Bf16 = 1 << 14,
            Dgh = 1 << 15,
            Rng = 1 << 16,
            Bti = 1 << 17,
            Mte = 1 << 18,
            Ecv = 1 << 19,
            Afp = 1 << 20,
            Rpres = 1 << 21,
            Mte3 = 1 << 22,
            Sme = 1 << 23,
            Sme_i16i64 = 1 << 24,
            Sme_f64f64 = 1 << 25,
            Sme_i8i32 = 1 << 26,
            Sme_f16f32 = 1 << 27,
            Sme_b16f32 = 1 << 28,
            Sme_f32f32 = 1 << 29,
            Sme_fa64 = 1 << 30,
            Wfxt = 1UL << 31,
            Ebf16 = 1UL << 32,
            Sve_Ebf16 = 1UL << 33,
            Cssc = 1UL << 34,
            Rprfm = 1UL << 35,
            Sve2p1 = 1UL << 36,
        }

        public static LinuxFeatureFlagsHwCap LinuxFeatureInfoHwCap { get; } = 0;
        public static LinuxFeatureFlagsHwCap2 LinuxFeatureInfoHwCap2 { get; } = 0;

        #endregion

        #region macOS

        [LibraryImport("libSystem.dylib", SetLastError = true)]
        private static unsafe partial int sysctlbyname([MarshalAs(UnmanagedType.LPStr)] string name, out int oldValue, ref ulong oldSize, nint newValue, ulong newValueSize);

        [SupportedOSPlatform("macos")]
        private static bool CheckSysctlName(string name)
        {
            ulong size = sizeof(int);
            if (sysctlbyname(name, out int val, ref size, nint.Zero, 0) == 0 && size == sizeof(int))
            {
                return val != 0;
            }
            return false;
        }

        private static readonly string[] _sysctlNames =
        [
            "hw.optional.floatingpoint",
            "hw.optional.AdvSIMD",
            "hw.optional.arm.FEAT_FP16",
            "hw.optional.arm.FEAT_AES",
            "hw.optional.arm.FEAT_PMULL",
            "hw.optional.arm.FEAT_LSE",
            "hw.optional.armv8_crc32",
            "hw.optional.arm.FEAT_SHA1",
            "hw.optional.arm.FEAT_SHA256"
        ];

        [Flags]
        public enum MacOsFeatureFlags
        {
            Fp = 1 << 0,
            AdvSimd = 1 << 1,
            Fp16 = 1 << 2,
            Aes = 1 << 3,
            Pmull = 1 << 4,
            Lse = 1 << 5,
            Crc32 = 1 << 6,
            Sha1 = 1 << 7,
            Sha256 = 1 << 8,
        }

        public static MacOsFeatureFlags MacOsFeatureInfo { get; } = 0;

        #endregion

        public static bool SupportsAdvSimd => LinuxFeatureInfoHwCap.HasFlag(LinuxFeatureFlagsHwCap.Asimd) || MacOsFeatureInfo.HasFlag(MacOsFeatureFlags.AdvSimd);
        public static bool SupportsAes => LinuxFeatureInfoHwCap.HasFlag(LinuxFeatureFlagsHwCap.Aes) || MacOsFeatureInfo.HasFlag(MacOsFeatureFlags.Aes);
        public static bool SupportsPmull => LinuxFeatureInfoHwCap.HasFlag(LinuxFeatureFlagsHwCap.Pmull) || MacOsFeatureInfo.HasFlag(MacOsFeatureFlags.Pmull);
        public static bool SupportsLse => LinuxFeatureInfoHwCap.HasFlag(LinuxFeatureFlagsHwCap.Atomics) || MacOsFeatureInfo.HasFlag(MacOsFeatureFlags.Lse);
        public static bool SupportsCrc32 => LinuxFeatureInfoHwCap.HasFlag(LinuxFeatureFlagsHwCap.Crc32) || MacOsFeatureInfo.HasFlag(MacOsFeatureFlags.Crc32);
        public static bool SupportsSha1 => LinuxFeatureInfoHwCap.HasFlag(LinuxFeatureFlagsHwCap.Sha1) || MacOsFeatureInfo.HasFlag(MacOsFeatureFlags.Sha1);
        public static bool SupportsSha256 => LinuxFeatureInfoHwCap.HasFlag(LinuxFeatureFlagsHwCap.Sha2) || MacOsFeatureInfo.HasFlag(MacOsFeatureFlags.Sha256);
    }
}
