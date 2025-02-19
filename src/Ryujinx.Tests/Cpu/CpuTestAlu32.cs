#define Alu32

using NUnit.Framework;

namespace Ryujinx.Tests.Cpu
{
    [Category("Alu32")]
    public sealed class CpuTestAlu32 : CpuTest32
    {
#if Alu32

        #region "ValueSource (Opcodes)"
        private static uint[] SuHAddSub8()
        {
            return
            [
                0xe6100f90u, // SADD8  R0, R0, R0
                0xe6100ff0u, // SSUB8  R0, R0, R0
                0xe6300f90u, // SHADD8 R0, R0, R0
                0xe6300ff0u, // SHSUB8 R0, R0, R0
                0xe6500f90u, // UADD8  R0, R0, R0
                0xe6500ff0u, // USUB8  R0, R0, R0
                0xe6700f90u, // UHADD8 R0, R0, R0
                0xe6700ff0u // UHSUB8 R0, R0, R0
            ];
        }

        private static uint[] UQAddSub16()
        {
            return
            [
                0xe6200f10u, // QADD16  R0, R0, R0
                0xe6600f10u, // UQADD16 R0, R0, R0
                0xe6600f70u // UQSUB16 R0, R0, R0
            ];
        }

        private static uint[] UQAddSub8()
        {
            return
            [
                0xe6600f90u, // UQADD8 R0, R0, R0
                0xe6600ff0u // UQSUB8 R0, R0, R0
            ];
        }

        private static uint[] SsatUsat()
        {
            return
            [
                0xe6a00010u, // SSAT R0, #1, R0, LSL #0
                0xe6a00050u, // SSAT R0, #1, R0, ASR #32
                0xe6e00010u, // USAT R0, #0, R0, LSL #0
                0xe6e00050u // USAT R0, #0, R0, ASR #32
            ];
        }

        private static uint[] Ssat16Usat16()
        {
            return
            [
                0xe6a00f30u, // SSAT16 R0, #1, R0
                0xe6e00f30u // USAT16 R0, #0, R0
            ];
        }

        private static uint[] LsrLslAsrRor()
        {
            return
            [
                0xe1b00030u, // LSRS R0, R0, R0
                0xe1b00010u, // LSLS R0, R0, R0
                0xe1b00050u, // ASRS R0, R0, R0
                0xe1b00070u // RORS R0, R0, R0
            ];
        }
        #endregion

        private const int RndCnt = 2;

        [Test, Pairwise, Description("RBIT <Rd>, <Rn>")]
        public void Rbit_32bit([Values(0u, 0xdu)] uint rd,
                               [Values(1u, 0xdu)] uint rm,
                               [Values(0x00000000u, 0x7FFFFFFFu,
                                       0x80000000u, 0xFFFFFFFFu)] uint wn)
        {
            uint opcode = 0xe6ff0f30u; // RBIT R0, R0
            opcode |= ((rm & 15) << 0) | ((rd & 15) << 12);

            uint w31 = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, r1: wn, sp: w31);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise]
        public void Lsr_Lsl_Asr_Ror([ValueSource(nameof(LsrLslAsrRor))] uint opcode,
                                    [Values(0x00000000u, 0x7FFFFFFFu,
                                            0x80000000u, 0xFFFFFFFFu)] uint shiftValue,
                                    [Range(0, 31)] int shiftAmount)
        {
            uint rd = 0;
            uint rm = 1;
            uint rs = 2;
            opcode |= ((rm & 15) << 0) | ((rd & 15) << 12) | ((rs & 15) << 8);

            SingleOpcode(opcode, r1: shiftValue, r2: (uint)shiftAmount);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise]
        public void Shadd8([Values(0u, 0xdu)] uint rd,
                           [Values(1u)] uint rm,
                           [Values(2u)] uint rn,
                           [Random(RndCnt)] uint w0,
                           [Random(RndCnt)] uint w1,
                           [Random(RndCnt)] uint w2)
        {
            uint opcode = 0xE6300F90u; // SHADD8 R0, R0, R0

            opcode |= ((rm & 15) << 0) | ((rd & 15) << 12) | ((rn & 15) << 16);

            uint sp = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, r0: w0, r1: w1, r2: w2, sp: sp);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise]
        public void Shsub8([Values(0u, 0xdu)] uint rd,
                           [Values(1u)] uint rm,
                           [Values(2u)] uint rn,
                           [Random(RndCnt)] uint w0,
                           [Random(RndCnt)] uint w1,
                           [Random(RndCnt)] uint w2)
        {
            uint opcode = 0xE6300FF0u; // SHSUB8 R0, R0, R0

            opcode |= ((rm & 15) << 0) | ((rd & 15) << 12) | ((rn & 15) << 16);

            uint sp = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, r0: w0, r1: w1, r2: w2, sp: sp);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise]
        public void Ssat_Usat([ValueSource(nameof(SsatUsat))] uint opcode,
                              [Values(0u, 0xdu)] uint rd,
                              [Values(1u, 0xdu)] uint rn,
                              [Values(0u, 7u, 8u, 0xfu, 0x10u, 0x1fu)] uint sat,
                              [Values(0u, 7u, 8u, 0xfu, 0x10u, 0x1fu)] uint shift,
                              [Values(0x00000000u, 0x7FFFFFFFu,
                                      0x80000000u, 0xFFFFFFFFu)] uint wn)
        {
            opcode |= ((rn & 15) << 0) | ((shift & 31) << 7) | ((rd & 15) << 12) | ((sat & 31) << 16);

            uint w31 = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, r1: wn, sp: w31);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise]
        public void Ssat16_Usat16([ValueSource(nameof(Ssat16Usat16))] uint opcode,
                                  [Values(0u, 0xdu)] uint rd,
                                  [Values(1u, 0xdu)] uint rn,
                                  [Values(0u, 7u, 8u, 0xfu)] uint sat,
                                  [Values(0x00000000u, 0x7FFFFFFFu,
                                          0x80000000u, 0xFFFFFFFFu)] uint wn)
        {
            opcode |= ((rn & 15) << 0) | ((rd & 15) << 12) | ((sat & 15) << 16);

            uint w31 = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, r1: wn, sp: w31);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise]
        public void SU_H_AddSub_8([ValueSource(nameof(SuHAddSub8))] uint opcode,
                                  [Values(0u, 0xdu)] uint rd,
                                  [Values(1u)] uint rm,
                                  [Values(2u)] uint rn,
                                  [Random(RndCnt)] uint w0,
                                  [Random(RndCnt)] uint w1,
                                  [Random(RndCnt)] uint w2)
        {
            opcode |= ((rm & 15) << 0) | ((rd & 15) << 12) | ((rn & 15) << 16);

            uint sp = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, r0: w0, r1: w1, r2: w2, sp: sp);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise]
        public void U_Q_AddSub_16([ValueSource(nameof(UQAddSub16))] uint opcode,
                                  [Values(0u, 0xdu)] uint rd,
                                  [Values(1u)] uint rm,
                                  [Values(2u)] uint rn,
                                  [Random(RndCnt)] uint w0,
                                  [Random(RndCnt)] uint w1,
                                  [Random(RndCnt)] uint w2)
        {
            opcode |= ((rm & 15) << 0) | ((rd & 15) << 12) | ((rn & 15) << 16);

            uint sp = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, r0: w0, r1: w1, r2: w2, sp: sp);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise]
        public void U_Q_AddSub_8([ValueSource(nameof(UQAddSub8))] uint opcode,
                                  [Values(0u, 0xdu)] uint rd,
                                  [Values(1u)] uint rm,
                                  [Values(2u)] uint rn,
                                  [Random(RndCnt)] uint w0,
                                  [Random(RndCnt)] uint w1,
                                  [Random(RndCnt)] uint w2)
        {
            opcode |= ((rm & 15) << 0) | ((rd & 15) << 12) | ((rn & 15) << 16);

            uint sp = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, r0: w0, r1: w1, r2: w2, sp: sp);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise]
        public void Uadd8_Sel([Values(0u)] uint rd,
                              [Values(1u)] uint rm,
                              [Values(2u)] uint rn,
                              [Random(RndCnt)] uint w0,
                              [Random(RndCnt)] uint w1,
                              [Random(RndCnt)] uint w2)
        {
            uint opUadd8 = 0xE6500F90; // UADD8 R0, R0, R0
            uint opSel = 0xE6800FB0; // SEL R0, R0, R0

            opUadd8 |= ((rm & 15) << 0) | ((rd & 15) << 12) | ((rn & 15) << 16);
            opSel |= ((rm & 15) << 0) | ((rd & 15) << 12) | ((rn & 15) << 16);

            SetContext(r0: w0, r1: w1, r2: w2);
            Opcode(opUadd8);
            Opcode(opSel);
            Opcode(0xE12FFF1E); // BX LR
            ExecuteOpcodes();

            CompareAgainstUnicorn();
        }
#endif
    }
}
