#define System

using ARMeilleure.State;
using NUnit.Framework;
using System.Collections.Generic;

namespace Ryujinx.Tests.Cpu
{
    [Category("System")]
    public sealed class CpuTestSystem : CpuTest
    {
#if System

        #region "ValueSource (Types)"
        private static IEnumerable<ulong> _GenNzcv_()
        {
            yield return 0x0000000000000000ul;
            yield return 0x7FFFFFFFFFFFFFFFul;
            yield return 0x8000000000000000ul;
            yield return 0xFFFFFFFFFFFFFFFFul;

            bool v = TestContext.CurrentContext.Random.NextBool();
            bool c = TestContext.CurrentContext.Random.NextBool();
            bool z = TestContext.CurrentContext.Random.NextBool();
            bool n = TestContext.CurrentContext.Random.NextBool();

            ulong rnd = 0UL;

            rnd |= (v ? 1UL : 0UL) << (int)PState.VFlag;
            rnd |= (c ? 1UL : 0UL) << (int)PState.CFlag;
            rnd |= (z ? 1UL : 0UL) << (int)PState.ZFlag;
            rnd |= (n ? 1UL : 0UL) << (int)PState.NFlag;

            yield return rnd;
        }
        #endregion

        #region "ValueSource (Opcodes)"
        private static uint[] _MrsMsr_Nzcv_()
        {
            return
            [
                0xD53B4200u, // MRS X0, NZCV
                0xD51B4200u // MSR NZCV, X0
            ];
        }
        #endregion

        [Test, Pairwise]
        public void MrsMsr_Nzcv([ValueSource(nameof(_MrsMsr_Nzcv_))] uint opcodes,
                                [Values(0u, 1u, 31u)] uint rt,
                                [ValueSource(nameof(_GenNzcv_))] ulong xt)
        {
            opcodes |= (rt & 31) << 0;

            bool v = TestContext.CurrentContext.Random.NextBool();
            bool c = TestContext.CurrentContext.Random.NextBool();
            bool z = TestContext.CurrentContext.Random.NextBool();
            bool n = TestContext.CurrentContext.Random.NextBool();

            ulong x31 = TestContext.CurrentContext.Random.NextULong();

            SingleOpcode(opcodes, x0: xt, x1: xt, x31: x31, overflow: v, carry: c, zero: z, negative: n);

            CompareAgainstUnicorn();
        }
#endif
    }
}
