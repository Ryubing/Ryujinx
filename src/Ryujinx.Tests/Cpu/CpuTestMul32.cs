#define Mul32

using NUnit.Framework;

namespace Ryujinx.Tests.Cpu
{
    [Category("Mul32")]
    public sealed class CpuTestMul32 : CpuTest32
    {
#if Mul32

        #region "ValueSource (Opcodes)"
        private static uint[] _Smlabb_Smlabt_Smlatb_Smlatt_()
        {
            return
            [
                0xe1000080u, // SMLABB R0, R0, R0, R0
                0xe10000C0u, // SMLABT R0, R0, R0, R0
                0xe10000A0u, // SMLATB R0, R0, R0, R0
                0xe10000E0u // SMLATT R0, R0, R0, R0
            ];
        }

        private static uint[] _Smlawb_Smlawt_()
        {
            return
            [
                0xe1200080u, // SMLAWB R0, R0, R0, R0
                0xe12000C0u // SMLAWT R0, R0, R0, R0
            ];
        }

        private static uint[] _Smulbb_Smulbt_Smultb_Smultt_()
        {
            return
            [
                0xe1600080u, // SMULBB R0, R0, R0
                0xe16000C0u, // SMULBT R0, R0, R0
                0xe16000A0u, // SMULTB R0, R0, R0
                0xe16000E0u // SMULTT R0, R0, R0
            ];
        }

        private static uint[] _Smulwb_Smulwt_()
        {
            return
            [
                0xe12000a0u, // SMULWB R0, R0, R0
                0xe12000e0u // SMULWT R0, R0, R0
            ];
        }
        #endregion

        [Test, Pairwise, Description("SMLA<x><y> <Rd>, <Rn>, <Rm>, <Ra>")]
        public void Smla___32bit([ValueSource(nameof(_Smlabb_Smlabt_Smlatb_Smlatt_))] uint opcode,
                                 [Values(0u, 0xdu)] uint rn,
                                 [Values(1u, 0xdu)] uint rm,
                                 [Values(2u, 0xdu)] uint ra,
                                 [Values(3u, 0xdu)] uint rd,
                                 [Values(0x00000000u, 0x7FFFFFFFu,
                                         0x80000000u, 0xFFFFFFFFu)] uint wn,
                                 [Values(0x00000000u, 0x7FFFFFFFu,
                                         0x80000000u, 0xFFFFFFFFu)] uint wm,
                                 [Values(0x00000000u, 0x7FFFFFFFu,
                                         0x80000000u, 0xFFFFFFFFu)] uint wa)
        {
            opcode |= ((rn & 15) << 0) | ((rm & 15) << 8) | ((ra & 15) << 12) | ((rd & 15) << 16);

            uint w31 = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, r0: wn, r1: wm, r2: wa, sp: w31);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("SMLAW<x> <Rd>, <Rn>, <Rm>, <Ra>")]
        public void Smlaw__32bit([ValueSource(nameof(_Smlawb_Smlawt_))] uint opcode,
                                 [Values(0u, 0xdu)] uint rn,
                                 [Values(1u, 0xdu)] uint rm,
                                 [Values(2u, 0xdu)] uint ra,
                                 [Values(3u, 0xdu)] uint rd,
                                 [Values(0x00000000u, 0x7FFFFFFFu,
                                         0x80000000u, 0xFFFFFFFFu)] uint wn,
                                 [Values(0x00000000u, 0x7FFFFFFFu,
                                         0x80000000u, 0xFFFFFFFFu)] uint wm,
                                 [Values(0x00000000u, 0x7FFFFFFFu,
                                         0x80000000u, 0xFFFFFFFFu)] uint wa)
        {
            opcode |= ((rn & 15) << 0) | ((rm & 15) << 8) | ((ra & 15) << 12) | ((rd & 15) << 16);

            uint w31 = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, r0: wn, r1: wm, r2: wa, sp: w31);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("SMUL<x><y> <Rd>, <Rn>, <Rm>")]
        public void Smul___32bit([ValueSource(nameof(_Smulbb_Smulbt_Smultb_Smultt_))] uint opcode,
                                 [Values(0u, 0xdu)] uint rn,
                                 [Values(1u, 0xdu)] uint rm,
                                 [Values(2u, 0xdu)] uint rd,
                                 [Values(0x00000000u, 0x7FFFFFFFu,
                                         0x80000000u, 0xFFFFFFFFu)] uint wn,
                                 [Values(0x00000000u, 0x7FFFFFFFu,
                                         0x80000000u, 0xFFFFFFFFu)] uint wm)
        {
            opcode |= ((rn & 15) << 0) | ((rm & 15) << 8) | ((rd & 15) << 16);

            uint w31 = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, r0: wn, r1: wm, sp: w31);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("SMULW<x> <Rd>, <Rn>, <Rm>")]
        public void Smulw__32bit([ValueSource(nameof(_Smulwb_Smulwt_))] uint opcode,
                                 [Values(0u, 0xdu)] uint rn,
                                 [Values(1u, 0xdu)] uint rm,
                                 [Values(2u, 0xdu)] uint rd,
                                 [Values(0x00000000u, 0x7FFFFFFFu,
                                         0x80000000u, 0xFFFFFFFFu)] uint wn,
                                 [Values(0x00000000u, 0x7FFFFFFFu,
                                         0x80000000u, 0xFFFFFFFFu)] uint wm)
        {
            opcode |= ((rn & 15) << 0) | ((rm & 15) << 8) | ((rd & 15) << 16);

            uint w31 = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, r0: wn, r1: wm, sp: w31);

            CompareAgainstUnicorn();
        }
#endif
    }
}
