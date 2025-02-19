#define SimdFmov

using ARMeilleure.State;
using NUnit.Framework;

namespace Ryujinx.Tests.Cpu
{
    [Category("SimdFmov")]
    public sealed class CpuTestSimdFmov : CpuTest
    {
#if SimdFmov

        #region "ValueSource"
        private static uint[] _F_Mov_Si_S_()
        {
            return
            [
                0x1E201000u // FMOV S0, #2.0
            ];
        }

        private static uint[] _F_Mov_Si_D_()
        {
            return
            [
                0x1E601000u // FMOV D0, #2.0
            ];
        }
        #endregion

        [Test, Pairwise]
        [Explicit]
        public void F_Mov_Si_S([ValueSource(nameof(_F_Mov_Si_S_))] uint opcodes,
                               [Range(0u, 255u, 1u)] uint imm8)
        {
            opcodes |= ((imm8 & 0xFFu) << 13);

            ulong z = TestContext.CurrentContext.Random.NextULong();
            V128 v0 = MakeVectorE0E1(z, z);

            SingleOpcode(opcodes, v0: v0);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise]
        [Explicit]
        public void F_Mov_Si_D([ValueSource(nameof(_F_Mov_Si_D_))] uint opcodes,
                               [Range(0u, 255u, 1u)] uint imm8)
        {
            opcodes |= ((imm8 & 0xFFu) << 13);

            ulong z = TestContext.CurrentContext.Random.NextULong();
            V128 v0 = MakeVectorE1(z);

            SingleOpcode(opcodes, v0: v0);

            CompareAgainstUnicorn();
        }
#endif
    }
}
