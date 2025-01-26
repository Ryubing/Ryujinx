#define AluRs32

using NUnit.Framework;

namespace Ryujinx.Tests.Cpu
{
    [Category("AluImm32")]
    public sealed class CpuTestAluImm32 : CpuTest32
    {
#if AluRs32

        #region "ValueSource (Opcodes)"
        private static uint[] Opcodes()
        {
            return
            [
                0xe2a00000u, // ADC R0, R0, #0
                0xe2b00000u, // ADCS R0, R0, #0
                0xe2800000u, // ADD R0, R0, #0
                0xe2900000u, // ADDS R0, R0, #0
                0xe3c00000u, // BIC R0, R0, #0
                0xe3d00000u, // BICS R0, R0, #0
                0xe2600000u, // RSB R0, R0, #0
                0xe2700000u, // RSBS R0, R0, #0
                0xe2e00000u, // RSC R0, R0, #0
                0xe2f00000u, // RSCS R0, R0, #0
                0xe2c00000u, // SBC R0, R0, #0
                0xe2d00000u, // SBCS R0, R0, #0
                0xe2400000u, // SUB R0, R0, #0
                0xe2500000u // SUBS R0, R0, #0
            ];
        }
        #endregion

        private const int RndCnt = 2;

        [Test, Pairwise]
        public void TestCpuTestAluImm32([ValueSource(nameof(Opcodes))] uint opcode,
                                        [Values(0u, 13u)] uint rd,
                                        [Values(1u, 13u)] uint rn,
                                        [Random(RndCnt)] uint imm,
                                        [Random(RndCnt)] uint wn,
                                        [Values(true, false)] bool carryIn)
        {
            opcode |= ((imm & 0xfff) << 0) | ((rn & 15) << 16) | ((rd & 15) << 12);

            uint sp = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, r1: wn, sp: sp, carry: carryIn);

            CompareAgainstUnicorn();
        }
#endif
    }
}
