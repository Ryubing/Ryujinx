#define AluBinary

using ARMeilleure.State;
using NUnit.Framework;

namespace Ryujinx.Tests.Cpu
{
    [Category("AluBinary")]
    public sealed class CpuTestAluBinary : CpuTest
    {
#if AluBinary
        public struct CrcTest
        {
            public uint Crc;
            public ulong Value;
            public bool C;

            public uint[] Results; // One result for each CRC variant (8, 16, 32)

            public CrcTest(uint crc, ulong value, bool c, params uint[] results)
            {
                Crc = crc;
                Value = value;
                C = c;
                Results = results;
            }
        }

        #region "ValueSource (CRC32)"
        private static CrcTest[] _CRC32_Test_Values_()
        {
            // Created with http://www.sunshine2k.de/coding/javascript/crc/crc_js.html, with:
            //  - non-reflected polynomials
            //  - input reflected, result reflected
            //  - bytes in order of increasing significance
            //  - xor 0
            // Only includes non-C variant, as the other can be tested with unicorn.

            return
            [
                new CrcTest(0x00000000u, 0x00_00_00_00_00_00_00_00u, false, 0x00000000, 0x00000000, 0x00000000, 0x00000000),
                new CrcTest(0x00000000u, 0x7f_ff_ff_ff_ff_ff_ff_ffu, false, 0x2d02ef8d, 0xbe2612ff, 0xdebb20e3, 0xa9de8355),
                new CrcTest(0x00000000u, 0x80_00_00_00_00_00_00_00u, false, 0x00000000, 0x00000000, 0x00000000, 0xedb88320),
                new CrcTest(0x00000000u, 0xff_ff_ff_ff_ff_ff_ff_ffu, false, 0x2d02ef8d, 0xbe2612ff, 0xdebb20e3, 0x44660075),
                new CrcTest(0x00000000u, 0xa0_02_f1_ca_52_78_8c_1cu, false, 0x14015c4f, 0x02799256, 0x9063c9e5, 0x8816610a),

                new CrcTest(0xffffffffu, 0x00_00_00_00_00_00_00_00u, false, 0x2dfd1072, 0xbe26ed00, 0xdebb20e3, 0x9add2096),
                new CrcTest(0xffffffffu, 0x7f_ff_ff_ff_ff_ff_ff_ffu, false, 0x00ffffff, 0x0000ffff, 0x00000000, 0x3303a3c3),
                new CrcTest(0xffffffffu, 0x80_00_00_00_00_00_00_00u, false, 0x2dfd1072, 0xbe26ed00, 0xdebb20e3, 0x7765a3b6),
                new CrcTest(0xffffffffu, 0xff_ff_ff_ff_ff_ff_ff_ffu, false, 0x00ffffff, 0x0000ffff, 0x00000000, 0xdebb20e3),
                new CrcTest(0xffffffffu, 0xa0_02_f1_ca_52_78_8c_1cu, false, 0x39fc4c3d, 0xbc5f7f56, 0x4ed8e906, 0x12cb419c)
            ];
        }
        #endregion

        [Test, Combinatorial]
        public void Crc32_b_h_w_x([Values(0u)] uint rd,
                                  [Values(1u)] uint rn,
                                  [Values(2u)] uint rm,
                                  [Range(0u, 3u)] uint size,
                                  [ValueSource(nameof(_CRC32_Test_Values_))] CrcTest test)
        {
            uint opcode = 0x1AC04000; // CRC32B W0, W0, W0

            opcode |= size << 10;
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            if (size == 3)
            {
                opcode |= 0x80000000;
            }

            uint w31 = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, x1: test.Crc, x2: test.Value, x31: w31, runUnicorn: false);

            ExecutionContext context = GetContext();
            ulong result = context.GetX((int)rd);
            Assert.That(result == test.Results[size]);
        }

        [Test, Pairwise, Description("CRC32X <Wd>, <Wn>, <Xm>"), Ignore("Unicorn fails.")]
        public void Crc32x([Values(0u, 31u)] uint rd,
                           [Values(1u, 31u)] uint rn,
                           [Values(2u, 31u)] uint rm,
                           [Values(0x00000000u, 0xFFFFFFFFu)] uint wn,
                           [Values((ulong)0x00_00_00_00_00_00_00_00,
                                   (ulong)0x7F_FF_FF_FF_FF_FF_FF_FF,
                                   0x80_00_00_00_00_00_00_00,
                                   0xFF_FF_FF_FF_FF_FF_FF_FF)] ulong xm)
        {
            uint opcode = 0x9AC04C00; // CRC32X W0, W0, X0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            uint w31 = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: xm, x31: w31);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("CRC32W <Wd>, <Wn>, <Wm>"), Ignore("Unicorn fails.")]
        public void Crc32w([Values(0u, 31u)] uint rd,
                           [Values(1u, 31u)] uint rn,
                           [Values(2u, 31u)] uint rm,
                           [Values(0x00000000u, 0xFFFFFFFFu)] uint wn,
                           [Values((uint)0x00_00_00_00, (uint)0x7F_FF_FF_FF,
                                   0x80_00_00_00, 0xFF_FF_FF_FF)] uint wm)
        {
            uint opcode = 0x1AC04800; // CRC32W W0, W0, W0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            uint w31 = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("CRC32H <Wd>, <Wn>, <Wm>"), Ignore("Unicorn fails.")]
        public void Crc32h([Values(0u, 31u)] uint rd,
                           [Values(1u, 31u)] uint rn,
                           [Values(2u, 31u)] uint rm,
                           [Values(0x00000000u, 0xFFFFFFFFu)] uint wn,
                           [Values((ushort)0x00_00, (ushort)0x7F_FF,
                                   (ushort)0x80_00, (ushort)0xFF_FF)] ushort wm)
        {
            uint opcode = 0x1AC04400; // CRC32H W0, W0, W0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            uint w31 = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("CRC32B <Wd>, <Wn>, <Wm>"), Ignore("Unicorn fails.")]
        public void Crc32b([Values(0u, 31u)] uint rd,
                           [Values(1u, 31u)] uint rn,
                           [Values(2u, 31u)] uint rm,
                           [Values(0x00000000u, 0xFFFFFFFFu)] uint wn,
                           [Values((byte)0x00, (byte)0x7F,
                                   (byte)0x80, (byte)0xFF)] byte wm)
        {
            uint opcode = 0x1AC04000; // CRC32B W0, W0, W0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            uint w31 = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("CRC32CX <Wd>, <Wn>, <Xm>")]
        public void Crc32cx([Values(0u, 31u)] uint rd,
                            [Values(1u, 31u)] uint rn,
                            [Values(2u, 31u)] uint rm,
                            [Values(0x00000000u, 0xFFFFFFFFu)] uint wn,
                            [Values((ulong)0x00_00_00_00_00_00_00_00,
                                    (ulong)0x7F_FF_FF_FF_FF_FF_FF_FF,
                                    0x80_00_00_00_00_00_00_00,
                                    0xFF_FF_FF_FF_FF_FF_FF_FF)] ulong xm)
        {
            uint opcode = 0x9AC05C00; // CRC32CX W0, W0, X0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            uint w31 = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: xm, x31: w31);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("CRC32CW <Wd>, <Wn>, <Wm>")]
        public void Crc32cw([Values(0u, 31u)] uint rd,
                            [Values(1u, 31u)] uint rn,
                            [Values(2u, 31u)] uint rm,
                            [Values(0x00000000u, 0xFFFFFFFFu)] uint wn,
                            [Values((uint)0x00_00_00_00, (uint)0x7F_FF_FF_FF,
                                    0x80_00_00_00, 0xFF_FF_FF_FF)] uint wm)
        {
            uint opcode = 0x1AC05800; // CRC32CW W0, W0, W0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            uint w31 = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("CRC32CH <Wd>, <Wn>, <Wm>")]
        public void Crc32ch([Values(0u, 31u)] uint rd,
                            [Values(1u, 31u)] uint rn,
                            [Values(2u, 31u)] uint rm,
                            [Values(0x00000000u, 0xFFFFFFFFu)] uint wn,
                            [Values((ushort)0x00_00, (ushort)0x7F_FF,
                                    (ushort)0x80_00, (ushort)0xFF_FF)] ushort wm)
        {
            uint opcode = 0x1AC05400; // CRC32CH W0, W0, W0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            uint w31 = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("CRC32CB <Wd>, <Wn>, <Wm>")]
        public void Crc32cb([Values(0u, 31u)] uint rd,
                            [Values(1u, 31u)] uint rn,
                            [Values(2u, 31u)] uint rm,
                            [Values(0x00000000u, 0xFFFFFFFFu)] uint wn,
                            [Values((byte)0x00, (byte)0x7F,
                                    (byte)0x80, (byte)0xFF)] byte wm)
        {
            uint opcode = 0x1AC05000; // CRC32CB W0, W0, W0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            uint w31 = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("SDIV <Xd>, <Xn>, <Xm>")]
        public void Sdiv_64bit([Values(0u, 31u)] uint rd,
                               [Values(1u, 31u)] uint rn,
                               [Values(2u, 31u)] uint rm,
                               [Values(0x0000000000000000ul, 0x7FFFFFFFFFFFFFFFul,
                                       0x8000000000000000ul, 0xFFFFFFFFFFFFFFFFul)] ulong xn,
                               [Values(0x0000000000000000ul, 0x7FFFFFFFFFFFFFFFul,
                                       0x8000000000000000ul, 0xFFFFFFFFFFFFFFFFul)] ulong xm)
        {
            uint opcode = 0x9AC00C00; // SDIV X0, X0, X0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            ulong x31 = TestContext.CurrentContext.Random.NextULong();

            SingleOpcode(opcode, x1: xn, x2: xm, x31: x31);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("SDIV <Wd>, <Wn>, <Wm>")]
        public void Sdiv_32bit([Values(0u, 31u)] uint rd,
                               [Values(1u, 31u)] uint rn,
                               [Values(2u, 31u)] uint rm,
                               [Values(0x00000000u, 0x7FFFFFFFu,
                                       0x80000000u, 0xFFFFFFFFu)] uint wn,
                               [Values(0x00000000u, 0x7FFFFFFFu,
                                       0x80000000u, 0xFFFFFFFFu)] uint wm)
        {
            uint opcode = 0x1AC00C00; // SDIV W0, W0, W0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            uint w31 = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("UDIV <Xd>, <Xn>, <Xm>")]
        public void Udiv_64bit([Values(0u, 31u)] uint rd,
                               [Values(1u, 31u)] uint rn,
                               [Values(2u, 31u)] uint rm,
                               [Values(0x0000000000000000ul, 0x7FFFFFFFFFFFFFFFul,
                                       0x8000000000000000ul, 0xFFFFFFFFFFFFFFFFul)] ulong xn,
                               [Values(0x0000000000000000ul, 0x7FFFFFFFFFFFFFFFul,
                                       0x8000000000000000ul, 0xFFFFFFFFFFFFFFFFul)] ulong xm)
        {
            uint opcode = 0x9AC00800; // UDIV X0, X0, X0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            ulong x31 = TestContext.CurrentContext.Random.NextULong();

            SingleOpcode(opcode, x1: xn, x2: xm, x31: x31);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("UDIV <Wd>, <Wn>, <Wm>")]
        public void Udiv_32bit([Values(0u, 31u)] uint rd,
                               [Values(1u, 31u)] uint rn,
                               [Values(2u, 31u)] uint rm,
                               [Values(0x00000000u, 0x7FFFFFFFu,
                                       0x80000000u, 0xFFFFFFFFu)] uint wn,
                               [Values(0x00000000u, 0x7FFFFFFFu,
                                       0x80000000u, 0xFFFFFFFFu)] uint wm)
        {
            uint opcode = 0x1AC00800; // UDIV W0, W0, W0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            uint w31 = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31);

            CompareAgainstUnicorn();
        }
#endif
    }
}
