using Ryujinx.Memory;
using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace ARMeilleure.CodeGen.X86
{
    static class HardwareCapabilities
    {
        private delegate uint GetXcr0();

        static HardwareCapabilities()
        {
            if (!X86Base.IsSupported)
            {
                return;
            }

            (int maxNum, _, _, _) = X86Base.CpuId(0x00000000, 0x00000000);

            (_, _, int ecx1, int edx1) = X86Base.CpuId(0x00000001, 0x00000000);
            FeatureInfo1Edx = (FeatureFlags1Edx)edx1;
            FeatureInfo1Ecx = (FeatureFlags1Ecx)ecx1;

            if (maxNum >= 7)
            {
                (_, int ebx7, int ecx7, _) = X86Base.CpuId(0x00000007, 0x00000000);
                FeatureInfo7Ebx = (FeatureFlags7Ebx)ebx7;
                FeatureInfo7Ecx = (FeatureFlags7Ecx)ecx7;
            }

            Xcr0InfoEax = (Xcr0FlagsEax)GetXcr0Eax();
        }

        private static uint GetXcr0Eax()
        {
            if (!FeatureInfo1Ecx.HasFlag(FeatureFlags1Ecx.Xsave))
            {
                // XSAVE feature required for xgetbv
                return 0;
            }

            ReadOnlySpan<byte> asmGetXcr0 =
            [
                0x31, 0xc9, // xor ecx, ecx
                0xf, 0x01, 0xd0, // xgetbv
                0xc3 // ret
            ];

            using MemoryBlock memGetXcr0 = new((ulong)asmGetXcr0.Length);

            memGetXcr0.Write(0, asmGetXcr0);

            memGetXcr0.Reprotect(0, (ulong)asmGetXcr0.Length, MemoryPermission.ReadAndExecute);

            GetXcr0 fGetXcr0 = Marshal.GetDelegateForFunctionPointer<GetXcr0>(memGetXcr0.Pointer);

            return fGetXcr0();
        }

        [Flags]
        public enum FeatureFlags1Edx
        {
            Sse = 1 << 25,
            Sse2 = 1 << 26,
        }

        [Flags]
        public enum FeatureFlags1Ecx
        {
            Sse3 = 1 << 0,
            Pclmulqdq = 1 << 1,
            Ssse3 = 1 << 9,
            Fma = 1 << 12,
            Sse41 = 1 << 19,
            Sse42 = 1 << 20,
            Popcnt = 1 << 23,
            Aes = 1 << 25,
            Xsave = 1 << 26,
            Osxsave = 1 << 27,
            Avx = 1 << 28,
            F16c = 1 << 29,
        }

        [Flags]
        public enum FeatureFlags7Ebx
        {
            Avx2 = 1 << 5,
            Avx512f = 1 << 16,
            Avx512dq = 1 << 17,
            Sha = 1 << 29,
            Avx512bw = 1 << 30,
            Avx512vl = 1 << 31,
        }

        [Flags]
        public enum FeatureFlags7Ecx
        {
            Gfni = 1 << 8,
        }

        [Flags]
        public enum Xcr0FlagsEax
        {
            Sse = 1 << 1,
            YmmHi128 = 1 << 2,
            Opmask = 1 << 5,
            ZmmHi256 = 1 << 6,
            Hi16Zmm = 1 << 7,
        }

        public static FeatureFlags1Edx FeatureInfo1Edx { get; }
        public static FeatureFlags1Ecx FeatureInfo1Ecx { get; }
        public static FeatureFlags7Ebx FeatureInfo7Ebx { get; } = 0;
        public static FeatureFlags7Ecx FeatureInfo7Ecx { get; } = 0;
        public static Xcr0FlagsEax Xcr0InfoEax { get; } = 0;

        public static bool SupportsSse => FeatureInfo1Edx.HasFlag(FeatureFlags1Edx.Sse);
        public static bool SupportsSse2 => FeatureInfo1Edx.HasFlag(FeatureFlags1Edx.Sse2);
        public static bool SupportsSse3 => FeatureInfo1Ecx.HasFlag(FeatureFlags1Ecx.Sse3);
        public static bool SupportsPclmulqdq => FeatureInfo1Ecx.HasFlag(FeatureFlags1Ecx.Pclmulqdq);
        public static bool SupportsSsse3 => FeatureInfo1Ecx.HasFlag(FeatureFlags1Ecx.Ssse3);
        public static bool SupportsFma => FeatureInfo1Ecx.HasFlag(FeatureFlags1Ecx.Fma);
        public static bool SupportsSse41 => FeatureInfo1Ecx.HasFlag(FeatureFlags1Ecx.Sse41);
        public static bool SupportsSse42 => FeatureInfo1Ecx.HasFlag(FeatureFlags1Ecx.Sse42);
        public static bool SupportsPopcnt => FeatureInfo1Ecx.HasFlag(FeatureFlags1Ecx.Popcnt);
        public static bool SupportsAesni => FeatureInfo1Ecx.HasFlag(FeatureFlags1Ecx.Aes);
        public static bool SupportsAvx => FeatureInfo1Ecx.HasFlag(FeatureFlags1Ecx.Avx | FeatureFlags1Ecx.Xsave | FeatureFlags1Ecx.Osxsave) && Xcr0InfoEax.HasFlag(Xcr0FlagsEax.Sse | Xcr0FlagsEax.YmmHi128);
        public static bool SupportsAvx2 => FeatureInfo7Ebx.HasFlag(FeatureFlags7Ebx.Avx2) && SupportsAvx;
        public static bool SupportsAvx512F => FeatureInfo7Ebx.HasFlag(FeatureFlags7Ebx.Avx512f) && FeatureInfo1Ecx.HasFlag(FeatureFlags1Ecx.Xsave | FeatureFlags1Ecx.Osxsave)
            && Xcr0InfoEax.HasFlag(Xcr0FlagsEax.Sse | Xcr0FlagsEax.YmmHi128 | Xcr0FlagsEax.Opmask | Xcr0FlagsEax.ZmmHi256 | Xcr0FlagsEax.Hi16Zmm);
        public static bool SupportsAvx512Vl => FeatureInfo7Ebx.HasFlag(FeatureFlags7Ebx.Avx512vl) && SupportsAvx512F;
        public static bool SupportsAvx512Bw => FeatureInfo7Ebx.HasFlag(FeatureFlags7Ebx.Avx512bw) && SupportsAvx512F;
        public static bool SupportsAvx512Dq => FeatureInfo7Ebx.HasFlag(FeatureFlags7Ebx.Avx512dq) && SupportsAvx512F;
        public static bool SupportsF16c => FeatureInfo1Ecx.HasFlag(FeatureFlags1Ecx.F16c);
        public static bool SupportsSha => FeatureInfo7Ebx.HasFlag(FeatureFlags7Ebx.Sha);
        public static bool SupportsGfni => FeatureInfo7Ecx.HasFlag(FeatureFlags7Ecx.Gfni);

        public static bool ForceLegacySse { get; set; }

        public static bool SupportsVexEncoding => SupportsAvx && !ForceLegacySse;
        public static bool SupportsEvexEncoding => SupportsAvx512F && !ForceLegacySse;
    }
}
